namespace QBO.Shared
{
    internal class AuthTokens
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? RealmId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string RedirectUrl { get; set; } = "https://archleaders.github.io/QBO-OAuth2-DotNET/";
        public string Environment { get; set; } = "sandbox";
    }
}
