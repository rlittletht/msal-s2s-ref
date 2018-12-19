using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Net.Http.Headers;
using System.Windows.Forms.VisualStyles;
using Microsoft.Identity.Client;

namespace WebApi.Controllers
{
    [Authorize]
    public class WebSvcController : ApiController
    {
        /*----------------------------------------------------------------------------
        	%%Function: GetCurrentClaimsDescription
        	%%Qualified: WebApi.Controllers.WebSvcController.GetCurrentClaimsDescription

        	build a string describing some information about the auth information 
            provided to the webapi. Namely, the email and scope claims.

            this does not call the MS graph, it just uses the information provided
            by the caller
        ----------------------------------------------------------------------------*/
        string GetCurrentClaimsDescription()
        {
            string sEmail = ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;
            string sScopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;

            return $"Provided Claims: Email({sEmail}), Scope({sScopeClaim})";
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetAuthenticationUrlForConsent
        	%%Qualified: WebApi.Controllers.WebSvcController.GetAuthenticationUrlForConsent

        	get the url that the user must visit in order to grant consent for the 
            WebApi to access the graph on the user's behalf.
        ----------------------------------------------------------------------------*/
        string GetAuthenticationUrlForConsent(ConfidentialClientApplication cca, string []graphScopes)
        {
            // if this throws, just let it throw
            Task<Uri> tskUri = cca.GetAuthorizationRequestUrlAsync(graphScopes, "", null);
            tskUri.Wait();

            return tskUri.Result.AbsoluteUri;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetUserAssertion
        	%%Qualified: WebApi.Controllers.WebSvcController.GetUserAssertion

        	build a UserAssertion that we will use to get our new token. This 
            is constructed from the BootstrapContext that has been preserved for
            us in the claims. 
        ----------------------------------------------------------------------------*/
        UserAssertion BuildUserAssertion()
        {
            string bootstrapContext = (string) ClaimsPrincipal.Current.Identities.First().BootstrapContext;

            if (bootstrapContext == null)
            {
                throw new WebApiException("Bootstrap context is null");
            }

            JwtSecurityToken tok = new JwtSecurityToken(bootstrapContext);

            return new UserAssertion(tok.RawData, "urn:ietf:params:oauth:grant-type:jwt-bearer");
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetAccessTokenForGraph
        	%%Qualified: WebApi.Controllers.WebSvcController.GetAccessTokenForGraph

            to call the MS graph, we have to have an access token that *our* application
            can use to access the graph. The access token we were given by the caller
            was created by the *calling application*. So, we will exchange that token
            for another token using AcquireTokenOnBehalfOfAsync. 
        ----------------------------------------------------------------------------*/
        string GetAccessTokenForGraph()
        {
            // we need to create a cca in order to get a token for the graph
            // (this cca will use our WebApi clientId and secret
            // (be sure to use the redirectUri here that matches the Web platform
            // that you added to the WebApi.  We will deal with potentially redirecting
            // that back to the web client later.
            ConfidentialClientApplication cca =
                new ConfidentialClientApplication(Startup.clientId,
                    "http://localhost/webapisvc/auth.aspx",
                    new ClientCredential(Startup.appKey), null, null);

            // ask for access to the graph defaults that our WebApi requests
            // (which should have been specified during registration to at least 
            // be User.Read)
            string[] graphScopes = {"https://graph.microsoft.com/.default"};

            UserAssertion userAssertion = BuildUserAssertion();

            Task<AuthenticationResult> tskAuthResult = null;

            try
            {
                tskAuthResult = cca.AcquireTokenOnBehalfOfAsync(graphScopes, userAssertion);
                tskAuthResult.Wait();
            }
            catch (Exception exc)
            {
                if (exc is Microsoft.Identity.Client.MsalUiRequiredException
                    || exc.InnerException is Microsoft.Identity.Client.MsalUiRequiredException)
                {
                    // We failed because we don't have consent from the user -- even
                    // though they consented for the WebApp application to access
                    // the graph, they also need to consent to this WebApi to grant permission
                    string sUrl = GetAuthenticationUrlForConsent(cca, graphScopes);

                    throw new WebApiExceptionNeedConsent(sUrl, "WebApi does not have consent from the user to access the graph on behalf of the user", exc);
                }

                // otherwise, just rethrow
                throw;
            }
            return tskAuthResult.Result.AccessToken;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetUserInfoFromGraph
        	%%Qualified: WebApi.Controllers.WebSvcController.GetUserInfoFromGraph
        	
            call the MS graph and get the user info for the user that we have
            an access token for.
        ----------------------------------------------------------------------------*/
        string GetUserInfoFromGraph()
        {
            string sRet = "";

            try
            {
                string sAccessToken = null;

                sAccessToken = GetAccessTokenForGraph();

                // Call the Graph API and retrieve the user's profile.  This is just a standard
                // rest api call, with the bearer token set to the access token we just got.
                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sAccessToken);
                Task<HttpResponseMessage> tskResponse = client.SendAsync(request);
                tskResponse.Wait();

                HttpResponseMessage response = tskResponse.Result;

                if (response.IsSuccessStatusCode)
                {
                    Task<string> tskString = response.Content.ReadAsStringAsync();
                    tskString.Wait();

                    sRet = $"jsonMe: {tskString.Result}";
                }
                else
                {
                    sRet = $"FAILED to call Graph: [{response.StatusCode}: {response.ReasonPhrase}";
                }
            }
            catch (WebApiExceptionNeedConsent)
            {
                throw; // allow this to propagate up -- we will handle this at the top level
            }
            catch (Exception exc)
            {
                return $"FAILED to acquire token for graph: {exc.Message}";
            }

            return sRet;
        }
        /*----------------------------------------------------------------------------
        	%%Function: GetTestResult
        	%%Qualified: WebApi.Controllers.WebSvcController.GetTestResult

            The goal here is to prepend the given id with information about the user
            (both acquired by the token used to call this WebApi, as well as by
            calling the MS Graph to get profile information about the user that
            called this WebApi)
        	
        ----------------------------------------------------------------------------*/
        public IHttpActionResult GetTestResult(string id)
        {
            string sReturn = null;

            try
            {
                string sClaimsDescription = GetCurrentClaimsDescription();
                string sGraphInfo = GetUserInfoFromGraph();

                sReturn = $"Context: [{sClaimsDescription}]; GraphInfo: [{sGraphInfo}]; Return: {id}";
            }
            catch (WebApiExceptionNeedConsent exc)
            {
                // this exception means we need to return an authentication error

                // NOTE: this is probably a horrible abuse of the Unauthorized() error code. I'd love
                // to know a better way to flow this consent URL back to the calling client!
                return Unauthorized(new AuthenticationHeaderValue("need-consent", exc.AuthorizationUrl));
            }
            // any other exceptions should just go unhandled (they should have been handled under us anyway

            return Ok(sReturn);
        }
    }
}
