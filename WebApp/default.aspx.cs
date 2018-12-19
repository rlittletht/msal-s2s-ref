using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
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
            if (Request.IsAuthenticated && Container.AccessToken != null)
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

            if (!Request.IsAuthenticated || Container.AccessToken == null)
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
        HttpClient HttpClientCreate()
        {
            HttpClient client = new HttpClient();
            
            // we have setup our webapi to take Bearer authentication, so add our access token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Container.AccessToken);

            return client;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCallService
        	%%Qualified: WebApp._default.DoCallService

            call the WebApi       	
        ----------------------------------------------------------------------------*/
        protected void DoCallService(object sender, EventArgs e)
        {
            divOutput.InnerHtml += "DoCallService Called<br/>";

            HttpClient client = HttpClientCreate();

            divOutput.InnerHtml += GetServiceResponse(client);
            divOutput.InnerHtml += "<br/>";
        }
    }
}