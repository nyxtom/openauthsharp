using System;
using Validation;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using ServiceStack.Text;
using System.IO;

namespace OpenAuthSharp.AspNet.Clients.OAuth2
{
    public class GoogleClient : OAuth2Client
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/auth";
        
        /// <summary>
        /// The token endpoint.
        /// </summary>
        private const string tokenEndpoint = "https://accounts.google.com/o/oauth2/token";
        
        /// <summary>
        /// The user info endpoint.
        /// </summary>
        private const string userEndpoint = "https://www.googleapis.com/oauth2/v1/userinfo";
        
        /// <summary>
        /// The base uri for scopes.
        /// </summary>
        private const string scopeUri = "https://www.googleapis.com/auth/";
        
        /// <summary>
        /// The _app id.
        /// </summary>
        private readonly string appId;
        
        /// <summary>
        /// The _app secret.
        /// </summary>
        private readonly string appSecret;
        
        /// <summary>
        /// The requested scopes.
        /// </summary>
        private readonly string[] requestedScopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.GoogleClient"/> class.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="appSecret">App secret.</param>
        public GoogleClient(string appId, string appSecret) 
            : this(appId, appSecret, new[] { "userinfo.profile", "userinfo.email" }) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAuthSharp.AspNet.Clients.OAuth2.GoogleClient"/> class.
        /// </summary>
        /// <param name="appId">App identifier.</param>
        /// <param name="appSecret">App secret.</param>
        /// <param name="requestedScopes">Requested scopes.</param>
        public GoogleClient(string appId, string appSecret, params string[] requestedScopes)
            : base("google")
        {
            Requires.NotNullOrWhiteSpace(appId, "appId");
            Requires.NotNullOrWhiteSpace(appSecret, "appSecret");
            Requires.NotNullEmptyOrNullElements(requestedScopes, "requestedScopes");

            this.appId = appId;
            this.appSecret = appSecret;
            this.requestedScopes = requestedScopes;
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
            var scopes = requestedScopes.Select(x => !x.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? scopeUri + x : x);
            UriBuilder builder = new UriBuilder(authorizationEndpoint);
            builder.AppendQueryArgs(new Dictionary<string, string>()
            {
                { "response_type", "code" },
                { "client_id", this.appId },
                { "scope", string.Join(" ", scopes) },
                { "redirect_uri", returnUrl.GetLeftPart(UriPartial.Path) },
                { "state", returnUrl.Query.Substring(1) },
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
            var uri = new Uri(userEndpoint + "?access_token=" + accessToken);
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            using (var webResponse = webRequest.GetResponse())
            {
                using (var stream = webResponse.GetResponseStream())
                {
                    if (stream == null)
                        return null;

                    NameValueCollection c = new NameValueCollection();
                    var result = JsonSerializer.DeserializeFromStream<Dictionary<string, string>>(stream);
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
            var postData = System.Web.HttpUtility.ParseQueryString(string.Empty);
            postData.Add(new NameValueCollection()
            {
                { "grant_type", "authorization_code" },
                { "code", authorizationCode },
                { "client_id", this.appId },
                { "client_secret", this.appSecret },
                { "redirect_uri", returnUrl.GetLeftPart(UriPartial.Path) },
            });
            
            var webRequest = (HttpWebRequest)WebRequest.Create(tokenEndpoint);
            
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            
            using (var s = webRequest.GetRequestStream())
                using (var sw = new StreamWriter(s))
                    sw.Write(postData.ToString());
            
            using (var webResponse = webRequest.GetResponse())
            {
                var responseStream = webResponse.GetResponseStream();
                if (responseStream == null)
                    return null;
                
                using (var reader = new StreamReader(responseStream))
                {
                    var response = reader.ReadToEnd();
                    var json = JsonObject.Parse(response);
                    string accessToken = string.Empty;
                    json.TryGetValue("access_token", out accessToken);
                    return accessToken;
                }
            }
        }

        /// <summary>
        /// Google requires that all return data be packed into a "state" parameter.
        /// Check if authentication succeeded after user is redirected back from the service provider.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="returnPageUrl">The return URL which should match the value passed to RequestAuthentication() method.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The authentication.</returns>
        public override AuthenticationResult VerifyAuthentication(System.Web.HttpContextBase context, Uri returnPageUrl)
        {
            var stateString = System.Web.HttpUtility.UrlDecode(context.Request.QueryString["state"]);
            if (stateString == null || !stateString.Contains("__provider__=google"))
                return base.VerifyAuthentication(context, returnPageUrl);
            
            var q = System.Web.HttpUtility.ParseQueryString(stateString);
            q.Add(context.Request.QueryString);
            q.Remove("state");
            
            context.RewritePath(context.Request.Path + "?" + q);
            return base.VerifyAuthentication(context, returnPageUrl);
        }
    }
}

