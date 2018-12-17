using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(WebApi.Startup))]

namespace WebApi
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Invoke the ConfigureAuth method, which will set up
            // the OWIN authentication pipeline using OAuth 2.0

            ConfigureAuth(app);
        }

        public void ConfigureServices()
        {
        }
    }
}
