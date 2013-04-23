using System;
using Validation;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization.Json;
using System.Web;

namespace OpenAuthSharp.AspNet.Clients.OAuth2
{
    public sealed class FacebookClient : OAuth2Client
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        public const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";

        /// <summary>
        /// The token endpoint.
        /// </summary>
        public const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";

        /// <summary>
        /// The app identifier.
        /// </summary>
        private readonly string appId;

        /// <summary>
        /// The app secret.
        /// </summary>
        private readonly string appSecret;

        /// <summary>
        /// The scope.
        /// </summary>
        private readonly string scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.FacebookClient"/> class.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="appSecret">App secret.</param>
        public FacebookClient(string appId, string appSecret)
            : this(appId, appSecret, "email")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.FacebookClient"/> class.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="appSecret">App secret.</param>
        /// <param name="scope">Scope.</param>
        public FacebookClient(string appId, string appSecret, string scope)
            : base("facebook")
        {
            Requires.NotNullOrWhiteSpace(appId, "appId");
            Requires.NotNullOrWhiteSpace(appSecret, "appSecret");
            Requires.NotNullOrWhiteSpace(scope, "scope");

            this.appId = appId;
            this.appSecret = appSecret;
            this.scope = scope;
        }

        /// <summary>
        /// Gets the full url pointing to the login page for this client. 
        /// The url should include the specified return url so that when 
        /// the login completes, user is redirected back to that url.
        /// </summary>
        /// <returns>The service login URL.</returns>
        /// <param name="returnUrl">Return URL.</param>
        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var builder = new UriBuilder(AuthorizationEndpoint);
            builder.AppendQueryArgs(new Dictionary<string, string>()
            {
                { "client_id", this.appId },
                { "redirect_uri", returnUrl.AbsoluteUri },
                { "scope", this.scope }
            });

            return builder.Uri;
        }

        /// <summary>
        /// Given the access token, gets the logged-in user's data. 
        /// The returned dictionary must include two keys 'id', and 'username'.
        /// </summary>
        /// <param name="accessToken">The access token of the current user.</param>
        /// <returns>A dictionary contains key-value pairs of user data</returns>
        protected override NameValueCollection GetUserData(string accessToken) 
        {
            FacebookMe graphData = null;
            string requestUrl = "https://graph.facebook.com/me?access_token=" + UriHelper.EscapeUriDataStringRfc3986(accessToken);
            var request = System.Net.WebRequest.Create(requestUrl);
            using (var response = request.GetResponse()) 
            {
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                       var serializer = new DataContractJsonSerializer(typeof(FacebookMe));
                        graphData = (FacebookMe)serializer.ReadObject(responseStream);
                    }
                }
            }
            
            // this dictionary must contains 
            var userData = new NameValueCollection();
            if (graphData != null)
            {
                userData.AddItemIfNotEmpty("id", graphData.Id);
                userData.AddItemIfNotEmpty("username", graphData.Email);
                userData.AddItemIfNotEmpty("name", graphData.Name);
                userData.AddItemIfNotEmpty("link", graphData.Link == null ? null : graphData.Link.AbsoluteUri);
                userData.AddItemIfNotEmpty("gender", graphData.Gender);
                userData.AddItemIfNotEmpty("birthday", graphData.Birthday);
            }
            return userData;
        }

        /// <summary>
        /// Queries the access token from the specified authorization code.
        /// </summary>
        /// <returns>The access token.</returns>
        /// <param name="returnUrl">Return URL.</param>
        /// <param name="authorizationCode">Authorization code.</param>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode) 
        {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(TokenEndpoint);
            builder.AppendQueryArgs(new Dictionary<string, string>()
            {   
                { "client_id", this.appId },
                { "redirect_uri", UriHelper.NormalizeHexEncoding(returnUrl.AbsoluteUri) },
                { "client_secret", this.appSecret },
                { "code", authorizationCode },
                { "scope", this.scope }
            });
            
            using (System.Net.WebClient client = new System.Net.WebClient()) 
            {
                string data = client.DownloadString(builder.Uri);
                if (string.IsNullOrEmpty(data))
                    return null;

                var parsedQueryString = HttpUtility.ParseQueryString(data);
                return parsedQueryString["access_token"];
            }
        }
    }
}

