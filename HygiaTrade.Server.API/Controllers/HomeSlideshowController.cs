using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/home-slideshow")]
public class HomeSlideshowController(ApplicationDbContext db) : ControllerBase
{
    private const string ContentKey = "home-hero-slideshow";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<HomeSlideshowPayload>> GetAsync()
    {
        await EnsureStoreAsync();
        HomeSlideshowPayload payload = await ReadAsync() ?? CreateDefaultPayload();
        return Ok(payload);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<ActionResult<HomeSlideshowPayload>> UpdateAsync([FromBody] HomeSlideshowPayload payload)
    {
        if (payload.Slides is null || payload.Slides.Count == 0)
        {
            return BadRequest(new { message = "At least one slideshow item is required." });
        }

        if (payload.Slides.Count > 20)
        {
            return BadRequest(new { message = "A maximum of 20 slideshow items is supported." });
        }

        for (int index = 0; index < payload.Slides.Count; index++)
        {
            HomeSlideDto slide = payload.Slides[index];
            if (string.IsNullOrWhiteSpace(slide.Id))
            {
                slide.Id = Guid.NewGuid().ToString("N");
            }

            slide.Order = index;
            slide.TitleBg = Limit(slide.TitleBg, 120);
            slide.TitleEn = Limit(slide.TitleEn, 120);
            slide.EyebrowBg = Limit(slide.EyebrowBg, 120);
            slide.EyebrowEn = Limit(slide.EyebrowEn, 120);
            slide.BadgeBg = Limit(slide.BadgeBg, 80);
            slide.BadgeEn = Limit(slide.BadgeEn, 80);
            slide.NoteBg = Limit(slide.NoteBg, 300);
            slide.NoteEn = Limit(slide.NoteEn, 300);
            slide.CtaBg = Limit(slide.CtaBg, 80);
            slide.CtaEn = Limit(slide.CtaEn, 80);
            slide.CtaUrl = Limit(slide.CtaUrl, 500);
            slide.Image = Limit(slide.Image, 2000);
            slide.Accent = Limit(slide.Accent, 200);
        }

        await EnsureStoreAsync();
        await WriteAsync(payload);
        return Ok(payload);
    }

    private static string Limit(string? value, int maxLength)
    {
        string normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private async Task EnsureStoreAsync()
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS "SiteContent" (
                    "Key" text PRIMARY KEY,
                    "Value" jsonb NOT NULL,
                    "ModifiedOn" timestamp with time zone NOT NULL DEFAULT NOW()
                );
                """;
            await command.ExecuteNonQueryAsync();

            await using DbCommand seedCommand = connection.CreateCommand();
            seedCommand.CommandText = """
                INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                VALUES (@key, CAST(@value AS jsonb), NOW())
                ON CONFLICT ("Key") DO NOTHING;
                """;
            AddParameter(seedCommand, "@key", ContentKey);
            AddParameter(seedCommand, "@value", JsonSerializer.Serialize(CreateDefaultPayload(), JsonOptions));
            await seedCommand.ExecuteNonQueryAsync();
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<HomeSlideshowPayload?> ReadAsync()
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT \"Value\"::text FROM \"SiteContent\" WHERE \"Key\" = @key LIMIT 1;";
            AddParameter(command, "@key", ContentKey);
            object? result = await command.ExecuteScalarAsync();
            string? json = result?.ToString();
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<HomeSlideshowPayload>(json, JsonOptions);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task WriteAsync(HomeSlideshowPayload payload)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                VALUES (@key, CAST(@value AS jsonb), NOW())
                ON CONFLICT ("Key") DO UPDATE SET
                    "Value" = EXCLUDED."Value",
                    "ModifiedOn" = NOW();
                """;
            AddParameter(command, "@key", ContentKey);
            AddParameter(command, "@value", JsonSerializer.Serialize(payload, JsonOptions));
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static HomeSlideshowPayload CreateDefaultPayload() => new()
    {
        Slides =
        [
            new HomeSlideDto
            {
                Id = "1",
                Order = 0,
                IsActive = true,
                EyebrowBg = "Чистота за дома и бизнеса",
                EyebrowEn = "Cleaning for home and business",
                TitleBg = "Почистващи препарати",
                TitleEn = "Cleaning products",
                BadgeBg = "АКТУАЛЕН КАТАЛОГ",
                BadgeEn = "CURRENT CATALOG",
                NoteBg = "Реални продукти, цени и наличности от каталога на HygiaTrade",
                NoteEn = "Real products, prices and stock levels from the HygiaTrade catalog",
                CtaBg = "Към продуктите",
                CtaEn = "View products",
                CtaUrl = "/products",
                Image = "https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=1200&q=80",
                Accent = "from-teal-100 via-cyan-50 to-white"
            },
            new HomeSlideDto
            {
                Id = "2",
                Order = 1,
                IsActive = true,
                EyebrowBg = "Ежедневна грижа",
                EyebrowEn = "Everyday care",
                TitleBg = "Перилни препарати",
                TitleEn = "Laundry detergents",
                BadgeBg = "ЗА ДОМА",
                BadgeEn = "FOR HOME",
                NoteBg = "Продукти за бяло, цветно пране и ежедневна употреба",
                NoteEn = "Products for white and colored laundry and everyday use",
                CtaBg = "Разгледай",
                CtaEn = "Browse",
                CtaUrl = "/products",
                Image = "https://images.unsplash.com/photo-1626806787461-102c1bfaaea1?auto=format&fit=crop&w=1200&q=80",
                Accent = "from-sky-100 via-cyan-50 to-white"
            },
            new HomeSlideDto
            {
                Id = "3",
                Order = 2,
                IsActive = true,
                EyebrowBg = "Професионална хигиена",
                EyebrowEn = "Professional hygiene",
                TitleBg = "За бизнеса и офиса",
                TitleEn = "For business and office",
                BadgeBg = "ПРОФЕСИОНАЛНО",
                BadgeEn = "PROFESSIONAL",
                NoteBg = "Препарати и консумативи с ясни цени и актуални наличности",
                NoteEn = "Cleaning products and supplies with clear prices and current stock",
                CtaBg = "Към каталога",
                CtaEn = "Open catalog",
                CtaUrl = "/products",
                Image = "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=80",
                Accent = "from-emerald-100 via-teal-50 to-white"
            }
        ]
    };
}

public sealed class HomeSlideshowPayload
{
    public List<HomeSlideDto> Slides { get; set; } = [];
}

public sealed class HomeSlideDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string EyebrowBg { get; set; } = string.Empty;
    public string EyebrowEn { get; set; } = string.Empty;
    public string TitleBg { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BadgeBg { get; set; } = string.Empty;
    public string BadgeEn { get; set; } = string.Empty;
    public string NoteBg { get; set; } = string.Empty;
    public string NoteEn { get; set; } = string.Empty;
    public string CtaBg { get; set; } = string.Empty;
    public string CtaEn { get; set; } = string.Empty;
    public string CtaUrl { get; set; } = "/products";
    public string Image { get; set; } = string.Empty;
    public string Accent { get; set; } = "from-teal-100 via-cyan-50 to-white";
}
