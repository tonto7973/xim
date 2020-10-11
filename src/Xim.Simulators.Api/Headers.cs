using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Api headers.
    /// </summary>
    public sealed class Headers : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// Gets or sets a header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <returns>The header value.</returns>
        /// <remarks>Previous headers are not overwritten; instead a new header is added.</remarks>
        public string this[string name]
        {
            get => Get(name);
            set => Set(name, value);
        }

        /// <summary>
        /// Gets the number of headers.
        /// </summary>
        public int Count => _headers.Count;

        /// <summary>
        /// Adds a header.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="value"/> is null.</exception>
        public void Add(string name, string value)
            => _headers.Add(new KeyValuePair<string, string>(
                    CheckValidName(name),
                    CheckValidValue(value)
                ));

        /// <summary>
        /// Removes all headers with the specified name.
        /// </summary>
        /// <param name="header">The name of the header to remove.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="header"/> is null.</exception>
        public void Remove(string header)
        {
            CheckValidName(header);

            var i = _headers.Count;
            while (i-- > 0)
            {
                if (_headers[i].Key.Equals(header, StringComparison.OrdinalIgnoreCase))
                    _headers.RemoveAt(i);
            }
        }

        private string Get(string header)
        {
            CheckValidName(header);

            StringValues values = _headers
                .Where(item => item.Key.Equals(header, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value)
                .ToArray();

            return values;
        }

        private void Set(string header, string value)
        {
            if (value == null)
                Remove(header);
            else
                Add(header, value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Headers"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="Headers"/>.</returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            => _headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _headers.GetEnumerator();

        /// <summary>
        /// Returns a string representing the <see cref="Headers"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> with the headers formatted.</returns>
        public override string ToString()
            => string.Join("\r\n", _headers.Select(header => $"{header.Key}: {header.Value}"));

        /// <summary>
        /// Creates a new instance of <see cref="Headers"/> by parsing http headers.
        /// </summary>
        /// <param name="httpHeaders">The headers to parse.</param>
        /// <returns>A new instance of <see cref="Headers"/> representing the parsed http headers.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="httpHeaders"/> is null.</exception>
        /// <remarks>
        /// The <paramref name="httpHeaders"/> string is expected to be formatted
        /// according to https://tools.ietf.org/html/rfc2616#section-4.2
        /// </remarks>
        public static Headers FromString(string httpHeaders)
        {
            if (httpHeaders == null)
                throw new ArgumentNullException(nameof(httpHeaders));

            var headers = new Headers();
            var headerTokens = new StringTokenizer(httpHeaders, new[] { '\n' });
            foreach (StringSegment headerToken in headerTokens)
            {
                var commaIndex = headerToken.IndexOf(':');
                if (commaIndex >= 0)
                {
                    var key = headerToken.Subsegment(0, commaIndex).Trim().ToString();
                    var value = headerToken.Subsegment(commaIndex + 1).Trim().ToString();

                    headers.Add(key, value);
                }
            }

            return headers;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Headers"/> from <see cref="IHeaderDictionary"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="httpHeaders"/> is null.</exception>
        public static Headers FromHeaderDictionary(IHeaderDictionary httpHeaders)
        {
            if (httpHeaders == null)
                throw new ArgumentNullException(nameof(httpHeaders));

            var headers = new Headers();
            foreach (KeyValuePair<string, StringValues> item in httpHeaders)
            {
                headers.Add(item.Key, item.Value);
            }
            return headers;
        }

        private static string CheckValidName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Format(SR.ApiHeaderNameIsEmpty), nameof(name));
            if (HeadersValidation.NameContainsInvalidChar(name, out (char Char, int Index) character))
                throw new ArgumentException(SR.Format(SR.ApiHeaderNameIsInvalid, ToLiteral(name), ToLiteral(character.Char), character.Index), nameof(name));

            return name;
        }

        private static string CheckValidValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (HeadersValidation.ValueContainsInvalidChar(value, out (char Char, int Index) character))
                throw new ArgumentException(SR.Format(SR.ApiHeaderValueIsInvalid, ToLiteral(value), ToLiteral(character.Char), character.Index), nameof(value));

            return value;
        }

        private static string ToLiteral(object code)
            => HttpUtility.JavaScriptStringEncode(code as string ?? code.ToString());
    }
}
