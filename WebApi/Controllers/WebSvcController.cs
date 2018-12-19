using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace WebApi.Controllers
{
    [Authorize]
    public class WebSvcController : ApiController
    {
        string GetCurrentClaimsDescription()
        {
            string sEmail = ClaimsPrincipal.Current.FindFirst("preferred_username")?.Value;
            string sScopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;

            return $"Provided Claims: Email({sEmail}), Scope({sScopeClaim})";
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
            string sClaimsDescription = GetCurrentClaimsDescription();

            string sReturn = $"Context: [{sClaimsDescription}]; Return: {id}";

            return Ok(sReturn);
        }
    }
}
