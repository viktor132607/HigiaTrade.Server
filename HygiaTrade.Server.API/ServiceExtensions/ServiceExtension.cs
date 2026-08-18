using Microsoft.Extensions.Options;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Options;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.Repositories;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.API.ServiceExtensions;

public static class ServiceExtension
{
	public static IServiceCollection AddCustomServices(
		this IServiceCollection services)
	{
		// SERVICES
		services.AddTransient<IAuthService, AuthService>();
		services.AddTransient<IUserService, UserService>();
		services.AddTransient<ICategoryService, CategoryService>();
		services.AddTransient<IProductService, ProductService>();
		services.AddTransient<IWishlistService, WishlistService>();
		services.AddTransient<IReviewService, ReviewService>();
		services.AddTransient<IOrderService, OrderService>();
		services.AddTransient<IGdprService, GdprService>();

		services.AddSingleton<
			IPasswordResetTokenStore,
			MemoryPasswordResetTokenStore>();

		services.AddTransient<ConsoleEmailNotificationService>();

		services.AddHttpClient<ResendEmailNotificationService>();

		services.AddTransient<IEmailNotificationService>(
			serviceProvider =>
			{
				EmailOptions options = serviceProvider
					.GetRequiredService<IOptions<EmailOptions>>()
					.Value;

				if (options.DeliveryMode.Equals(
					"Resend",
					StringComparison.OrdinalIgnoreCase))
				{
					return serviceProvider
						.GetRequiredService<ResendEmailNotificationService>();
				}

				return serviceProvider
					.GetRequiredService<ConsoleEmailNotificationService>();
			});

		// REPOSITORIES
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<ICategoryRepository, CategoryRepository>();
		services.AddScoped<IProductRepository, ProductRepository>();
		services.AddScoped<IWishlistRepository, WishlistRepository>();
		services.AddScoped<IReviewRepository, ReviewRepository>();
		services.AddScoped<IImageRepository, ImageRepository>();
		services.AddScoped<IOrderRepository, OrderRepository>();
		services.AddScoped<IOrderItemRepository, OrderItemRepository>();

		return services;
	}
}
