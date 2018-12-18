# msal-s2s-ref
Reference minimal implementation of msal authentication between two web services (web app and web api or rest service)

The master branch has a very basic implementation of a WebApp that has implements signin using the Microsoft login servers.

More documentation will be added here to describe the implementation.

Branches based on master branch will augment this readme file to describe what the branch achieves. The goal is to show various stages of implementation, eventually arriving at a WebApp (client application) that calls a WebApi, both separate registered application with Azure/Microsoft. The WebApi then access the Microsoft Graph using the credentials provided by the user in the initial login.

(User logs in with WebApp, then gets a token to call the WebApi, then the WebApi uses the authentication from the WebApp to access the MS Graph on behalf of the user).
