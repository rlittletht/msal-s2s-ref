using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebApi.Controllers
{
    [Authorize]
    public class WebSvcController : ApiController
    {
        public IHttpActionResult GetTestResult(string id)
        {
            return Ok(id);
        }
    }
}
