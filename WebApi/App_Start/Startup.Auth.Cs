using System.Configuration;
using Owin;
using Microsoft.Owin.Security.Jwt;
using Microsoft.Owin.Security.OAuth;
using Microsoft.IdentityModel.Tokens;

namespace WebApi
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ClientId"];
        public static string appKey = ConfigurationManager.AppSettings["AppKey"];
        public static string clientIdAudience = ConfigurationManager.AppSettings["ValidClientAudience"];

        public void ConfigureAuth(IAppBuilder app)
        {
            // NOTE: The usual WindowsAzureActiveDirectoryBearerAuthentication middleware uses a
            // metadata endpoint which is not supported by the v2.0 endpoint.  Instead, this 
            // OpenIdConnectSecurityTokenProvider implementation can be used to fetch & use the OpenIdConnect
            // metadata document - which for the v2 endpoint is https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration


            TokenValidationParameters tvps = new TokenValidationParameters
            { 
                // This is where you specify that your API only accepts tokens from its own clients
                ValidAudiences = new [] { clientId, clientIdAudience },
                
                // Change below to 'true' if you want this Web API to accept tokens issued to one Azure AD tenant only (single-tenant)
                ValidateIssuer = false,
            };

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JwtFormat(
                    tvps,
                    new OpenIdConnectCachingSecurityTokenProvider("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration")
                )
                
            });
        }
    }
}
