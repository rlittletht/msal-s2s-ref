# msal-s2s-ref
Reference minimal implementation of msal authentication between two web services (web app and web api or rest service)

The master branch has a very basic implementation of a WebApp that has implements signin using the Microsoft login servers.

More documentation will be added here to describe the implementation.

Branches based on master branch will augment this readme file to describe what the branch achieves. The goal is to show various stages of implementation, eventually arriving at a WebApp (client application) that calls a WebApi, both separate registered application with Azure/Microsoft. The WebApi then access the Microsoft Graph using the credentials provided by the user in the initial login.

(User logs in with WebApp, then gets a token to call the WebApi, then the WebApi uses the authentication from the WebApp to access the MS Graph on behalf of the user).

**BasicWebApiService**

This branch implements a basic, unauthenticated WebApi. In the end, you have a client app that can call the WebApi, with no authentication.

**WebApiReference**

This breanch implements an authenticated WebApi. In the eyes of Azure (account.live.com/developers/applications/index), the WebApp and the WebApi are *separate* converged applications. The WebApp handles authentication/login for the user, gets an access token for the WebApi (including getting consent from the user to access the WebApi as the user), and adds the authentication to the http request being sent to the webapi. The WebApi properly establishes itself as an application of type WebApi, and validates that a valid access token was sent to the WebApi.

At this point, the WebApi is *protected*. This is a pretty good stopping point for most people. (Actually, most people could also just have a WebApp and WebApi as the same converged application in Azure as well).

**WebApiGraphOnBehalfReference**

This branch implements the WebApi calling the Microsoft Graph on behalf of the user that is accessing the WebApi.

