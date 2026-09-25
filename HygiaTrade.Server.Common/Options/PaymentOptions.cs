namespace HygiaTrade.Common.Options;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    public string OnlineProviderName { get; set; } = "";

    public string OnlinePaymentLabel { get; set; } = "Card payment";

    public string[] SupportedMethods { get; set; } =
    [
        "cash-on-delivery",
        "bank-transfer"
    ];

    public BankTransferOptions BankTransfer { get; set; } = new();
}

public class BankTransferOptions
{
    public string Beneficiary { get; set; } = "HygiaTrade Ltd.";

    public string Iban { get; set; } = "";

    public string Bic { get; set; } = "";

    public string BankName { get; set; } = "";

    public string ReferencePrefix { get; set; } = "HT";
}
