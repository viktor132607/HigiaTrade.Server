using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using HygiaTrade.API.Configuration;
using HygiaTrade.API.Middlewares;
using HygiaTrade.API.ServiceExtensions;
using HygiaTrade.Common.Options;
using HygiaTrade.Data;
using HygiaTrade.Data.Helpers;
using HygiaTrade.Domain.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
	.Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
	.ValidateOnStart();

builder.Services.Configure<ClientAppOptions>(
	builder.Configuration.GetSection(ClientAppOptions.SectionName));

builder.Services.Configure<EmailOptions>(
	builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.Configure<CorsOptions>(
	builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.Configure<PaymentOptions>(
	builder.Configuration.GetSection(PaymentOptions.SectionName));

builder.Services.Configure<DevelopmentOptions>(
	builder.Configuration.GetSection(DevelopmentOptions.SectionName));

builder.Services.Configure<InventoryOptions>(
	builder.Configuration.GetSection(InventoryOptions.SectionName));

builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

JwtOptions jwtOptions = builder.Configuration
	.GetSection(JwtOptions.SectionName)
	.Get<JwtOptions>()
	?? throw new InvalidOperationException("JWT configuration is missing.");

JwtSecurityConfiguration.ValidateOrThrow(jwtOptions);

CorsOptions corsOptions = builder.Configuration
	.GetSection(CorsOptions.SectionName)
	.Get<CorsOptions>()
	?? new CorsOptions();

builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();
builder.Services.AddCustomServices();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

	options.AddFixedWindowLimiter(
		policyName: "contact",
		limiterOptions =>
		{
			limiterOptions.PermitLimit = 5;
			limiterOptions.Window = TimeSpan.FromMinutes(10);
			limiterOptions.QueueLimit = 0;
			limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
			limiterOptions.AutoReplenishment = true;
		});
});

ResolvedDatabaseConnection resolvedDatabaseConnection;

try
{
	resolvedDatabaseConnection =
		DatabaseConnectionStringResolver.Resolve(
			builder.Configuration,
			builder.Environment);
}
catch (InvalidOperationException ex)
{
	Console.Error.WriteLine(
		$"Database configuration error: {ex.Message}");

	throw;
}

builder.Services.AddDbContext<ApplicationDbContext>(
	options =>
		options.UseNpgsql(
			resolvedDatabaseConnection.ConnectionString));

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters =
			JwtSecurityConfiguration
				.CreateTokenValidationParameters(jwtOptions);

		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				Console.Error.WriteLine(
					$"JWT authentication failed: {context.Exception.Message}");

				return Task.CompletedTask;
			},

			OnChallenge = context =>
			{
				Console.Error.WriteLine(
					$"JWT challenge: {context.Error} - {context.ErrorDescription}");

				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
	options.AddPolicy("ConfiguredOrigins", policy =>
	{
		if (corsOptions.AllowedOrigins.Length == 0)
		{
			policy
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod();

			return;
		}

		policy
			.WithOrigins(corsOptions.AllowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

WebApplication app = builder.Build();

app.Logger.LogInformation(
	"PostgreSQL connection resolved from configuration key {DatabaseConnectionSource}.",
	resolvedDatabaseConnection.SourceKey);

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseCors("ConfiguredOrigins");

app.UseRateLimiter();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
	options
		.WithTheme(ScalarTheme.Moon)
		.WithDefaultHttpClient(
			ScalarTarget.Shell,
			ScalarClient.Curl);
});

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
	try
	{
		ApplicationDbContext db =
			scope.ServiceProvider
				.GetRequiredService<ApplicationDbContext>();

		IOptions<DevelopmentOptions> developmentOptionsAccessor =
			scope.ServiceProvider
				.GetRequiredService<IOptions<DevelopmentOptions>>();

		DevelopmentOptions developmentOptions =
			developmentOptionsAccessor.Value;

		if (app.Environment.IsDevelopment() &&
			developmentOptions.ResetDatabaseOnStart)
		{
			await DatabaseUtils.TruncateAllTablesSafeAsync(db);
		}

		app.Logger.LogInformation(
			"Applying Entity Framework Core migrations.");

		const int maxMigrationAttempts = 10;

		for (
			int attempt = 1;
			attempt <= maxMigrationAttempts;
			attempt++)
		{
			try
			{
				await db.Database.MigrateAsync();
				break;
			}
			catch (Exception) when (attempt < maxMigrationAttempts)
			{
				app.Logger.LogWarning(
					"Database is not ready yet. Retrying migration attempt {Attempt}/{MaxAttempts} in 3 seconds.",
					attempt,
					maxMigrationAttempts);

				await Task.Delay(TimeSpan.FromSeconds(3));
			}
		}
	}
	catch (Exception ex)
	{
		app.Logger.LogCritical(
			ex,
			"Database startup failed while applying migrations using configuration key {DatabaseConnectionSource}.",
			resolvedDatabaseConnection.SourceKey);

		throw;
	}
}

await DbInitializer.SeedAsync(app.Services);

app.Run();
