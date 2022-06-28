### What are OAuth2 Tokens?

This document briefly covers what each **Token** is used for in OAuth 2.0 for Quickbooks Online. For more information check out the [Intuit official documentation](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization/faq).

---

#### Client ID
  - The **Client ID** is the ID of the app trying to get authentication.
    It is required so our servers know what authorization URL to send back to your application.

<br>

#### Client Secret
  - The **Client Secret** is, in essence, your app's password. It is required as an extra layer of security over your data.

<br>

#### Redirect URL
  - The **Redirect URL** is where Quickbooks puts your authentication code after authorizing your Quickbooks Online app. This URL is typically your host which will handle the query data from our server.

<br>

#### Environment
  - The **Environment** is the context used in the current session. This can either be `sandbox` for testing with sandbox data, or `production` for production use with your company's real data.

<br>

#### Access Token
  - The **Access Token** is the final token received in the OAuth 2.0 process. This token is required to read/write data to your Quickbooks Online account and is only valid for <ins>one hour</ins>.

<br>

#### Refresh Token
  - The **Refresh Token** is used to refresh your **Access Token** without having to re-authenticate your app. This token is valid for 100 days before you need to re-authenticate.

<br>

#### Realm ID
  - The **Realm ID** is the identifier of your company. It is required along with your access token to read/write data to your Quickbooks Oline account.