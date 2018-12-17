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

            if (Request.IsAuthenticated)
            {
                btnLoginLogoff.Text = "Sign Out";
                btnLoginLogoff.Click -= DoSignInClick;
                btnLoginLogoff.Click += DoSignOutClick;
            }
            else
            {
                btnLoginLogoff.Text = "Sign In";
                btnLoginLogoff.Click -= DoSignOutClick;
                btnLoginLogoff.Click += DoSignInClick;
            }

            // if we are already authenticated, we will get an identity here; otherwise null
            m_sIdentity = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;
            m_sTenant = System.Security.Claims.ClaimsPrincipal.Current.FindFirst("iss")?.Value;
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

            if (!Request.IsAuthenticated)
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
            divOutput.InnerHtml += $"Current User: {m_sIdentity}<br/>Tenant: {m_sTenant}";
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetServiceResponse
        	%%Qualified: WebApp._default.GetServiceResponse

            call the webapi and get the response. broke this out because its nice
            to be able to collect all the async calls in the same place.

            would be really nice to use await in here, but every time i try it, i get
            threading issues, so old school task it is. 
        ----------------------------------------------------------------------------*/
        string GetServiceResponse()
        {
            HttpClient client = new HttpClient();
            Task<HttpResponseMessage> tskResponse = client.GetAsync("http://localhost/webapisvc/api/websvc/test");
                                    
            tskResponse.Wait();

            string sResponse;

            using (MemoryStream stm = new MemoryStream())
            {
                tskResponse.Result.Content.CopyToAsync(stm).Wait();
                sResponse = Encoding.UTF8.GetString(stm.ToArray());
            }

            return sResponse;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCallService
        	%%Qualified: WebApp._default.DoCallService

            call the WebApi       	
        ----------------------------------------------------------------------------*/
        protected void DoCallService(object sender, EventArgs e)
        {
            divOutput.InnerHtml += "DoCallService Called<br/>";

            divOutput.InnerHtml += GetServiceResponse();
        }
    }
}