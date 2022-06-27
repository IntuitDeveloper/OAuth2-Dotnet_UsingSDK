
using Intuit.Ipp.OAuth2PlatformClient;
using System.Collections.Specialized;
using System.Text.Json;
using System.Web;

namespace QBO.Shared
{
    public class QboHelper
    {
        /// <summary>
        /// Creates a new authorization URL using the OAuth2 class.
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static string GetAuthorizationURL(params OidcScopes[] scopes)
        {
            // Initialize the OAuth2Client and
            // AuthTokens if either is null.
            if (QboLocal.Client == null || QboLocal.Tokens == null) {
                QboLocal.Initialize();
            }

            // 'Local.Client' will never be null here.
            #pragma warning disable CS8602

            // Get the authorization url based
            // on the passed scopes.
            return QboLocal.Client.GetAuthorizationURL(scopes.ToList());
        }

        /// <summary>
        /// Checks the passed <paramref name="queryString"/>.
        /// <br/>
        /// If the query was successful, the function returns <c>true</c> and sets the Token values.
        /// <br/>
        /// Otherwise the function returns <c>false</c> or throws an exception when <paramref name="suppressErrors"/> is <c>false</c>.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="suppressErrors"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static bool CheckQueryParamsAndSet(string queryString, bool suppressErrors = true)
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
                TokenResponse responce = QboLocal.Client.GetBearerTokenAsync(query["code"]).Result;

                // Set the token values with the client
                // responce and query parameters.
                QboLocal.Tokens.AccessToken = responce.AccessToken;
                QboLocal.Tokens.RefreshToken = responce.RefreshToken;
                QboLocal.Tokens.RealmId = query["realmId"];

                // Return true. The Tokens have
                // been set as expected.
                return true;
            }
            else {

                // Is the caller chooses to suppress
                // errors return false instead
                // of throwing an exception.
                if (suppressErrors) {
                    return false;
                }
                else {
                    throw new InvalidDataException(
                        $"The 'code' or 'realmId' was not present in the query parameters '{query}'."
                    );
                }
            }
        }

        /// <summary>
        /// Serializes the static tokens instance (Local.Tokens) and writes the serialized string to the <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Absolute or relative path to the target JSON file to be written.</param>
        public static void WriteTokensAsJson(QboAuthTokens authTokens, string path = ".\\Tokens.json")
        {
            // Serialize the passed object
            // to a JSON formatted string.
            string serialized = JsonSerializer.Serialize(authTokens, new JsonSerializerOptions() {
                WriteIndented = true,
            });

            // Create the parent directory
            // to avoid possible conflicts.
            Directory.CreateDirectory(new FileInfo(path).Directory.FullName);

            // Write the string to the path.
            File.WriteAllText(path, serialized);
        }
    }
}
