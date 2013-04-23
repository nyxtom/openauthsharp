using System;
using Validation;
using System.Web;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace OpenAuthSharp.AspNet.Clients.OAuth2
{
    public abstract class OAuth2Client : IAuthenticationClient
    {
        private readonly string providerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.OAuth2Client"/> class.
        /// </summary>
        /// <param name="providerName">Provider name.</param>
        protected OAuth2Client(string providerName)
        {
            Requires.NotNullOrWhiteSpace(providerName, "providerName");
            this.providerName = providerName;
        }

        /// <summary>
        /// Gets the name of the provider which provides authentication service.
        /// </summary>
        /// <value>The name of the provider.</value>
        public string ProviderName { get { return this.providerName; } }

        /// <summary>
        /// Requests the authentication.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="returnUrl">Return URL.</param>
        public virtual void RequestAuthentication(HttpContextBase context, Uri returnUrl)
        {
            Requires.NotNull(context, "context");
            Requires.NotNull(returnUrl, "returnUrl");

            string redirectUrl = this.GetServiceLoginUrl(returnUrl).AbsoluteUri;
            context.Response.Redirect(redirectUrl, true);
        }

        /// <summary>
        /// Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="context">The context of the current request.</param>
        /// <returns>The authentication.</returns>
        public AuthenticationResult VerifyAuthentication(HttpContextBase context)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnPageUrl">The return URL which should match the value passed to RequestAuthentication() method.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// An instance of <see cref="AuthenticationResult" /> containing authentication result.
        /// </returns>
        public virtual AuthenticationResult VerifyAuthentication(HttpContextBase context, Uri returnPageUrl)
        {
            Requires.NotNull(context, "context");

            string code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
                return AuthenticationResult.Failed;

            string accessToken = this.QueryAccessToken(returnPageUrl, code);
            if (accessToken == null)
                return AuthenticationResult.Failed;

            var userData = this.GetUserData(accessToken);
            if (userData == null)
                return AuthenticationResult.Failed;

            // Some oAuth providers do not return value for the 'username' attribute. 
            // In that case, try the 'name' attribute. If it's still unavailable, fall back to 'id'
            string id = userData["id"];
            string name = userData["username"] ?? userData["name"] ?? id;

            // add the access token to the user data dictionary just in case page developers want to use it
            userData["accesstoken"] = accessToken;

            return new AuthenticationResult(true, this.ProviderName, id, name, userData);
        }

        /// <summary>
        /// Gets the full url pointing to the login page for this client. 
        /// The url should include the specified return url so that when 
        /// the login completes, user is redirected back to that url.
        /// </summary>
        /// <returns>The service login URL.</returns>
        /// <param name="returnUrl">Return URL.</param>
        protected abstract Uri GetServiceLoginUrl(Uri returnUrl);

        /// <summary>
        /// Given the access token, gets the logged-in user's data. 
        /// The returned dictionary must include two keys 'id', and 'username'.
        /// </summary>
        /// <param name="accessToken">The access token of the current user.</param>
        /// <returns>A dictionary contains key-value pairs of user data</returns>
        protected abstract NameValueCollection GetUserData(string accessToken);

        /// <summary>
        /// Queries the access token from the specified authorization code.
        /// </summary>
        /// <returns>The access token.</returns>
        /// <param name="returnUrl">Return URL.</param>
        /// <param name="authorizationCode">Authorization code.</param>
        protected abstract string QueryAccessToken(Uri returnUrl, string authorizationCode);
    }
}

