using System;
using Validation;
using System.Collections.Generic;
using System.Web;
using System.IO;
using ServiceStack.Text;
using System.Collections.Specialized;

namespace OpenAuthSharp.AspNet.Clients.OAuth2
{
    public class GitHubClient : OAuth2Client
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private const string authorizationEndpoint = "https://github.com/login/oauth/authorize";
        
        /// <summary>
        /// The token endpoint.
        /// </summary>
        private const string tokenEndpoint = "https://github.com/login/oauth/access_token";
        
        /// <summary>
        /// The user endpoint.
        /// </summary>
        private const string userEndpoint = "https://api.github.com/user";
        
        /// <summary>
        /// The _app id.
        /// </summary>
        private readonly string appId;
        
        /// <summary>
        /// The _app secret.
        /// </summary>
        private readonly string appSecret;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.GitHubClient"/> class.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="appSecret">App secret.</param>
        public GitHubClient(string appId, string appSecret)
            : base("github")
        {
            Requires.NotNullOrWhiteSpace(appId, "appId");
            Requires.NotNullOrWhiteSpace(appSecret, "appSecret");
            
            this.appId = appId;
            this.appSecret = appSecret;
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
            var builder = new UriBuilder(authorizationEndpoint);
            
            builder.AppendQueryArgument("client_id", this.appId);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            
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
            var request = System.Net.WebRequest.Create(userEndpoint + "?access_token=" + accessToken);
            Dictionary<string, string> authData;
            
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                        return null;

                    NameValueCollection c = new NameValueCollection();
                    var result = JsonSerializer.DeserializeFromStream<Dictionary<string, string>>(responseStream);
                    foreach (var item in result) 
                    {
                        c.AddItemIfNotEmpty(item.Key, item.Value);
                    }
                    return c;
                }
            }
        }

        /// <summary>
        /// Queries the access token from the specified authorization code.
        /// </summary>
        /// <returns>The access token.</returns>
        /// <param name="returnUrl">Return URL.</param>
        /// <param name="authorizationCode">Authorization code.</param>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var builder = new UriBuilder(tokenEndpoint);
            
            builder.AppendQueryArgument("client_id", this.appId);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("client_secret", this.appSecret);
            builder.AppendQueryArgument("code", authorizationCode);
            
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                var data = client.DownloadString(builder.Uri);
                if (string.IsNullOrEmpty(data))
                    return null;
                
                var parsedQueryString = HttpUtility.ParseQueryString(data);
                return parsedQueryString["access_token"];
            }
        }
    }
}

