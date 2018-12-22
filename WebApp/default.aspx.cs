using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;

namespace WebApp
{
    public partial class _default : System.Web.UI.Page
    {
        private string m_sIdentity;
        private string m_sTenant;

        /*----------------------------------------------------------------------------
        	%%Function: IsSignedIn
        	%%Qualified: WebApp._default.IsSignedIn
        	
            return true if the signin process is complete -- this includes making 
            sure there is an entry for this userid in the TokenCache
        ----------------------------------------------------------------------------*/
        bool IsSignedIn()
        {
            return Request.IsAuthenticated && FTokenCachePopulated();
        }

        /*----------------------------------------------------------------------------
        	%%Function: HandleAuth
        	%%Qualified: WebApp._default.HandleAuth

            Handle authentication for the page, including setting up the controls
            on the page to reflect the authentication state.
        ----------------------------------------------------------------------------*/
        void HandleAuth()
        {
            // if the request is authenticated, then we are authenticated and have information
            // (make sure we have an access token as well; if not, then we have cached idtoken
            // but haven't made the exchange for an access token)
            if (IsSignedIn())
            {
                btnLoginLogoff.Text = "Sign Out";
                btnLoginLogoff.Click -= DoSignInClick;
                btnLoginLogoff.Click += DoSignOutClick;

                m_sIdentity = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;
                m_sTenant = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("iss")?.Value;
            }
            else
            {
                btnLoginLogoff.Text = "Sign In";
                btnLoginLogoff.Click -= DoSignOutClick;
                btnLoginLogoff.Click += DoSignInClick;
            }

            // if we are already authenticated, we will get an identity here; otherwise null
            if (m_sTenant != null)
            {
                Regex rex = new Regex("https://login.microsoftonline.com/([^/]*)/");

                m_sTenant = rex.Match(m_sTenant).Groups[1].Value;
                if (m_sTenant == "9188040d-6c67-4c5b-b112-36a304b66dad")
                    m_sTenant = "Microsoft Consumer";
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoSignInClick
        	%%Qualified: WebApp._default.DoSignInClick
        ----------------------------------------------------------------------------*/
        public void DoSignInClick(object sender, EventArgs args)
        {
            string sReturnAddress = "/webapp/default.aspx";

            if (!IsSignedIn())
            {
                HttpContext.Current.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = sReturnAddress },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoSignOutClick
        	%%Qualified: WebApp._default.DoSignOutClick
        ----------------------------------------------------------------------------*/
        public void DoSignOutClick(object sender, EventArgs args)
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
        }

        /*----------------------------------------------------------------------------
        	%%Function: Page_Load
        	%%Qualified: WebApp._default.Page_Load

            setup the page for authentication and update the current logged in state       	
        ----------------------------------------------------------------------------*/
        protected void Page_Load(object sender, EventArgs e)
        {
            HandleAuth();
            if (!IsPostBack)
                divOutput.InnerHtml += $"<br/>Current User: {m_sIdentity}<br/>Tenant: {m_sTenant}<br/>";
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetServiceResponse
        	%%Qualified: WebApp._default.GetServiceResponse

            call the webapi and get the response. broke this out because its nice
            to be able to collect all the async calls in the same place.

            would be really nice to use await in here, but every time i try it, i get
            threading issues, so old school task it is. 
        ----------------------------------------------------------------------------*/
        string GetServiceResponse(HttpClient client)
        {
            Task<HttpResponseMessage> tskResponse = client.GetAsync("http://localhost/webapisvc/api/websvc/test");
                                    
            tskResponse.Wait();


            if (tskResponse.Result.StatusCode == HttpStatusCode.Unauthorized)
            {
                // check to see if we just need to get consent
                foreach (AuthenticationHeaderValue val in tskResponse.Result.Headers.WwwAuthenticate)
                {
                    if (val.Scheme == "need-consent")
                    {
                        // the parameter is the URL the user needs to visit in order to grant consent. Construct
                        // a link to report to the user (here we can inject HTML into our DIV to make
                        // this easier).

                        // This is not the best user experience (they end up in a new tab to grant consent, 
                        // and that tab is orphaned... but for now it makes it clear how a flow *could*
                        // work)
                        return
                            $"The current user has not given the WebApi consent to access the Microsoft Graph on their behalf. <a href='{val.Parameter}' target='_blank'>Click Here</a> to grant consent.";
                    }
                }
            }

            if (tskResponse.Result.StatusCode != HttpStatusCode.OK)
                return $"Service Call Failed: {tskResponse.Result.StatusCode}";

            string sResponse;

            using (MemoryStream stm = new MemoryStream())
            {
                tskResponse.Result.Content.CopyToAsync(stm).Wait();
                sResponse = Encoding.UTF8.GetString(stm.ToArray());
            }

            // don't allow the WebApi to return raw HTML markup
            return Server.HtmlEncode(sResponse);
        }

        /*----------------------------------------------------------------------------
        	%%Function: HttpClientCreate
        	%%Qualified: WebApp._default.HttpClientCreate
            
            setup the http client for the webapi calls we're going to make
        ----------------------------------------------------------------------------*/
        HttpClient HttpClientCreate(string sAccessToken)
        {
            HttpClient client = new HttpClient();
            
            // we have setup our webapi to take Bearer authentication, so add our access token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sAccessToken);

            return client;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetUserId
        	%%Qualified: WebApp._default.GetUserId
        	
            convenient way to get the current user id (so we can get to the right
            TokenCache)
        ----------------------------------------------------------------------------*/
        string GetUserId()
        {
            return ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetContextBase
        	%%Qualified: WebApp._default.GetContextBase
        	
            get the HttpContextBase we can use for the SessionState (which is needed
            by our TokenCache implemented by MSALSessionCache
        ----------------------------------------------------------------------------*/
        HttpContextBase GetContextBase()
        {
            return Context.GetOwinContext().Environment["System.Web.HttpContextBase"] as HttpContextBase;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetAccessToken
        	%%Qualified: WebApp._default.GetAccessToken

            Get an access token for accessing the WebApi. This will use 
            AcquireTokenSilentAsync to get the token. Since this is using the 
            same tokencache as we populated when the user logged in, we will
            get the access token from that cache. 
        ----------------------------------------------------------------------------*/
        string GetAccessToken()
        {
            if (!IsSignedIn())
                return null;

            // Retrieve the token with the specified scopes
            var scopes = new string[] {Startup.scopeWebApi};
            string userId = GetUserId();
            TokenCache tokenCache = new MSALSessionCache(userId, GetContextBase()).GetMsalCacheInstance();
            ConfidentialClientApplication cca = new ConfidentialClientApplication(Startup.clientId, Startup.authority, Startup.redirectUri, new ClientCredential(Startup.appKey), tokenCache, null);

            Task<IEnumerable<IAccount>> tskAccounts = cca.GetAccountsAsync();
            tskAccounts.Wait();

            IAccount account = tskAccounts.Result.FirstOrDefault();

            Task<AuthenticationResult> tskResult = cca.AcquireTokenSilentAsync(scopes, account, Startup.authority, false);

            tskResult.Wait();
            return tskResult.Result.AccessToken;
        }

        /*----------------------------------------------------------------------------
        	%%Function: FTokenCachePopulated
        	%%Qualified: WebApp._default.FTokenCachePopulated
        	
        	return true if our TokenCache has been populated for the current 
            UserId.  Since our TokenCache is currently only stored in the session, 
            if our session ever gets reset, we might get into a state where there
            is a cookie for auth (and will let us automatically login), but the
            TokenCache never got populated (since it is only populated during the
            actual authentication process). If this is the case, we need to treat
            this as if the user weren't logged in. The user will SignIn again, 
            populating the TokenCache.
        ----------------------------------------------------------------------------*/
        bool FTokenCachePopulated()
        {
            return MSALSessionCache.CacheExists(GetUserId(), GetContextBase());
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCallService
        	%%Qualified: WebApp._default.DoCallService

            call the WebApi       	
        ----------------------------------------------------------------------------*/
        protected void DoCallService(object sender, EventArgs e)
        {
            divOutput.InnerHtml += "DoCallService Called<br/>";

            string sAccessToken = GetAccessToken();
            HttpClient client = HttpClientCreate(sAccessToken);

            divOutput.InnerHtml += GetServiceResponse(client);
            divOutput.InnerHtml += "<br/>";
        }
    }
}