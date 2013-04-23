using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using Validation;

namespace OpenAuthSharp
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Adds the item if not empty.
        /// </summary>
        /// <param name="dictionary">Dictionary.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        internal static void AddItemIfNotEmpty(this NameValueCollection dictionary, string key, string value) 
        {
            if (key == null)
                throw new ArgumentNullException("key");
            
            if (!string.IsNullOrEmpty(value))
                dictionary[key] = value;
        }

        /// <summary>
        /// Enumerates all members of the collection as key=value pairs.
        /// </summary>
        /// <param name="nvc">The collection to enumerate.</param>
        /// <returns>A sequence of pairs.</returns>
        internal static IEnumerable<KeyValuePair<string, string>> AsKeyValuePairs(this NameValueCollection nvc) 
        {
            Requires.NotNull(nvc, "nvc");
            
            foreach (string key in nvc)
            {
                foreach (string value in nvc.GetValues(key))
                {
                    yield return new KeyValuePair<string, string>(key, value);
                }
            }
        }
    }
}

