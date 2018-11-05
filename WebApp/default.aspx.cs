using System;
using System.Collections.Generic;
using System.Linq;
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
        }

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

        public void DoSignOutClick(object sender, EventArgs args)
        {
            HttpContext.Current.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType,
                CookieAuthenticationDefaults.AuthenticationType);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            HandleAuth();
        }

        protected void DoCallService(object sender, EventArgs e)
        {
            divOutput.InnerHtml += "DoCallService Called<br/>";
        }
    }
}