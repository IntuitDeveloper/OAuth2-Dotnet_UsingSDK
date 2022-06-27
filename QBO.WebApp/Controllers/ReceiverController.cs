using Microsoft.AspNetCore.Mvc;
using QBO.Shared;
using QBO.WebApp.Models;

namespace QBO.WebApp.Controllers
{
    public class ReceiverController : Controller
    {
        public IActionResult Index()
        {
            // Get the current query string
            // from current page.
            string query = Request.QueryString.Value ?? "";

            // Use the the shared helper library
            // to validate the query parameters
            // and write the output file.
            if (QboHelper.CheckQueryParamsAndSet(query) && QboLocal.Tokens != null) {
                QboHelper.WriteTokensAsJson(QboLocal.Tokens, ".\\NewTokens.json");

                // Direct the view to the
                // ReceiverViewModel to report
                // a success message.
                return View(new ReceiverViewModel("Success!"));
            }
            else {

                // Otherwise direct the view to the
                // ReceiverViewModel to report a
                // failure message.
                return View(new ReceiverViewModel("Authentication failed."));
            }
        }
    }
}
