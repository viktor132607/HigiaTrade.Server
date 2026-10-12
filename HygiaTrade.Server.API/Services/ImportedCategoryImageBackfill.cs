using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

/// <summary>
/// One-time, idempotent reconciliation of the category artwork uploaded in
/// HigiaTrade.Client/public/images. Only blank image slots are populated.
/// User-uploaded/custom category pictures always take precedence.
/// </summary>
public static class ImportedCategoryImageBackfill
{
    private const string ImageBaseUrl = "https://higiatrade.com/images/";

    // The current storefront's 8 top-level categories. Each asset is tracked
    // in HigiaTrade.Client commit c6036249f3e9085211dc39828eafe2483240b504.
    private static readonly IReadOnlyDictionary<string, string> Artwork =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Аксесоари"] = "a_clean_minimalist_vector_icon_on_a_white_backgro.png",
            ["Ароматизатори за въздух"] = "03_aromatizatori_sprey.png",
            ["Инсектициди"] = "04_insekticidi.png",
            ["Перилни препарати"] = "червена_икона_за_пране_и_препарат.png",
            ["Почистващи препарати"] = "a_clean_flat_vector_icon_in_thick_red_outline_on_w.png",
            ["Препарат за петна"] = "a_clean_minimal_vector_line_art_icon_illustration.png",
            ["Препарати за миене на съдове"] = "07_preparati_za_miene_na_sadove.png",
            ["Професионално почистване"] = "08_profesionalno_pochistvane.png"
        };

    public static async Task<int> ApplyAsync(
        ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var categories = await db.Categories
            .Where(category => !category.IsDeleted &&
                category.ParentCategoryId == null &&
                (category.ImageUri == null || category.ImageUri.Trim() == ""))
            .ToListAsync(cancellationToken);

        int updated = 0;
        foreach (var category in categories)
        {
            if (!Artwork.TryGetValue(category.Name.Trim(), out string? filename))
                continue;
            // Escape Unicode filenames; no remote image copying and no schema changes.
            category.ImageUri = ImageBaseUrl + Uri.EscapeDataString(filename);
            category.ModifiedOn = DateTime.UtcNow;
            updated++;
        }

        if (updated > 0)
            await db.SaveChangesAsync(cancellationToken);

        return updated;
    }
}
