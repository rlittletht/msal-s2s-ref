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

**WebApiTokenCache**

This branch removes the hacky static variable for communicating the AccessToken from the Authentication flow into the WebApp. We now use a TokenCache (persistend in Session). This also guards against the session being reset and an old cookie laying around.



In order to build and run this sample, you will need to register *two* applications with Azure/Live (at https://account.live.com/developers/applications/index).

First, we have an Application registered for the WebApp itself. This was created as a "Converged Application", with a single platform added "Web":
* Allow Implicit Flow = yes
* Redirect URL = http://localhost/webapp/default.aspx
		
The application has no delegated permissions (these will be explicitly requested during authentication).
	
(NOTE: Its assumed you are publishing to http://localhost/webapp, and that an app was created there in IIS.
![Screenshot for WebApp registration](https://github.com/rlittletht/msal-s2s-ref/blob/WebApiGraphOnBehalfReference/WebAppRegistration_Clipping.png)

Next, since the point of this example is to show two separate entities communicating (two different applications), we also have a WebApi registered. This is also created as a "Converged Application", with a single platform added, "WebApi":
	
* There is a set of preauthorized applications, add the application ID from your WebApp here and give it a score of access_as_user.
* Also leave the default graph permission "User.Read" for delegated permissions.

In order to grant consent to the WebApi, you have to get an AuthorizationUrl. In order to get an AuthorizationUrl, you have to have a redirectUri. When you register a WebApi, you don't have an option to create a redirectUri. So, you have to go to your WebApi registration and ADD a Web platform with a redirectUri specifically to allow consent to be granted. (This doesn't seem like a great way to do it, but I'm getting desperate and can't figure out another way).  Also, since you are going to tell the ConfidentialClientApplication that it should redirect to a particular URI, we need to make sure there's really a page there to redirect to.

Here is what the WebApi registration looks like with both the Web and Web API platforms added:
![Screenshot for WebApp registration](https://github.com/rlittletht/msal-s2s-ref/blob/WebApiGraphOnBehalfReference/WebApiRegistration_Clipping.png)





