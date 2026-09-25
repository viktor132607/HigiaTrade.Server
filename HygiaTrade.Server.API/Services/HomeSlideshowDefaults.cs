using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public interface IHomeSlideshowDefaults
{
    HomeSlideshowPayload Create();
}

public sealed class HomeSlideshowDefaults
    : IHomeSlideshowDefaults
{
    public HomeSlideshowPayload Create() => new()
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
