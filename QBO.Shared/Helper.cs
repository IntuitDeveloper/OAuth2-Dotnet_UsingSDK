
using Intuit.Ipp.OAuth2PlatformClient;
using System.Collections.Specialized;
using System.Text.Json;
using System.Web;

namespace QBO.Shared
{
    public class Helper
    {
        public static string GetAuthorizationURL(params OidcScopes[] scopes)
        {
            // Initialize the OAuth2Client and
            // AuthTokens if either is null.
            if (Local.Client == null || Local.Tokens == null) {
                Local.Initialize();
            }

            // 'Local.Client' will never be null here.
            #pragma warning disable CS8602

            // Get the authorization url based
            // on the passed scopes.
            return Local.Client.GetAuthorizationURL(scopes.ToList());
        }

        public static void SetTokensFromQuery(string queryString)
        {
            // Parse the query string into a
            // NameValueCollection for easy access
            // to each parameter.
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);

            // Make sure the required query
            // parameters exist.
            if (query["code"] != null && query["realmId"] != null) {

                // Use the OAuth2Client to get a new
                // access token from the QBO servers.
                TokenResponse responce = Local.Client.GetBearerTokenAsync(query["code"]).Result;

                // Set the token values with the client
                // responce and query parameters.
                Local.Tokens.AccessToken = responce.AccessToken;
                Local.Tokens.RefreshToken = responce.RefreshToken;
                Local.Tokens.RealmId = query["realmId"];
            }
            else {
                throw new InvalidDataException(
                    $"The 'code' or 'realmId' was not present in the query parameters '{query}'."
                );
            }
        }

        public static void WriteTokensAsJson(string path = ".\\Tokens.json")
        {
            // Serialize the 'Local.Tokens' AuthTokens
            // instance to a Json formatted string.
            string serialized = JsonSerializer.Serialize(Local.Tokens, new JsonSerializerOptions() {
                WriteIndented = true,
            });

            // Write the string to the passed
            // path parameter
            File.WriteAllText(path, serialized);
        }
    }
}
