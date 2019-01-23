# msal-s2s-ref
Reference minimal implementation of msal authentication between two web services (web app and web api or rest service)

The master branch has a very basic implementation of a WebApp that has implements signin using the Microsoft login servers.

More documentation will be added here to describe the implementation.

Branches based on master branch will augment this readme file to describe what the branch achieves. The goal is to show various stages of implementation, eventually arriving at a WebApp (client application) that calls a WebApi, both separate registered application with Azure/Microsoft. The WebApi then access the Microsoft Graph using the credentials provided by the user in the initial login.

(User logs in with WebApp, then gets a token to call the WebApi, then the WebApi uses the authentication from the WebApp to access the MS Graph on behalf of the user).

Branches:

* BasicClientWebApp - Just a web page that uses MSAL to authenticate
* BasicWebApiService - Adds a WebApi that has no authentication (but is called from the client WebApp)
* WebApiReference - Adds authentication to the WebApi (meaning there are 2 apps registered with Azure/Live)
* WebApiGraphOnBehalf - Adds consent flow to the WebApi and now the WebApi access the MS Graph on behalf of the user logged that is logged into the client WebApp
* WebAppTokenCache - This just cleans up a little bit -- gets rid of the hacky static variable used to propagate the AccessToken and instead uses a proper TokenCache stored in the SessionState.

Visit all of the various branches for the final implementation (and look at the history in those branches for a sort of "step by step" story of how its achieved.

I hope this saves someone even a little bit of the time I've been banging my head against the wall trying to figure this out :)

Cheers!
