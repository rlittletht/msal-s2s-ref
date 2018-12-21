using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

[assembly: OwinStartup(typeof(WebApp.Startup))]

namespace WebApp
{
    // we need some place to stash the accesstoken, so when we get it during authentication, the application can access it
    // (NOTE: This is a cheap hack to share the token. Don't actually do this since you will have one instance of this
    // variable per app, which means all users will share it. Not a good plan.
    public static class Container
    {
        public static string AccessToken { get; set; }
    }

    public class Startup
    {
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        string clientId = System.Configuration.ConfigurationManager.AppSettings["ClientId"];

        // The AppKey is used to create a ConfidentialClientApplication in order to exchange an auth token for an access token
        string appKey = System.Configuration.ConfigurationManager.AppSettings["AppKey"];

        // When requesting scopes during login (and token exchange), we need to specify that we want to access
        // our webapi, since this is a different application. We get this scope from the WebApi application registration page.
        string scopeWebApi = System.Configuration.ConfigurationManager.AppSettings["WebApiScope"];

        // RedirectUri is the URL where the user will be redirected to after they sign in.
        string redirectUri = System.Configuration.ConfigurationManager.AppSettings["RedirectUri"];

        // Tenant is the tenant ID (e.g. contoso.onmicrosoft.com, or 'common' for multi-tenant)
        static string tenant = System.Configuration.ConfigurationManager.AppSettings["Tenant"];

        // Authority is the URL for authority, composed by Azure Active Directory v2 endpoint and the tenant name (e.g. https://login.microsoftonline.com/contoso.onmicrosoft.com/v2.0)
        string authority = String.Format(System.Globalization.CultureInfo.InvariantCulture, System.Configuration.ConfigurationManager.AppSettings["Authority"], tenant);


        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    // Sets the ClientId, authority, RedirectUri as obtained from web.config
                    ClientId = clientId,
                    Authority = authority,
                    RedirectUri = redirectUri,
                    // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it is using the home page
                    PostLogoutRedirectUri = redirectUri,
                    // our scope is more complicated now as we want to get permission for the webapi too
                    Scope = $"{scopeWebApi} {OpenIdConnectScope.OpenIdProfile} offline_access user.read",
                    // ResponseType is set to request the id_token - which contains basic information about the signed-in user, as well as Code which
                    // is required to get an auth code in OnAuthorizationCodeReceived
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
                    // To only allow users from a single organizations, set ValidateIssuer to true and 'tenant' setting in web.config to the tenant name
                    // To allow users from only a list of specific organizations, set ValidateIssuer to true and use ValidIssuers parameter 
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false
                    },
                    // OpenIdConnectAuthenticationNotifications configures OWIN to send notification of failed authentications to OnAuthenticationFailed method
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = OnAuthenticationFailed,
                        AuthorizationCodeReceived = OnAuthorizationCodeReceived
                    }

                }
            );
        }

        /// <summary>
        /// take the authorization token we get from login and exchange that for an access token
        /// This is only called once per login, and only during authentication. If the session
        /// already has an idtoken, then this won't get called. This means that there will be times
        /// (especially during debugging) when you will have an idtoken, but you won't have the
        /// opportunity to exchange the auth code for an access token. To account for that,
        /// require sign-in whenever you don't have the cached access token.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            var code = context.Code;
            
            ConfidentialClientApplication cca =
                new ConfidentialClientApplication(clientId, redirectUri, new ClientCredential(appKey), null, null);

            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, new string[] { scopeWebApi });
            
            Container.AccessToken = result.AccessToken;
        }
        /// <summary>
        /// Handle failed authentication requests by redirecting the user to the home page with an error in the query string
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            context.HandleResponse();
            context.Response.Redirect("/?errormessage=FAILED:" + context.Exception.Message);
            return Task.FromResult(0);
        }
    }
}