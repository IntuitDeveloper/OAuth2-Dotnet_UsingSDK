[![Rate your Sample](./docs/res/Ratesample.png)][ss1][![Yes](./docs/res/Thumbup.png)][ss2][![No](./docs/res/Thumbdown.png)][ss3] 

# Quickbooks Online - OAuth2 Samples in DotNET

The Intuit Developer team has written these OAuth 2.0 sample applications using the .NET 6.0 (C# 10) framework to provide working examples of OAuth 2.0 verification concepts and methods.

## Getting Started

Before proceeding, it may be helpful to understand how OAuth 2.0 works in Quickbooks Online. Check out the [Authorization FAQ](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization/faq) and the [Authorization and authentication page](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization) found in the official [Intuit documentation](https://developer.intuit.com/) for more information on OAuth 2.0.

### Pre-Requisites

- [Visual Studio](https://visualstudio.microsoft.com/vs/)
- [DotNET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- A QBO [App](https://developer.intuit.com/app/developer/dashboard)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section) _(Only required for the WinForms and WPF samples.)_

### Setup

Download the [source code](https://github.com/IntuitDeveloper/OAuth2-Dotnet_UsingSDK/archive/refs/heads/master.zip) or use the clone function in [Visual Studio](https://docs.microsoft.com/en-us/visualstudio/version-control/git-clone-repository?view=vs-2022) to clone the repo to a local folder.

#### User Configuration

After cloning or downloading the repo, you will need to update the [Tokens.json](./QBO.Shared/Tokens.jsonc) file to match your [apps](https://developer.intuit.com/app/developer/dashboard) `ClientId` and `ClientSecret`. These values are in the Keys & credentials section under `Development Settings` on your QBO app's dashboard.

```jsonc
{
  // The ClientId and ClientSecret
  // can be found in the QBO app on
  // the Keys & credentials page.
  "ClientId": "{your client id here}",
  "ClientSecret": "{your client secret here}",

  // Make sure this URL (or your custom URL) is
  // added to the redirect URLs in your QBO app.
  // 
  // Note: this URL can be anything as long as
  // it is listed in your QBO apps redirect URLs.
  "RedirectUrl": "https://archleaders.github.io/QBO-OAuth2-DotNET/",

  // This will be filled after running
  // the app and authenticating.
  "AccessToken": null,
  "RefreshToken": null,
  "RealmId": null
}
```

_<ins>Note</ins> — if you are using the [QBO.WebApp](./QBO.WebApp/) project, change the [RedirectUrl](./QBO.Shared/Tokens.jsonc#L13) to `https://localhost:7106/Receiver`_

> _For more information on each configuration parameter, check out [this document](./docs/help/Tokens.md) on the different Tokens and why they are used in OAuth 2.0._

#### Building

Once you have configured the settings to match your QBO App's settings, [build the solution](https://docs.microsoft.com/en-us/visualstudio/ide/walkthrough-building-an-application?view=vs-2022) in Visual Studio and run any one of the sample applications.

## How it Works

This repository is set up to minimize code duplication and keep everything organized. That is done by having a single [shared library](./QBO.Shared/) that handles QBO connections and anything else done in the back-end of your application.

This section covers how each sample project handles OAuth2 authentication with the QBO SDK.

### Desktop — [WinForms](./QBO.WinForms/) / [WPF](./QBO.WPF/)

The Desktop sample implements a [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2) control from the [WebView2 library](https://www.nuget.org/packages/Microsoft.Web.WebView2) to display the Intuit sign-on page to the user while still keeping it contained within the application.

_<ins>Note</ins> — All users must have the [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section) runtime installed on their machine._

#### Authentication Flow

In the desktop sample applications, the authentication code is triggered and ended by two events. These two events can be anything, if the user runs the second event; this is clarified further by examining the authentication flow.

- **First Event** ([`Form.Load`](./QBO.WinForms/MainForm.cs#L13) in the sample application)
  - The `ClientID` and `ClientSecret` are used to get an authorization URL from QBO. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L16-L30)
  - That URL is sent to the `WebView2` control to be rendered. [<sup>WinForms</sup>](./QBO.WinForms/MainForm.cs#L13-L35)
  - The user is then prompted to sign in to their QBO account on the rendered page.
  - After signing in, the `WebView2` control is redirected to the `RedirectUrl` with a `code` and `realmId` in the query parameters.

_At this point, your application has no idea that the authentication completed. We need a message from the user (or the redirected site) to say: "Yes, I have signed in and have been redirected." That message in this example is the `Form.Closing` event._

- **Second Event** ([`Form.Closing`](./QBO.WinForms/MainForm.cs#L37) in the sample applications)
  - The query parameters in the current WebView source URL are sent to the [helper method](./QBO.Shared/QboHelper.cs#L43-L82) to be handled. [<sup>WinForms</sup>](./QBO.WinForms/MainForm.cs#L43-L50)
  - These are then used to get an access token from the `OAuth2Client`. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L56)
  - The next step depends on how you will store your access and refresh tokens. In this sample, it is just stored in a class to be written to a JSON file. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L84-L102) <sup>|</sup> [<sup>WinForms</sup>](./QBO.WinForms/MainForm.cs#L51)

Further details are in the code and comments of each project.

### Web App — [ASP.NET Core](./QBO.WebApp/)

The ASP.NET sample application (as a web app) can natively display the Intuit sign-in page and collect the response from our server by setting the redirect URL to your host address (typically a page set up to receive and handle the query).

#### Authentication Flow

In the ASP.NET sample application, the authentication code is run when the `Home` (root) page is visited and ends when the `Receiver` page is visited. This example is not very practical in a real-world scenario; it is used to leave out unnecessary extra code that might be confusing.

- **First Event** ([`HomeController.Index`](./QBO.WebApp/Controllers/HomeController.cs#L11-L22) in the sample application)
  - The `ClientID` and `ClientSecret` are used to get an authorization URL from QBO. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L16-L30)
  - The controller then redirects to that URL and gets discarded automatically. [<sup>WebApp</sup>](./QBO.WebApp/Controllers/HomeController.cs#L21)
  - The user is then prompted to sign in to there QBO account on the opened page.
  - After the user signs in, a query request is sent to the redirect URL to be handled.

<br>

- **Second Event** ([`ReceiverController.Index`](./QBO.WebApp/Controllers/ReceiverController.cs#L9-L33) in the sample application)
  - The query parameters of the current page are sent to the [helper method](./QBO.Shared/QboHelper.cs#L43-L82) to be handled. [<sup>WebApp</sup>](./QBO.WebApp/Controllers/ReceiverController.cs#L13-L18)
  - These are then used to get an access token from the `OAuth2Client`. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L56)
  - The next step depends on how you will store your `Access` and `Refresh` tokens. In this sample, it is just stored in a class to be written to a JSON file. [<sup>Shared</sup>](./QBO.Shared/QboHelper.cs#L84-L102) <sup>|</sup> [<sup>WebApp</sup>](./QBO.WebApp/Controllers/ReceiverController.cs#L19)

Further details are in the code and comments of each project.

<!--

## Flows

> _<ins>Note</ins> — This section only applies to the old [ASP.NET app](./OAuth2-Dotnet_UsingSDK/OAuth2-Dotnet_UsingSDK/)._

When runing the [ASP.NET sample app](./OAuth2-Dotnet_UsingSDK/OAuth2-Dotnet_UsingSDK) you are presented with three buttons. These buttons are as follows:

- **Sign In With Intuit**
  - This flow requests OpenID-only scopes. After authorizing (or if the account you are using has already authorized this app), the redirect URL will parse the JWT ID token and make an API call to the user information endpoint.

  <br>

- **Connect To QuickBooks**
  - This flow requests OAuth scopes. You will be able to make a QuickBooks API sample call (using the OAuth2 token) on the `Connected` landing page.

  <br>

- **Get App Now**
  - This flow requests both OpenID and OAuth scopes. It simulates the request that would come once a user clicks "Get App Now" on the [apps](apps.com) website after you publish your app.

--->

---

_<ins>Note</ins> — this app uses the new OAuth2Client. If you want to refer methods using standalone OAuth2 clients, please download the source code for [v1.0](https://github.com/IntuitDeveloper/OAuth2-Dotnet_UsingSDK/releases/tag/1.0) in the [Release](https://github.com/IntuitDeveloper/OAuth2-Dotnet_UsingSDK/releases) section on GitHub._

[ss1]: #
[ss2]: https://customersurveys.intuit.com/jfe/form/SV_9LWgJBcyy3NAwHc?check=Yes&checkpoint=OAuth2-Dotnet_UsingSDK&pageUrl=github
[ss3]: https://customersurveys.intuit.com/jfe/form/SV_9LWgJBcyy3NAwHc?check=No&checkpoint=OAuth2-Dotnet_UsingSDK&pageUrl=github
