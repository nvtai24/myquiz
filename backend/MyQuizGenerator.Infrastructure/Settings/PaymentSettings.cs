namespace MyQuizGenerator.Infrastructure.Settings
{
    public class PaymentSettings
    {
        public const string SectionName = "PaymentSettings";
        public string ApiToken { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
    }
}