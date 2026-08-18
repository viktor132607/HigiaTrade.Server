using System.Text;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class ProductSeeder
{
    private const string LaundryImage = "https://images.unsplash.com/photo-1610557892470-55d9e80c0bce?auto=format&fit=crop&w=1200&q=80";
    private const string CleaningImage = "https://images.unsplash.com/photo-1563453392212-326f5e854473?auto=format&fit=crop&w=1200&q=80";
    private const string KitchenImage = "https://images.unsplash.com/photo-1556911220-bff31c812dba?auto=format&fit=crop&w=1200&q=80";
    private const string BathroomImage = "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1200&q=80";
    private const string FloorImage = "https://images.unsplash.com/photo-1527515637462-cff94eecc1ac?auto=format&fit=crop&w=1200&q=80";
    private const string ProfessionalImage = "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=80";

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        Dictionary<string, Guid> categoryIds = db.Categories
            .Where(category => !string.IsNullOrWhiteSpace(category.Name))
            .ToDictionary(category => category.Name, category => category.Id, StringComparer.OrdinalIgnoreCase);

        if (categoryIds.Count == 0)
        {
            return;
        }

        Guid laundry = GetCategoryId(categoryIds, "Перилни препарати");
        Guid laundryPowder = GetCategoryId(categoryIds, "Течни и прахообразни перилни препарати", laundry);
        Guid softeners = GetCategoryId(categoryIds, "Омекотители", laundry);
        Guid stainRemovers = GetCategoryId(categoryIds, "Препарати за петна", laundry);
        Guid babyLaundry = GetCategoryId(categoryIds, "Бебешки перилни препарати и омекотители", laundry);
        Guid laundryAdditives = GetCategoryId(categoryIds, "Добавки за пране", laundry);
        Guid dishwashing = GetCategoryId(categoryIds, "Препарати за миене на съдове");
        Guid generalCleaning = GetCategoryId(categoryIds, "Препарати за общо почистване");
        Guid bleach = GetCategoryId(categoryIds, "Препарати с белина", generalCleaning);
        Guid kitchen = GetCategoryId(categoryIds, "Почистващи препарати за кухня", generalCleaning);
        Guid bathroom = GetCategoryId(categoryIds, "Почистващи препарати за баня", generalCleaning);
        Guid floor = GetCategoryId(categoryIds, "Почистващи препарати за подови настилки", generalCleaning);
        Guid furniture = GetCategoryId(categoryIds, "Почистващи препарати за мебели", generalCleaning);
        Guid windows = GetCategoryId(categoryIds, "Почистващи препарати за прозорци", generalCleaning);
        Guid carpets = GetCategoryId(categoryIds, "Почистващи препарати за килими и дамаски", generalCleaning);
        Guid accessories = GetCategoryId(categoryIds, "Аксесоари");
        Guid professional = GetCategoryId(categoryIds, "Професионално почистване");
        Guid airFresheners = GetCategoryId(categoryIds, "Ароматизатори за въздух");
        Guid insecticides = GetCategoryId(categoryIds, "Инсектициди");
        Guid paper = GetCategoryId(categoryIds, "Хартия");

        List<Product> products =
        [
            CreateProduct(
                title: "Sano Maxima Bio Color прах за пране 1.25 кг",
                description: BuildDescription(
                    "Концентриран прах за пране Sano Maxima Bio Color за цветни тъкани и упорити петна.",
                    "Подходящ за цветно пране.",
                    "Формула с био ензими за трудни петна.",
                    "Фосфатно свободна формула.",
                    "Ориентировъчно 35 пранета.",
                    "Подходящ за автоматично пране."),
                mainImageUrl: LaundryImage,
                regularPrice: 14.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 60,
                rating: 4.8,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima Sensitive прах за пране 1.25 кг",
                description: BuildDescription(
                    "Концентриран прах за пране Sano Maxima Sensitive за бебешки, детски и чувствителни тъкани.",
                    "Подходящ за чувствителна кожа.",
                    "Формула без фосфати.",
                    "Ориентировъчно 35 пранета.",
                    "Подходящ за семейства с деца.",
                    "Нежно изпиране на ежедневни дрехи."),
                mainImageUrl: LaundryImage,
                regularPrice: 14.98m,
                discountPercentage: 5,
                discountedPrice: 14.23m,
                quantity: 50,
                rating: 4.8,
                categoryId: babyLaundry),
            CreateProduct(
                title: "Sano Maxima Advance прах за пране 1.25 кг",
                description: BuildDescription(
                    "Концентриран прах за пране Sano Maxima Advance с активен кислород за силно почистване.",
                    "Подходящ за бяло и цветно пране според указанията на дрехата.",
                    "Помага срещу упорити петна.",
                    "Фосфатно свободна формула.",
                    "Ориентировъчно 35 пранета.",
                    "За памук, лен и синтетика."),
                mainImageUrl: LaundryImage,
                regularPrice: 14.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 45,
                rating: 4.7,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima Javel Effect прах за пране 1.25 кг",
                description: BuildDescription(
                    "Концентриран прах за пране Sano Maxima Javel Effect за силно почистване и свеж вид на тъканите.",
                    "Подходящ за бяло пране и силно замърсени дрехи.",
                    "Компактна формула.",
                    "Фосфатно свободна формула.",
                    "Ориентировъчно 35 пранета.",
                    "За машинно пране."),
                mainImageUrl: LaundryImage,
                regularPrice: 14.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 40,
                rating: 4.6,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima Bio Color прах за пране 3.25 кг",
                description: BuildDescription(
                    "Голяма опаковка концентриран прах за пране Sano Maxima Bio Color за цветно пране.",
                    "Ориентировъчно 90 пранета.",
                    "Био ензими за петна.",
                    "Фосфатно свободна формула.",
                    "За домакинства с често пране.",
                    "Подходящ за автоматични перални."),
                mainImageUrl: LaundryImage,
                regularPrice: 34.98m,
                discountPercentage: 7,
                discountedPrice: 32.53m,
                quantity: 35,
                rating: 4.9,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima Sensitive прах за пране 3.25 кг",
                description: BuildDescription(
                    "Голяма опаковка Sano Maxima Sensitive за чувствителна кожа, бебешки и детски дрехи.",
                    "Ориентировъчно 90 пранета.",
                    "Подходящ за чувствителни тъкани.",
                    "Фосфатно свободна формула.",
                    "Нежно изпиране.",
                    "Икономична опаковка за семейна употреба."),
                mainImageUrl: LaundryImage,
                regularPrice: 37.98m,
                discountPercentage: 5,
                discountedPrice: 36.08m,
                quantity: 30,
                rating: 4.9,
                categoryId: babyLaundry),
            CreateProduct(
                title: "Sano Maxima Sensitive гел за пране 4 л",
                description: BuildDescription(
                    "Концентриран гел за пране Sano Maxima Sensitive за машинно и ръчно пране.",
                    "Особено подходящ за чувствителна кожа.",
                    "Голяма опаковка 4 литра.",
                    "Ориентировъчно до 80 пранета.",
                    "Подходящ за бебешки и детски дрехи.",
                    "Течна формула за лесно дозиране."),
                mainImageUrl: LaundryImage,
                regularPrice: 37.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 28,
                rating: 4.8,
                categoryId: babyLaundry),
            CreateProduct(
                title: "Sano Maxima Soft Silk концентриран гел за пране 1.5 л",
                description: BuildDescription(
                    "Концентриран гел за пране Sano Maxima Soft Silk с ароматна формула за ежедневна употреба.",
                    "X2 концентрирана формула.",
                    "Ориентировъчно до 60 пранета.",
                    "Подходящ за ръчно и машинно пране.",
                    "Премахва петна и освежава тъканите.",
                    "Компактна опаковка 1.5 л."),
                mainImageUrl: LaundryImage,
                regularPrice: 28.69m,
                discountPercentage: 8,
                discountedPrice: 26.40m,
                quantity: 33,
                rating: 4.7,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima Power Gel 1.5 л",
                description: BuildDescription(
                    "Концентриран гел за пране Sano Maxima Power Gel за ежедневни замърсявания и петна.",
                    "Подходящ за цветни и ежедневни тъкани според етикета.",
                    "Течна формула за удобно дозиране.",
                    "Подходящ за машинно и ръчно пране.",
                    "Икономична концентрирана формула.",
                    "Свеж аромат след пране."),
                mainImageUrl: LaundryImage,
                regularPrice: 24.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 40,
                rating: 4.6,
                categoryId: laundryPowder),
            CreateProduct(
                title: "Sano Maxima омекотител Blue Blossom 1 л",
                description: BuildDescription(
                    "Омекотител Sano Maxima за меки тъкани и дълготраен аромат.",
                    "Подходящ за ежедневна употреба.",
                    "Придава мекота на дрехите.",
                    "Оставя свеж аромат.",
                    "За машинно пране.",
                    "Да се използва според указанията на етикета."),
                mainImageUrl: LaundryImage,
                regularPrice: 8.49m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 70,
                rating: 4.5,
                categoryId: softeners),
            CreateProduct(
                title: "Sano Maxima омекотител Lavender 1 л",
                description: BuildDescription(
                    "Омекотител Sano Maxima с лавандулов аромат за свежо и меко пране.",
                    "Омекотява тъканите.",
                    "Подходящ за ежедневни дрехи.",
                    "Помага за приятен аромат след пране.",
                    "За машинна употреба.",
                    "Икономична домашна опаковка."),
                mainImageUrl: LaundryImage,
                regularPrice: 8.49m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 65,
                rating: 4.5,
                categoryId: softeners),
            CreateProduct(
                title: "Sano Stain Remover препарат за петна 750 мл",
                description: BuildDescription(
                    "Препарат за предварително третиране на петна преди пране.",
                    "Подходящ за локално третиране.",
                    "Помага срещу ежедневни петна.",
                    "Да се тества върху скрит участък при деликатни материи.",
                    "Подходящ за бяло и цветно пране според указанията.",
                    "Удобна спрей опаковка."),
                mainImageUrl: LaundryImage,
                regularPrice: 9.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 55,
                rating: 4.6,
                categoryId: stainRemovers),
            CreateProduct(
                title: "Sano Oxygen добавка за пране 700 г",
                description: BuildDescription(
                    "Добавка за пране с кислородно действие за подпомагане на почистването.",
                    "Подходяща за упорити петна.",
                    "Използва се като допълнение към основния перилен препарат.",
                    "Подходяща за домашна употреба.",
                    "Да се използва според указанията на опаковката.",
                    "Практична суха формула."),
                mainImageUrl: LaundryImage,
                regularPrice: 10.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 38,
                rating: 4.4,
                categoryId: laundryAdditives),
            CreateProduct(
                title: "Sano Spark препарат за измиване на съдове Лимон 1 л",
                description: BuildDescription(
                    "Sano Spark препарат за съдове с лимонов аромат и минимум 24% активни съставки.",
                    "Премахва мазнини и замърсявания.",
                    "Придава блясък на съдове и прибори.",
                    "Ефективен и икономичен.",
                    "Подходящ за топла и студена вода.",
                    "Опаковка 1 литър."),
                mainImageUrl: KitchenImage,
                regularPrice: 6.49m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 100,
                rating: 4.8,
                categoryId: dishwashing),
            CreateProduct(
                title: "Sano Spark препарат за измиване на съдове Лавандула 1 л",
                description: BuildDescription(
                    "Sano Spark препарат за съдове с аромат на лавандула.",
                    "Почиства мазнини.",
                    "Оставя блясък върху съдове и прибори.",
                    "Икономичен при дозиране.",
                    "Подходящ за ежедневна употреба.",
                    "Опаковка 1 литър с помпа."),
                mainImageUrl: KitchenImage,
                regularPrice: 6.79m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 95,
                rating: 4.8,
                categoryId: dishwashing),
            CreateProduct(
                title: "Sano Spark препарат за измиване на съдове Краставица 1 л",
                description: BuildDescription(
                    "Sano Spark препарат за съдове с аромат на краставица.",
                    "Премахва мазнините от съдове и прибори.",
                    "Подходящ за ежедневна употреба.",
                    "Икономична формула.",
                    "Съдържа минимум 24% активни съставки.",
                    "Опаковка 1 литър."),
                mainImageUrl: KitchenImage,
                regularPrice: 6.79m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 90,
                rating: 4.7,
                categoryId: dishwashing),
            CreateProduct(
                title: "Sano Spark ZERO препарат без оцветители и парфюми",
                description: BuildDescription(
                    "Sano Spark ZERO препарат за измиване без парфюми и оцветители.",
                    "Подходящ за бебешки бутилки и бебешки чинии.",
                    "Подходящ за измиване на плодове и зеленчуци според указанията.",
                    "Почиства и придава блясък.",
                    "Премахва мазнините.",
                    "Съдържа минимум 24% активни съставки."),
                mainImageUrl: KitchenImage,
                regularPrice: 7.49m,
                discountPercentage: 5,
                discountedPrice: 7.12m,
                quantity: 80,
                rating: 4.9,
                categoryId: dishwashing),
            CreateProduct(
                title: "Sano Forte Plus обезмаслител за кухня 750 мл",
                description: BuildDescription(
                    "Силен обезмаслител за кухненски повърхности, печки, абсорбатори и плотове.",
                    "Разгражда мазнини.",
                    "Подходящ за кухнята.",
                    "Удобна спрей опаковка.",
                    "Да не се използва върху чувствителни повърхности без тест.",
                    "За домашна и професионална употреба."),
                mainImageUrl: KitchenImage,
                regularPrice: 9.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 75,
                rating: 4.8,
                categoryId: kitchen),
            CreateProduct(
                title: "Sano Jet препарат за фурни и грил 750 мл",
                description: BuildDescription(
                    "Препарат за фурни, грилове и загорели мазнини.",
                    "Подходящ за силни кухненски замърсявания.",
                    "Помага срещу загорели остатъци.",
                    "Спрей опаковка за директно нанасяне.",
                    "Да се използва според инструкциите.",
                    "Подходящ за периодично дълбоко почистване."),
                mainImageUrl: KitchenImage,
                regularPrice: 10.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 58,
                rating: 4.7,
                categoryId: kitchen),
            CreateProduct(
                title: "Sano Antikalk препарат против котлен камък 750 мл",
                description: BuildDescription(
                    "Препарат против котлен камък за баня, кухня и санитарни повърхности.",
                    "Премахва варовик и следи от вода.",
                    "Подходящ за кранове, плочки и мивки.",
                    "Освежава повърхностите.",
                    "Да се тества при чувствителни материали.",
                    "Удобна спрей опаковка."),
                mainImageUrl: BathroomImage,
                regularPrice: 8.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 62,
                rating: 4.7,
                categoryId: bathroom),
            CreateProduct(
                title: "Sano Mildew Remover препарат против мухъл 750 мл",
                description: BuildDescription(
                    "Препарат против мухъл и черни петна във влажни помещения.",
                    "Подходящ за баня и фуги.",
                    "Помага за премахване на мухъл.",
                    "Да се използва при добра вентилация.",
                    "Спрей опаковка за директно нанасяне.",
                    "Да не се смесва с други препарати."),
                mainImageUrl: BathroomImage,
                regularPrice: 9.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 55,
                rating: 4.8,
                categoryId: bathroom),
            CreateProduct(
                title: "Sano Javel препарат с белина 1 л",
                description: BuildDescription(
                    "Почистващ препарат с белина за хигиена и почистване на устойчиви повърхности.",
                    "Подходящ за баня и кухня според указанията.",
                    "Силен почистващ ефект.",
                    "Да не се смесва с други препарати.",
                    "Подходящ за дълбоко почистване.",
                    "Да се използва с внимание върху цветни повърхности."),
                mainImageUrl: CleaningImage,
                regularPrice: 5.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 95,
                rating: 4.6,
                categoryId: bleach),
            CreateProduct(
                title: "Sano Multi Purpose универсален почистващ препарат 1 л",
                description: BuildDescription(
                    "Универсален почистващ препарат за ежедневни повърхности в дома и офиса.",
                    "Подходящ за различни миещи се повърхности.",
                    "Премахва ежедневни замърсявания.",
                    "Оставя усещане за чистота.",
                    "Икономична опаковка 1 литър.",
                    "За редовна употреба."),
                mainImageUrl: CleaningImage,
                regularPrice: 7.49m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 80,
                rating: 4.5,
                categoryId: generalCleaning),
            CreateProduct(
                title: "Sano Floor Plus препарат за под 1 л",
                description: BuildDescription(
                    "Почистващ препарат за под с отблъскващо хлебарките свойство.",
                    "Подходящ за подови настилки.",
                    "Оставя чистота и свежест.",
                    "Използва се разреден във вода.",
                    "Подходящ за редовно миене.",
                    "Практична опаковка 1 литър."),
                mainImageUrl: FloorImage,
                regularPrice: 8.49m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 70,
                rating: 4.6,
                categoryId: floor),
            CreateProduct(
                title: "Sano Floor Fresh Home препарат за под 2 л",
                description: BuildDescription(
                    "Ароматизиран препарат за подови настилки за дома и офиса.",
                    "Подходящ за ежедневна поддръжка.",
                    "Използва се разреден във вода.",
                    "Оставя свеж аромат.",
                    "Подходящ за големи площи.",
                    "Опаковка 2 литра."),
                mainImageUrl: FloorImage,
                regularPrice: 12.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 48,
                rating: 4.5,
                categoryId: floor),
            CreateProduct(
                title: "Sano Poliwix препарат за паркет и дърво 1 л",
                description: BuildDescription(
                    "Препарат за почистване и грижа за дървени подове и паркет.",
                    "Подходящ за поддръжка на дървени повърхности.",
                    "Помага за освежаване на настилката.",
                    "Използва се според указанията на опаковката.",
                    "За редовно почистване.",
                    "Опаковка 1 литър."),
                mainImageUrl: FloorImage,
                regularPrice: 11.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 32,
                rating: 4.5,
                categoryId: floor),
            CreateProduct(
                title: "Sano Furniture Cleaner препарат за мебели 750 мл",
                description: BuildDescription(
                    "Почистващ препарат за мебели и устойчиви повърхности.",
                    "Подходящ за ежедневна поддръжка.",
                    "Премахва прах и леки замърсявания.",
                    "Оставя чист и поддържан вид.",
                    "Да се тества върху скрит участък.",
                    "Спрей опаковка."),
                mainImageUrl: CleaningImage,
                regularPrice: 8.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 36,
                rating: 4.4,
                categoryId: furniture),
            CreateProduct(
                title: "Sano Clear препарат за прозорци и повърхности 4 л",
                description: BuildDescription(
                    "SANO PROFESSIONAL CLEAR течен препарат за прозорци и други повърхности.",
                    "Подходящ за прозорци, огледала, предни стъкла и керамични повърхности.",
                    "Почиства и полира.",
                    "Използва се с контейнер за пръскане.",
                    "Професионална опаковка 4 литра.",
                    "Да се тества върху скрит участък преди употреба."),
                mainImageUrl: ProfessionalImage,
                regularPrice: 29.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 24,
                rating: 4.7,
                categoryId: windows),
            CreateProduct(
                title: "Sano Carpet Shampoo препарат за килими и дамаски 1 л",
                description: BuildDescription(
                    "Препарат за почистване на килими, мокети и дамаски.",
                    "Подходящ за текстилни повърхности.",
                    "Помага срещу петна и неприятни миризми.",
                    "Да се тества върху скрит участък.",
                    "Подходящ за домашна употреба.",
                    "Опаковка 1 литър."),
                mainImageUrl: CleaningImage,
                regularPrice: 12.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 26,
                rating: 4.4,
                categoryId: carpets),
            CreateProduct(
                title: "Sano Professional S-255 Fresh препарат за под 4 л",
                description: BuildDescription(
                    "SANO PROFESSIONAL S-255 FRESH силно ароматизиран течен препарат за подове.",
                    "Подходящ за ръчно почистване.",
                    "Подходящ за автоматично измиване според инструкциите.",
                    "Разрежда се във вода според замърсяването.",
                    "Не се препоръчва смесване с други препарати.",
                    "Професионална опаковка 4 литра."),
                mainImageUrl: ProfessionalImage,
                regularPrice: 34.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 22,
                rating: 4.8,
                categoryId: professional),
            CreateProduct(
                title: "Sano Professional Clear препарат за прозорци 4 л",
                description: BuildDescription(
                    "Професионален течен препарат за прозорци, огледала и други гладки повърхности.",
                    "Почиства и полира.",
                    "Подходящ за офиси и търговски обекти.",
                    "Използва се с пулверизатор.",
                    "Опаковка 4 литра.",
                    "Да не се използва върху хора, животни, храна или електрически точки."),
                mainImageUrl: ProfessionalImage,
                regularPrice: 29.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 21,
                rating: 4.7,
                categoryId: professional),
            CreateProduct(
                title: "Sano DG-1 Forte супер концентриран обезмаслител 4 л",
                description: BuildDescription(
                    "SANO DG-1 Forte супер концентриран обезмаслител за печки и силно замърсени повърхности.",
                    "Подходящ за професионални кухни.",
                    "Силен обезмасляващ ефект.",
                    "Използва се според инструкциите.",
                    "Да не се смесва с други препарати.",
                    "Професионална опаковка 4 литра."),
                mainImageUrl: ProfessionalImage,
                regularPrice: 39.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 18,
                rating: 4.8,
                categoryId: professional),
            CreateProduct(
                title: "Sano Fresh Home ароматизатор за въздух 350 мл",
                description: BuildDescription(
                    "Ароматизатор за въздух Sano Fresh Home за дома, офиса и санитарни помещения.",
                    "Освежава въздуха.",
                    "Подходящ за ежедневна употреба.",
                    "Приятен дълготраен аромат.",
                    "Практична спрей опаковка.",
                    "Опаковка 350 мл."),
                mainImageUrl: CleaningImage,
                regularPrice: 6.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 75,
                rating: 4.5,
                categoryId: airFresheners),
            CreateProduct(
                title: "Sano Fresh Home Lavender ароматизатор 350 мл",
                description: BuildDescription(
                    "Ароматизатор Sano Fresh Home Lavender за свежест в помещенията.",
                    "Подходящ за дом и офис.",
                    "Аромат на лавандула.",
                    "Лесно пръскане.",
                    "Подходящ за санитарни помещения.",
                    "Опаковка 350 мл."),
                mainImageUrl: CleaningImage,
                regularPrice: 6.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 70,
                rating: 4.5,
                categoryId: airFresheners),
            CreateProduct(
                title: "Sano Floor Plus препарат с отблъскващо хлебарки свойство 1 л",
                description: BuildDescription(
                    "Почистващ препарат за под с отблъскващо хлебарките свойство.",
                    "Подходящ за подови настилки.",
                    "Почиства и освежава.",
                    "Помага за поддръжка на чисти помещения.",
                    "Използва се разреден във вода.",
                    "За редовна употреба."),
                mainImageUrl: FloorImage,
                regularPrice: 8.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 50,
                rating: 4.6,
                categoryId: insecticides),
            CreateProduct(
                title: "Sano Anti Cockroach препарат против хлебарки 750 мл",
                description: BuildDescription(
                    "Препарат за третиране на места, където се появяват хлебарки.",
                    "Подходящ за дома и санитарни помещения.",
                    "Да се използва според указанията.",
                    "Да се пази от деца и домашни любимци.",
                    "Практична спрей опаковка.",
                    "За целево третиране."),
                mainImageUrl: CleaningImage,
                regularPrice: 11.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 25,
                rating: 4.3,
                categoryId: insecticides),
            CreateProduct(
                title: "Sano Microfiber кърпа универсална",
                description: BuildDescription(
                    "Микрофибърна кърпа за почистване на повърхности в дома и офиса.",
                    "Подходяща за сухо и влажно почистване.",
                    "Може да се използва върху различни повърхности.",
                    "Практичен аксесоар за ежедневна употреба.",
                    "Лесна за изпиране.",
                    "Мека структура."),
                mainImageUrl: CleaningImage,
                regularPrice: 3.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 120,
                rating: 4.5,
                categoryId: accessories),
            CreateProduct(
                title: "Sano гъба за съдове комплект 5 бр.",
                description: BuildDescription(
                    "Комплект гъби за миене на съдове и кухненски повърхности.",
                    "Подходящи за ежедневна употреба.",
                    "С абразивна страна за по-трудни замърсявания.",
                    "Комплект 5 броя.",
                    "Практичен кухненски аксесоар.",
                    "Подходящи за домакинства и офиси."),
                mainImageUrl: KitchenImage,
                regularPrice: 2.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 150,
                rating: 4.4,
                categoryId: accessories),
            CreateProduct(
                title: "Sano домакинска хартия 2 пласта 2 бр.",
                description: BuildDescription(
                    "Домакинска хартия за кухня, почистване и ежедневна употреба.",
                    "Подходяща за попиване и избърсване.",
                    "Практичен пакет 2 броя.",
                    "За дома, офиса и търговски обекти.",
                    "Лесна за употреба.",
                    "Подходяща като консуматив към почистващи продукти."),
                mainImageUrl: CleaningImage,
                regularPrice: 4.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 100,
                rating: 4.3,
                categoryId: paper),
            CreateProduct(
                title: "Sano тоалетна хартия 3 пласта 8 бр.",
                description: BuildDescription(
                    "Тоалетна хартия за дома, офиса и санитарни помещения.",
                    "Пакет 8 броя.",
                    "Подходяща за ежедневна употреба.",
                    "Мека и удобна.",
                    "Практичен консуматив.",
                    "Подходяща за търговски и офис обекти."),
                mainImageUrl: BathroomImage,
                regularPrice: 8.98m,
                discountPercentage: 0,
                discountedPrice: 0m,
                quantity: 85,
                rating: 4.4,
                categoryId: paper),
        ];

        HashSet<string> existingProductTitles = db.Products
            .Where(product => !string.IsNullOrWhiteSpace(product.Title))
            .Select(product => product.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Product> productsToAdd = products
            .Where(product => !existingProductTitles.Contains(product.Title))
            .ToList();

        if (productsToAdd.Count == 0)
        {
            return;
        }

        await db.Products.AddRangeAsync(productsToAdd);
        await db.SaveChangesAsync();
    }

    private static Guid GetCategoryId(Dictionary<string, Guid> categoryIds, string name, Guid? fallback = null)
    {
        if (categoryIds.TryGetValue(name, out Guid id))
        {
            return id;
        }

        if (fallback.HasValue)
        {
            return fallback.Value;
        }

        return categoryIds.Values.First();
    }

    private static Product CreateProduct(
        string title,
        string description,
        string mainImageUrl,
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice,
        uint quantity,
        double rating,
        Guid categoryId,
        params string[] secondaryImageUrls)
    {
        return new Product
        {
            Title = title,
            Description = description,
            MainImageUrl = mainImageUrl,
            RegularPrice = regularPrice,
            DiscountPercentage = discountPercentage,
            DiscountedPrice = discountedPrice,
            Quantity = quantity,
            Rating = rating,
            CategoryId = categoryId,
            SecondaryImages = secondaryImageUrls.Select(url => new Image { Uri = url }).ToList(),
        };
    }

    private static string BuildDescription(string summary, params string[] bulletPoints)
    {
        StringBuilder builder = new();
        builder.Append("<p>").Append(summary).Append("</p><ul>");

        foreach (string bulletPoint in bulletPoints)
        {
            builder.Append("<li>").Append(bulletPoint).Append("</li>");
        }

        builder.Append("</ul>");
        return builder.ToString();
    }
}
