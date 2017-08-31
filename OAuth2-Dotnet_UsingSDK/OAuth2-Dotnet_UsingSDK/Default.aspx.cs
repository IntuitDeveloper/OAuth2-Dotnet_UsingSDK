

/******************************************************
 * Intuit sample app for Oauth2 using Intuit .Net SDK
 * RFC docs- https://tools.ietf.org/html/rfc6749
 * ****************************************************/

//https://stackoverflow.com/questions/23562044/window-opener-is-undefined-on-internet-explorer/26359243#26359243
//IE issue- https://stackoverflow.com/questions/7648231/javascript-issue-in-ie-with-window-opener

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Web.UI;
using System.Configuration;
using System.Web;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Threading.Tasks;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.LinqExtender;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using Intuit.Ipp.Exception;
using System.Linq;

namespace OAuth2_Dotnet_UsingSDK
{
    public partial class Default : System.Web.UI.Page
    {
        // OAuth2 client configuration
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string discoveryUrl = ConfigurationManager.AppSettings["discoveryUrl"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string logPath = ConfigurationManager.AppSettings["logPath"];



        static string stateVal;
        static string authorizationEndpoint;
        static string tokenEndpoint;
        static string userinfoEndPoint;
        static string revokeEndpoint;
        static string issuerUrl;

        static string mod;
        static string expo;
        static string code;
        static string incoming_state;
        static string realmId;

        DiscoveryClient discoveryClient;
        DiscoveryResponse doc;
        //AuthorizeRequest request;
        public static IList<JsonWebKey> keys;
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>();





        public HttpContext CurrentContext { get; private set; }

        protected void Page_PreInit(object sender, EventArgs e)
        {
            if (!dictionary.ContainsKey("accessToken"))
            {
                //display connect buttons 
                connect.Visible = true;
                revoke.Visible = false;
                lblConnected.Visible = false;
            }
            else
            {
                //display revoke button
                connect.Visible = false;
                revoke.Visible = true;

            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.AsyncMode = true;

            if (!dictionary.ContainsKey("accessToken"))
            {
                if (Request.QueryString.Count > 0)
                {
                    //Map the querytsring param values to Authorize response to get State and Code
                    var response = new AuthorizeResponse(Request.QueryString.ToString());




                    //extracts the state
                    if (response.State != null)
                    {
                        string CSRF;
                        //state returned after callback
                        incoming_state = response.State;
                        if (dictionary.TryGetValue("CSRF", out CSRF))
                        {
                            //saave the state(CSRF token/Campaign Id/Tracking Id) in session to verify after Oauth2 Callback. This is just for reference. 
                            //actual CSRF handling should be done as per security standards in some hidden fields or encrypted permanent store

                            if (CSRF == incoming_state)
                            {
                                //extract realmId is scope is for C2QB or Get App Now
                                //SIWI will not return realmId/companyId
                                if (response.RealmId != null)
                                {
                                    realmId = response.RealmId;
                                    //Session["realmId"] = realmId;
                                    if (!dictionary.ContainsKey("realmId"))
                                    {
                                        dictionary.Add("realmId", realmId);
                                    }
                                }



                                //extract the code
                                if (response.Code != null)
                                {
                                    code = response.Code;
                                    output("Authorization code obtained.");

                                    //start the code exchange at the Token Endpoint.
                                    //this call will fail with 'invalid grant' error if application is not stopped after testing one button click flow as code is not renewed
                                    PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                                    // Register the asynchronous task.
                                    Page.RegisterAsyncTask(t);

                                    // Execute the register asynchronous task.
                                    Page.ExecuteRegisteredAsyncTasks();

                                }


                            }
                            else
                            {
                                output("Invalid State");
                                dictionary.Clear();

                            }
                        }
                    }

                }

            }
            else
            {
                connect.Visible = false;
                revoke.Visible = true;
            }


        }

        #region button click events

        protected async void ImgC2QB_Click(object sender, ImageClickEventArgs e)
        {
            if (!dictionary.ContainsKey("accessToken"))
            {
                try
                {
                    //get Discovery Data and JWKS Keys
                    //call this once a day or at application_start in your code.
                    await getDiscoveryData_JWKSkeys();
                }
                catch (Exception ex)
                {
                    output(ex.Message);
                }

                //doOauth for Connect to Quickbooks button
                await doOAuth("C2QB");
            }


        }

        protected async void ImgOpenId_Click(object sender, ImageClickEventArgs e)
        {

            if (!dictionary.ContainsKey("accessToken"))
            {

                try
                {
                    //get Discovery Data and JWKS Keys
                    //call this once a day or at application_start in your code.
                    await getDiscoveryData_JWKSkeys();
                }
                catch (Exception ex)
                {
                    output(ex.Message);
                }

                //doOauth for Get App Now button
                await doOAuth("OpenId");
            }


        }

        protected async void ImgSIWI_Click(object sender, ImageClickEventArgs e)
        {

            if (!dictionary.ContainsKey("accessToken"))
            {
                try
                {
                    //get Discovery Data and JWKS Keys
                    //call this once a day or at application_start in your code.
                    await getDiscoveryData_JWKSkeys();
                }
                catch (Exception ex)
                {
                    output(ex.Message);
                }



                //doOauth for Sign In with Intuit button
                await doOAuth("SIWI");
            }


        }

        protected async void ImgRevoke_Click(object sender, ImageClickEventArgs e)
        {
            if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("accessToken")))
            {
                //revoke tokens
                await performRevokeToken(dictionary["accessToken"], dictionary["refreshToken"]);
            }

        }

        protected async void ImgQBOAPICall_Click(object sender, ImageClickEventArgs e)
        {
            if (dictionary.ContainsKey("realmId"))
            {

                if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("accessToken")))
                {
                    //call QBO api
                    await qboApiCall(dictionary["accessToken"], dictionary["refreshToken"], dictionary["realmId"]);
                }
            }
            else
            {
                output("SIWI call does not returns realm for QBO qbo api call.");
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "SIWI call does not returns realm for QBO qbo api call";
            }

        }




        #endregion

        #region get Discovery data

        /// <summary>
        /// Get Discovery Data
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task getDiscoveryData_JWKSkeys()
        {
            output("Fetching Discovery Data.");

            //Intialize DiscoverPolicy
            DiscoveryPolicy dpolicy = new DiscoveryPolicy();
            dpolicy.RequireHttps = true;
            dpolicy.ValidateIssuerName = true;


            //Assign the Sandbox Discovery url for the Apps' Dev clientid and clientsecret that you use
            //Or
            //Assign the Production Discovery url for the Apps' Production clientid and clientsecret that you use



            if (discoveryUrl != null && clientID != null && clientSecret != null)
            {
                discoveryClient = new DiscoveryClient(discoveryUrl);
            }

            doc = await discoveryClient.GetAsync();

            if (doc.StatusCode == HttpStatusCode.OK)
            {
                //Authorization endpoint url
                authorizationEndpoint = doc.AuthorizeEndpoint;

                //Token endpoint url
                tokenEndpoint = doc.TokenEndpoint;

                //UseInfo endpoint url
                userinfoEndPoint = doc.UserInfoEndpoint;

                //Revoke endpoint url
                revokeEndpoint = doc.RevocationEndpoint;

                //Issuer endpoint Url 
                issuerUrl = doc.Issuer;

                //JWKS Keys
                keys = doc.KeySet.Keys;
                output("Discovery Data obtained.");
            }
            else
            {
                output("Discovery error");
            }




        }
        #endregion

        #region OAuth2 calls
        /// <summary>
        /// Start Oauth by getting a code first
        /// </summary>
        /// <param name="callMadeBy"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task doOAuth(string callMadeBy)
        {
            output("Intiating OAuth2 call to get code.");
            string authorizationRequest = "";
            string scopeVal = "";

            ////Save the state(CSRF token/Campaign Id/Tracking Id) in dictionary to verify after Oauth2 Callback. This is just for reference. 
            ////Actual CSRF handling should be done as per security standards in some hidden fields or encrypted permanent store

            stateVal = CryptoRandom.CreateUniqueId();

            if (!dictionary.ContainsKey("CSRF"))
            {
                dictionary.Add("CSRF", stateVal);
            }


            //decide scope based on which flow was initiated
            if (callMadeBy == "C2QB") //C2QB scopes
            {
                if (!dictionary.ContainsKey("callMadeBy"))
                {
                    dictionary.Add("callMadeBy", callMadeBy);
                }
                else
                {
                    dictionary["callMadeBy"] = callMadeBy;
                }

                scopeVal = OidcScopes.Accounting.GetStringValue() + " " + OidcScopes.Payment.GetStringValue();
            }
            else if (callMadeBy == "OpenId")//Get App Now scopes
            {
                if (!dictionary.ContainsKey("callMadeBy"))
                {
                    dictionary.Add("callMadeBy", callMadeBy);
                }
                else
                {
                    dictionary["callMadeBy"] = callMadeBy;
                }

                scopeVal = OidcScopes.Accounting.GetStringValue() + " " + OidcScopes.Payment.GetStringValue()
                    + " " + OidcScopes.OpenId.GetStringValue() + " " + OidcScopes.Address.GetStringValue()
                    + " " + OidcScopes.Email.GetStringValue() + " " + OidcScopes.Phone.GetStringValue()
                    + " " + OidcScopes.Profile.GetStringValue();
            }
            else if (callMadeBy == "SIWI")//Sign In With Intuit scopes
            {
                if (!dictionary.ContainsKey("callMadeBy"))
                {
                    dictionary.Add("callMadeBy", callMadeBy);
                }
                else
                {
                    dictionary["callMadeBy"] = callMadeBy;
                }

                scopeVal = OidcScopes.OpenId.GetStringValue() + " " + OidcScopes.Address.GetStringValue()
                    + " " + OidcScopes.Email.GetStringValue() + " " + OidcScopes.Phone.GetStringValue()
                    + " " + OidcScopes.Profile.GetStringValue();
            }

            output("Setting up Authorize url");
            //Create the OAuth 2.0 authorization request.

            if (authorizationEndpoint != "" && authorizationEndpoint != null)
            {
                authorizationRequest = string.Format("{0}?client_id={1}&response_type=code&scope={2}&redirect_uri={3}&state={4}",
                        authorizationEndpoint,
                        clientID,
                        scopeVal,
                        System.Uri.EscapeDataString(redirectURI),
                        stateVal);


                output("Calling AuthorizeUrl");
                if (callMadeBy == "C2QB" || callMadeBy == "SIWI")
                {
                    //redirect to authorization request url in pop-up
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");

                }
                else
                {
                    //redirect to authorization request url
                    Response.Redirect(authorizationRequest);
                }


            }
            else
            {
                output("Missing authorizationEndpoint url!");
            }


        }


        /// <summary>
        /// Start code exchange to get the Access Token and Refresh Token
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task performCodeExchange()
        {
            output("Exchanging code for tokens.");

            string id_token = "";
            string refresh_token = "";
            string access_token = "";
            bool isTokenValid = false;
            string sub = "";
            string email = "";
            string emailVerified = "";
            string givenName = "";
            string familyName = "";
            string phoneNumber = "";
            string phoneNumberVerified = "";
            string streetAddress = "";
            string locality = "";
            string region = "";
            string postalCode = "";
            string country = "";




            //Request Oauth2 tokens
            var tokenClient = new TokenClient(tokenEndpoint, clientID, clientSecret);

            TokenResponse accesstokenCallResponse = await tokenClient.RequestTokenFromCodeAsync(code, redirectURI);

            if (accesstokenCallResponse.HttpStatusCode == HttpStatusCode.OK)
            {


                //save the refresh token in persistent store so that it can be used to refresh short lived access tokens
                refresh_token = accesstokenCallResponse.RefreshToken;
                if (!dictionary.ContainsKey("refreshToken"))
                {
                    dictionary.Add("refreshToken", refresh_token);
                }


                output("Refresh token obtained.");

                //access token
                access_token = accesstokenCallResponse.AccessToken;
                output("Access token obtained.");

                if (!dictionary.ContainsKey("accessToken"))
                {
                    dictionary.Add("accessToken", access_token);
                }

                //Identity Token (returned only for OpenId scope)
                id_token = accesstokenCallResponse.IdentityToken;
                output("Id token obtained.");

                //validate idToken
                isTokenValid = await isIdTokenValid(id_token);
                output("Validating Id Token.");

                output("Calling UserInfo");
                //get userinfo
                //This will work only for SIWI and Get App Now(OpenId) flows
                //Since C2QB flow does not has the required scopes, you will get exception.
                //Here we will handle the exeception and then finally make QBO api call
                //In your code, based on your workflows/scope, you can choose to not make this call
                UserInfoResponse userInfoResponse = await getUserInfo(access_token, refresh_token);

                if (userInfoResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    //Read UserInfo Details
                    IEnumerable<System.Security.Claims.Claim> userData = userInfoResponse.Json.ToClaims();

                    foreach (System.Security.Claims.Claim item in userData)
                    {
                        if (item.Type == "sub" && item.Value != null)
                            sub = item.Value;
                        if (item.Type == "email" && item.Value != null)
                            email = item.Value;
                        if (item.Type == "emailVerified" && item.Value != null)
                            emailVerified = item.Value;
                        if (item.Type == "givenName" && item.Value != null)
                            givenName = item.Value;
                        if (item.Type == "familyName" && item.Value != null)
                            familyName = item.Value;
                        if (item.Type == "phoneNumber" && item.Value != null)
                            phoneNumber = item.Value;
                        if (item.Type == "phoneNumberVerified" && item.Value != null)
                            phoneNumberVerified = item.Value;

                        if (item.Type == "address" && item.Value != null)
                        {

                            Address jsonObject = JsonConvert.DeserializeObject<Address>(item.Value);

                            if (jsonObject.StreetAddress != null)
                                streetAddress = jsonObject.StreetAddress;
                            if (jsonObject.Locality != null)
                                locality = jsonObject.Locality;
                            if (jsonObject.Region != null)
                                region = jsonObject.Region;
                            if (jsonObject.PostalCode != null)
                                postalCode = jsonObject.PostalCode;
                            if (jsonObject.Country != null)
                                country = jsonObject.Country;
                        }

                    }

                }



            }
            else if (accesstokenCallResponse.HttpStatusCode == HttpStatusCode.Unauthorized && Session["RefreshToken"] != null)
            {
                //Validate if refresh token was already saved in session and use that to regenerate the access token.

                output("Exchanging refresh token for access token.");
                //Handle exception 401 and then make this call
                // Call RefreshToken endpoint to get new access token when you recieve a 401 Status code
                TokenResponse refereshtokenCallResponse = await performRefreshToken(refresh_token);
                if (accesstokenCallResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    //save the refresh token in persistent store so that it can be used to refresh short lived access tokens
                    refresh_token = accesstokenCallResponse.RefreshToken;
                    if (!dictionary.ContainsKey("refreshToken"))
                    {
                        dictionary.Add("refreshToken", refresh_token);
                    }
                    else
                    {
                        dictionary["refreshToken"] = refresh_token;
                    }

                    output("Refresh token obtained.");


                    //access token
                    access_token = accesstokenCallResponse.AccessToken;

                    output("Access token obtained.");
                    if (!dictionary.ContainsKey("accessToken"))
                    {
                        dictionary.Add("accessToken", access_token);
                    }
                    else
                    {
                        dictionary["accessToken"] = access_token;
                    }


                    //Identity Token (returned only for OpenId scope)
                    id_token = accesstokenCallResponse.IdentityToken;
                    output("Id token obtained.");

                    //validate idToken
                    isTokenValid = await isIdTokenValid(id_token);
                    output("Validating Id Token.");


                    output("Calling UserInfo");
                    //get userinfo
                    //This will work only for SIWI and Get App Now(OpenId) flows
                    //Since C2QB flow does not has the required scopes, you will get exception.
                    //Here we will handle the exeception and then finally make QBO api call
                    //In your code, based on your workflows/scope, you can choose to not make this call
                    UserInfoResponse userInfoResponse = await getUserInfo(access_token, refresh_token);

                    if (userInfoResponse.HttpStatusCode == HttpStatusCode.OK)
                    {
                        //Read UserInfo Details
                        IEnumerable<System.Security.Claims.Claim> userData = userInfoResponse.Json.ToClaims();

                        foreach (System.Security.Claims.Claim item in userData)
                        {
                            if (item.Type == "sub" && item.Value != null)
                                sub = item.Value;
                            if (item.Type == "email" && item.Value != null)
                                email = item.Value;
                            if (item.Type == "emailVerified" && item.Value != null)
                                emailVerified = item.Value;
                            if (item.Type == "givenName" && item.Value != null)
                                givenName = item.Value;
                            if (item.Type == "familyName" && item.Value != null)
                                familyName = item.Value;
                            if (item.Type == "phoneNumber" && item.Value != null)
                                phoneNumber = item.Value;
                            if (item.Type == "phoneNumberVerified" && item.Value != null)
                                phoneNumberVerified = item.Value;

                            if (item.Type == "address" && item.Value != null)
                            {

                                Address jsonObject = JsonConvert.DeserializeObject<Address>(item.Value);

                                if (jsonObject.StreetAddress != null)
                                    streetAddress = jsonObject.StreetAddress;
                                if (jsonObject.Locality != null)
                                    locality = jsonObject.Locality;
                                if (jsonObject.Region != null)
                                    region = jsonObject.Region;
                                if (jsonObject.PostalCode != null)
                                    postalCode = jsonObject.PostalCode;
                                if (jsonObject.Country != null)
                                    country = jsonObject.Country;
                            }

                        }

                    }
                }
            }


            //Redirect to pop-up window for C2QB and SIWI flows

            if (dictionary["callMadeBy"] == "OpenId")
            {

                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }





        }

        /// <summary>
        /// Refresh token call
        /// </summary>
        /// <param name="refresh_token"></param>
        /// <returns></returns>
        public async Task<TokenResponse> performRefreshToken(string refresh_token)
        {
            output("Exchanging refresh token for access token.");//refresh token is valid for 100days and access token for 1hr

            //Request Oauth2 tokens
            var tokenClient = new TokenClient(tokenEndpoint, clientID, clientSecret);

            //Handle exception 401 and then make this call
            // Call RefreshToken endpoint to get new access token when you recieve a 401 Status code
            TokenResponse refereshtokenCallResponse = await tokenClient.RequestRefreshTokenAsync(refresh_token);
            output("Access token refreshed.");

            dictionary["accessToken"] = refereshtokenCallResponse.AccessToken;
            dictionary["refreshToken"] = refereshtokenCallResponse.RefreshToken;

            output("Dictionary keys updated.");
            return refereshtokenCallResponse;


        }


        /// <summary>
        /// Revoke token call
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <returns></returns>
        private async System.Threading.Tasks.Task performRevokeToken(string access_token, string refresh_token)
        {
            output("Performing Revoke tokens.");

            var revokeClient = new TokenRevocationClient(revokeEndpoint, clientID, clientSecret);

            //Revoke access token
            TokenRevocationResponse revokeAccessTokenResponse = await revokeClient.RevokeAccessTokenAsync(access_token);
            var revokeAccessTokenStatus = revokeAccessTokenResponse.HttpStatusCode;
            if (revokeAccessTokenStatus == HttpStatusCode.OK)
            {
                //We are removing all sessions and querystring here even if we get error on revoke. 
                //In your code, you can choose to handle the errors and then delete sessions and querystring
                //Session.Clear();
                //Session.Abandon();
                dictionary.Clear();
                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }
            output("Token revoked.");
        }

        /// <summary>
        /// Validate Id token obetained for OpenId flow
        /// </summary>
        /// <param name="id_token"></param>
        /// <returns></returns>
        private System.Threading.Tasks.Task<bool> isIdTokenValid(string id_token)
        {
            output("Making IsIdToken Valid Call.");


            string idToken = id_token;
            if (keys != null)
            {
                //Get mod and exponent value
                foreach (var key in keys)
                {
                    if (key.N != null)
                    {
                        //Mod
                        mod = key.N;
                    }
                    if (key.N != null)
                    {
                        //Exponent
                        expo = key.E;
                    }

                }

                //IdentityToken
                if (idToken != null)
                {
                    //Split the identityToken to get Header and Payload
                    string[] splitValues = idToken.Split('.');
                    if (splitValues[0] != null)
                    {

                        //Decode header 
                        var headerJson = Encoding.UTF8.GetString(Base64Url.Decode(splitValues[0].ToString()));

                        //Deserilaize headerData
                        IdTokenHeader headerData = JsonConvert.DeserializeObject<IdTokenHeader>(headerJson);

                        //Verify if the key id of the key used to sign the payload is not null
                        if (headerData.Kid == null)
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }

                        //Verify if the hashing alg used to sign the payload is not null
                        if (headerData.Alg == null)
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }

                    }
                    if (splitValues[1] != null)
                    {
                        //Decode payload
                        var payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(splitValues[1].ToString()));


                        IdTokenJWTClaimTypes payloadData = JsonConvert.DeserializeObject<IdTokenJWTClaimTypes>(payloadJson);

                        //Verify Aud matches ClientId
                        if (payloadData.Aud != null)
                        {
                            if (payloadData.Aud[0].ToString() != clientID)
                            {
                                return System.Threading.Tasks.Task.FromResult(false);
                            }
                        }
                        else
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }


                        //Verify Authtime matches the time the ID token was authorized.                
                        if (payloadData.Auth_time == null)
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }



                        //Verify exp matches the time the ID token expires, represented in Unix time (integer seconds).                
                        if (payloadData.Exp != null)
                        {
                            long expiration = Convert.ToInt64(payloadData.Exp);


                            long currentEpochTime = EpochTimeExtensions.ToEpochTime(DateTime.UtcNow);
                            //Verify the ID expiration time with what expiry time you have calculated and saved in your application
                            //If they are equal then it means IdToken has expired 

                            if ((expiration - currentEpochTime) <= 0)
                            {
                                return System.Threading.Tasks.Task.FromResult(false);

                            }



                        }

                        //Verify Iat matches the time the ID token was issued, represented in Unix time (integer seconds).            
                        if (payloadData.Iat == null)
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }


                        //verify Iss matches the  issuer identifier for the issuer of the response.     
                        if (payloadData.Iss != null)
                        {
                            if (payloadData.Iss.ToString() != issuerUrl)
                            {

                                return System.Threading.Tasks.Task.FromResult(false);
                            }
                        }
                        else
                        {
                            return System.Threading.Tasks.Task.FromResult(false);
                        }



                        //Verify sub. Sub is an identifier for the user, unique among all Intuit accounts and never reused. 
                        //An Intuit account can have multiple emails at different points in time, but the sub value is never changed.
                        //Use sub within your application as the unique-identifier key for the user.
                        if (payloadData.Sub == null)
                        {

                            return System.Threading.Tasks.Task.FromResult(false);
                        }



                    }

                    //Use external lib to decode mod and expo value and generte hashes
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

                    //Read values of n and e from discovery document.
                    rsa.ImportParameters(
                      new RSAParameters()
                      {
                          //Read values from discovery document
                          Modulus = Base64Url.Decode(mod),
                          Exponent = Base64Url.Decode(expo)
                      });

                    //Verify Siganture hash matches the signed concatenation of the encoded header and the encoded payload with the specified algorithm
                    SHA256 sha256 = SHA256.Create();

                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(splitValues[0] + '.' + splitValues[1]));

                    RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm("SHA256");
                    if (rsaDeformatter.VerifySignature(hash, Base64Url.Decode(splitValues[2])))
                    {
                        //identityToken is valid
                        return System.Threading.Tasks.Task.FromResult(true);
                    }
                    else
                    {
                        //identityToken is not valid
                        return System.Threading.Tasks.Task.FromResult(false);

                    }
                }
                else
                {
                    //identityToken is not valid
                    return System.Threading.Tasks.Task.FromResult(false);
                }
            }
            else
            {
                //Missing mod and expo values
                return System.Threading.Tasks.Task.FromResult(false);
            }


        }


        /// <summary>
        /// Get User Info
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <returns></returns>
        public async Task<UserInfoResponse> getUserInfo(string access_token, string refresh_token)
        {
            output("Making Get User Info Call.");



            //Get UserInfo data when correct scope is set for SIWI and Get App now flows
            var userInfoClient = new UserInfoClient(userinfoEndPoint);
            UserInfoResponse userInfoResponse = await userInfoClient.GetAsync(access_token);
            output("Get User Info Call completed.");
            return userInfoResponse;


        }
        #endregion

        #region qbo calls
        /// <summary>
        /// Test QBO api call
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <param name="realmId"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task qboApiCall(string access_token, string refresh_token, string realmId)
        {
            try
            {

                if (realmId != "")
                {
                    output("Making QBO API Call.");

                    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(access_token);

                    ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";//sandbox
                                                                                                               //serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";//prod

                    serviceContext.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;
                    serviceContext.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;
                    serviceContext.IppConfiguration.MinorVersion.Qbo = "8";


                    //serviceContext.IppConfiguration.Logger.RequestLog.EnableRequestResponseLogging = true;
                    //serviceContext.IppConfiguration.Logger.RequestLog.ServiceRequestLoggingLocation = @"C:\Users\nshrivastava\Documents\Logs";
                    //serviceContext.RequestId = "897kjhjjhkh9";

                    DataService commonServiceQBO = new DataService(serviceContext);
                    Intuit.Ipp.Data.Item item = new Intuit.Ipp.Data.Item();
                    List<Item> results = commonServiceQBO.FindAll<Item>(item, 1, 1).ToList<Item>();
                    QueryService<Invoice> inService = new QueryService<Invoice>(serviceContext);
                    Invoice In = inService.ExecuteIdsQuery("SELECT * FROM Invoice").FirstOrDefault();

                    output("QBO call successful.");
                    lblQBOCall.Visible = true;
                    lblQBOCall.Text = "QBO Call successful";
                }

            }
            catch (IdsException ex)
            {
                if (ex.Message == "UnAuthorized-401" || ex.Message == "The remote server returned an error: (401) Unauthorized.")               
                {

                    output("Invalid/Expired Access Token.");
                    //if you get a 401 token expiry then perform token refresh
                    await performRefreshToken(refresh_token);
                 
                    if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("realmId")))
                    {
                        
                        await qboApiCall(dictionary["accessToken"], dictionary["refreshToken"], dictionary["realmId"]);

                    }
                }
                else
                {
                    output(ex.Message);
                }

            }
            catch (Exception ex)
            {
                //Check Status Code 401 and then 
                output("Invalid/Expired Access Token.");
            }



        }

        #endregion

        #region methods for Oauth2


        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        public string GetLogPath()
        {


            try
            {
                if (logPath == "")
                {
                    logPath = System.Environment.GetEnvironmentVariable("TEMP");
                    if (!logPath.EndsWith("\\")) logPath += "\\";
                }
            }
            catch
            {
                output("Log error path not found.");
            }

            return logPath;



        }


        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        public void output(string logMsg)
        {
            //Console.WriteLine(logMsg);

            System.IO.StreamWriter sw = System.IO.File.AppendText(GetLogPath() + "OAuth2SampleAppLogs.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, logMsg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }



        #endregion




    }

    /// <summary>
    /// Helper for calling self
    /// </summary>
    public static class ResponseHelper
    {
        public static void Redirect(this HttpResponse response, string url, string target, string windowFeatures)
        {

            if ((String.IsNullOrEmpty(target) || target.Equals("_self", StringComparison.OrdinalIgnoreCase)) && String.IsNullOrEmpty(windowFeatures))
            {
                response.Redirect(url);
            }
            else
            {
                Page page = (Page)HttpContext.Current.Handler;

                if (page == null)
                {
                    throw new InvalidOperationException("Cannot redirect to new window outside Page context.");
                }
                url = page.ResolveClientUrl(url);

                string script;
                if (!String.IsNullOrEmpty(windowFeatures))
                {
                    script = @"window.open(""{0}"", ""{1}"", ""{2}"");";
                }
                else
                {
                    script = @"window.open(""{0}"", ""{1}"");";
                }
                script = String.Format(script, url, target, windowFeatures);
                ScriptManager.RegisterStartupScript(page, typeof(Page), "Redirect", script, true);
            }
        }
    }
}