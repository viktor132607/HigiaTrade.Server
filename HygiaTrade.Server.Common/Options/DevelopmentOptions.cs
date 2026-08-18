namespace HygiaTrade.Common.Options;

public class DevelopmentOptions
{
    public const string SectionName = "DevelopmentSettings";

    public bool ResetDatabaseOnStart { get; set; }

    public bool ExposeEmailPreviewLinks { get; set; } = true;
}
