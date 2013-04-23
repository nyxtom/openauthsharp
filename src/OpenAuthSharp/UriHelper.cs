using System;
using System.Collections.Generic;
using Validation;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Specialized;
using System.Web;

namespace OpenAuthSharp
{
    internal static class UriHelper
    {
        /// <summary>
        /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
        /// </summary>
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };

        /// <summary>
        /// Converts any % encoded values in the URL to uppercase.
        /// </summary>
        /// <param name="url">The URL string to normalize</param>
        /// <returns>The normalized url</returns>
        /// <example>NormalizeHexEncoding("Login.aspx?ReturnUrl=%2fAccount%2fManage.aspx") returns "Login.aspx?ReturnUrl=%2FAccount%2FManage.aspx"</example>
        /// <remarks>
        /// There is an issue in Facebook whereby it will rejects the redirect_uri value if
        /// the url contains lowercase % encoded values.
        /// </remarks>
        internal static string NormalizeHexEncoding(string url) 
        {
            var chars = url.ToCharArray();
            for (int i = 0; i < chars.Length - 2; i++) 
            {
                if (chars[i] == '%') 
                {
                    chars[i + 1] = char.ToUpperInvariant(chars[i + 1]);
                    chars[i + 2] = char.ToUpperInvariant(chars[i + 2]);
                    i += 2;
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Strips any and all URI query parameters that start with some prefix.
        /// </summary>
        /// <param name="uri">The URI that may have a query with parameters to remove.</param>
        /// <param name="prefix">The prefix for parameters to remove.  A period is NOT automatically appended.</param>
        /// <returns>Either a new Uri with the parameters removed if there were any to remove, or the same Uri instance if no parameters needed to be removed.</returns>
        public static Uri StripQueryArgumentsWithPrefix(this Uri uri, string prefix) 
        {
            Requires.NotNull(uri, "uri");
            Requires.NotNullOrEmpty(prefix, "prefix");
            
            NameValueCollection queryArgs = HttpUtility.ParseQueryString(uri.Query);
            var matchingKeys = queryArgs.Keys.OfType<string>().Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matchingKeys.Count > 0) 
            {
                UriBuilder builder = new UriBuilder(uri);
                foreach (string key in matchingKeys) 
                {
                    queryArgs.Remove(key);
                }
                builder.Query = CreateQueryString(queryArgs.ToDictionary());
                return builder.Uri;
            } 
            else 
            {
                return uri;
            }
        }

        /// <summary>
        /// Appends the query argument.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        internal static void AppendQueryArgument(this UriBuilder builder, string name, string value) 
        {
            AppendQueryArgs(builder, new[] { new KeyValuePair<string, string>(name, value) });
        }

        /// <summary>
        /// Appends the query arguments.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <param name="args">Arguments.</param>
        internal static void AppendQueryArgs(this UriBuilder builder, IEnumerable<KeyValuePair<string, string>> args) 
        {
            Requires.NotNull(builder, "builder");
            
            if (args != null && args.Count() > 0) 
            {
                StringBuilder sb = new StringBuilder(50 + (args.Count() * 10));
                if (!string.IsNullOrEmpty(builder.Query)) {
                    sb.Append(builder.Query.Substring(1));
                    sb.Append('&');
                }
                sb.Append(CreateQueryString(args));
                
                builder.Query = sb.ToString();
            }
        }

        /// <summary>
        /// Creates the query string.
        /// </summary>
        /// <returns>The query string.</returns>
        /// <param name="args">Arguments.</param>
        internal static string CreateQueryString(IEnumerable<KeyValuePair<string, string>> args) 
        {
            Requires.NotNull(args, "args");
            
            if (!args.Any()) {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(args.Count() * 10);
            
            foreach (var p in args) 
            {
                if (p.Key == null)
                    continue;

                if (p.Value == null)
                    p.Value = string.Empty;

                sb.Append(EscapeUriDataStringRfc3986(p.Key));
                sb.Append('=');
                sb.Append(EscapeUriDataStringRfc3986(p.Value));
                sb.Append('&');
            }
            sb.Length--; // remove trailing &
            
            return sb.ToString();
        }

        internal static string EscapeUriDataStringRfc3986(string value) 
        {
            Requires.NotNull(value, "value");
            
            // fast path for empty values.
            if (value.Length == 0) {
                return value;
            }
            
            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            StringBuilder escaped = new StringBuilder(Uri.EscapeDataString(value));
            
            // Upgrade the escaping to RFC 3986, if necessary.
            for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++) 
            {
                escaped.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
            }
            
            // Return the fully-RFC3986-escaped string.
            return escaped.ToString();
        }
    }
}

