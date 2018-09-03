

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
using Intuit.Ipp.ReportService;

namespace OAuth2_Dotnet_UsingSDK
{
    public partial class Default : System.Web.UI.Page
    {
        // OAuth2 client configuration
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string logPath = ConfigurationManager.AppSettings["logPath"];
        static string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];
        static OAuth2Client oauthClient = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
        static string authCode;
        static string idToken;
        public static IList<JsonWebKey> keys;
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>();

        protected void Page_PreInit(object sender, EventArgs e)
        {
            if (!dictionary.ContainsKey("accessToken"))
            {
                mainButtons.Visible = true;
                connected.Visible = false;
            }
            else
            {
                mainButtons.Visible = false;
                connected.Visible = true;
            }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            AsyncMode = true;
            if (!dictionary.ContainsKey("accessToken"))
            {
                if (Request.QueryString.Count > 0)
                {
                    var response = new AuthorizeResponse(Request.QueryString.ToString());
                    if (response.State != null)
                    {
                        if (oauthClient.CSRFToken == response.State)
                        {
                            if (response.RealmId != null)
                            {
                                if (!dictionary.ContainsKey("realmId"))
                                {
                                    dictionary.Add("realmId", response.RealmId);
                                }
                            }

                            if (response.Code != null)
                            {
                                authCode = response.Code;
                                output("Authorization code obtained.");
                                PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                                Page.RegisterAsyncTask(t);
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
            else
            {
                mainButtons.Visible = false;
                connected.Visible = true;
            }
        }

        #region button click events

        protected void ImgOpenId_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating OpenId call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.OpenId);
                    scopes.Add(OidcScopes.Phone);
                    scopes.Add(OidcScopes.Profile);
                    scopes.Add(OidcScopes.Address);
                    scopes.Add(OidcScopes.Email);

                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected void ImgC2QB_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating OAuth2 call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Accounting);
                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected void ImgGetAppNow_Click(object sender, ImageClickEventArgs e)
        {
            output("Intiating Get App Now call.");
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Accounting);
                    scopes.Add(OidcScopes.OpenId);
                    scopes.Add(OidcScopes.Phone);
                    scopes.Add(OidcScopes.Profile);
                    scopes.Add(OidcScopes.Address);
                    scopes.Add(OidcScopes.Email);

                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected async void btnQBOAPICall_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("accessToken") && dictionary.ContainsKey("realmId"))
            {
                await QboApiCall();
            }
            else
            {
                output("Access token not found.");
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Access token not found.";
            }
        }

        protected async void btnUserInfo_Click(object sender, EventArgs e)
        {
            if (idToken != null)
            {
                var userInfoResp = await oauthClient.GetUserInfoAsync(dictionary["accessToken"]);
                lblUserInfo.Visible = true;
                lblUserInfo.Text = userInfoResp.Raw;
            }
            else
            {
                lblUserInfo.Visible = true;
                lblUserInfo.Text = "UserInfo call is available through OpenId/GetAppNow flow first.";
                output("Go through OpenId flow first.");
            }
        }

        protected async void btnRefresh_Click(object sender, EventArgs e)
        {
            if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("refreshToken")))
            {
                output("Exchanging refresh token for access token.");
                var tokenResp = await oauthClient.RefreshTokenAsync(dictionary["refreshToken"]);
            }
        }

        protected async void btnRevoke_Click(object sender, EventArgs e)
        {
            output("Performing Revoke tokens.");
            if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("refreshToken")))
            {
                var revokeTokenResp = await oauthClient.RevokeTokenAsync(dictionary["refreshToken"]);
                if (revokeTokenResp.HttpStatusCode == HttpStatusCode.OK)
                {
                    dictionary.Clear();
                    if (Request.Url.Query == "")
                        Response.Redirect(Request.RawUrl);
                    else
                        Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
                output("Token revoked.");
            }
        }
        #endregion

        /// <summary>
        /// Start code exchange to get the Access Token and Refresh Token
        /// </summary>
        public async System.Threading.Tasks.Task performCodeExchange()
        {
            output("Exchanging code for tokens.");
            try
            {
                var tokenResp = await oauthClient.GetBearerTokenAsync(authCode);
                if (!dictionary.ContainsKey("accessToken"))
                    dictionary.Add("accessToken", tokenResp.AccessToken);
                else
                    dictionary["accessToken"] = tokenResp.AccessToken;

                if (!dictionary.ContainsKey("refreshToken"))
                    dictionary.Add("refreshToken", tokenResp.RefreshToken);
                else
                    dictionary["refreshToken"] = tokenResp.RefreshToken;

                if (tokenResp.IdentityToken != null)
                    idToken = tokenResp.IdentityToken;
                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }
            catch (Exception ex)
            {
                output("Problem while getting bearer tokens.");
            }
        }

        /// <summary>
        /// Test QBO api call
        /// </summary>
        public async System.Threading.Tasks.Task QboApiCall()
        {
            try
            {
                if ((dictionary.ContainsKey("accessToken")) && (dictionary.ContainsKey("realmId")))
                {
                    output("Making QBO API Call.");
                    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(dictionary["accessToken"]);
                    ServiceContext serviceContext = new ServiceContext(dictionary["realmId"], IntuitServicesType.QBO, oauthValidator);
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
                    //serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";//prod
                    serviceContext.IppConfiguration.MinorVersion.Qbo = "29";
                    ReportService reportService = new ReportService(serviceContext);

                    //Date should be in the format YYYY-MM-DD 
                    //Response format hsold be JSON as that is pnly supported rigth now for reports 
                    reportService.accounting_method = "Accrual";
                    reportService.start_date = "2018-01-01";
                    reportService.end_date = "2018-07-01";
                    ////reportService.classid = "2800000000000634813"; 
                    //reportService.date_macro = "Last Month"; 
                    reportService.summarize_column_by = "Month";


                    //List<String> columndata = new List<String>();
                    //columndata.Add("tx_date");
                    //columndata.Add("dept_name");
                    //string coldata = String.Join(",", columndata);
                    //reportService.columns = coldata;

                    var report1 = reportService.ExecuteReport("TrialBalance");


                    DataService commonServiceQBO = new DataService(serviceContext);
                    //Item item = new Item();
                    //List<Item> results = commonServiceQBO.FindAll<Item>(item, 1, 1).ToList<Item>();
                    QueryService<Invoice> inService = new QueryService<Invoice>(serviceContext);
                    var In = inService.ExecuteIdsQuery("SELECT count(*) FROM Invoice").Count();



                    Batch batch = commonServiceQBO.CreateNewBatch();
                   

                    batch.Add("select count(*) from Account", "queryAccount");
                    batch.Execute();

                    if (batch.IntuitBatchItemResponses != null && batch.IntuitBatchItemResponses.Count() > 0)
                    {
                        IntuitBatchResponse res = batch.IntuitBatchItemResponses.FirstOrDefault();
                        List<Account> acc = res.Entities.ToList().ConvertAll(item => item as Account);
                    };
                        output("QBO call successful.");
                    lblQBOCall.Visible = true;
                    lblQBOCall.Text = "QBO Call successful";
                }
            }
            catch (IdsException ex)
            {
                if (ex.Message == "Unauthorized-401")
                {
                    output("Invalid/Expired Access Token.");

                    var tokenResp = await oauthClient.RefreshTokenAsync(dictionary["refreshToken"]);
                    if (tokenResp.AccessToken != null && tokenResp.RefreshToken != null)
                    {
                        dictionary["accessToken"] = tokenResp.AccessToken;
                        dictionary["refreshToken"] = tokenResp.RefreshToken;
                        await QboApiCall();
                    }
                    else
                    {
                        output("Error while refreshing tokens: " + tokenResp.Raw);
                    }
                }
                else
                {
                    output(ex.Message);
                }
            }
            catch (Exception ex)
            {
                output("Invalid/Expired Access Token.");
            }
        }

        #region Helper methods for logging
        /// <summary>
        /// Gets log path
        /// </summary>
        public string GetLogPath()
        {
            try
            {
                if (logPath == "")
                {
                    logPath = Environment.GetEnvironmentVariable("TEMP");
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
        /// <param name="logMsg">string to be appended</param>
        public void output(string logMsg)
        {
            StreamWriter sw = File.AppendText(GetLogPath() + "OAuth2SampleAppLogs.txt");
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
