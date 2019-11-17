using System;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Set of convenience methods for <see cref="Headers"/>.
    /// </summary>
    public static class HeadersExtensions
    {
        /// <summary>
        /// Adds a Location header to <see cref="Headers"/>.
        /// </summary>
        /// <param name="headers">The <see cref="Headers"/> instance.</param>
        /// <param name="location">The location to add.</param>
        /// <returns>The <see cref="Headers"/> instance.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="headers"/> or <paramref name="location"/> is null.</exception>
        public static Headers AddLocation(this Headers headers, Uri location)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            var url = location.IsAbsoluteUri
                ? location.AbsoluteUri
                : location.GetComponents(UriComponents.SerializationInfoString,
                                         UriFormat.UriEscaped);

            headers.Add("Location", url);

            return headers;
        }

        /// <summary>
        /// Adds a WWW-Authenticate header to <see cref="Headers"/>.
        /// </summary>
        /// <param name="headers">The <see cref="Headers"/> instance.</param>
        /// <param name="challenge">The authentication challenge, i.e. <c>"Bearer error=\"insufficient_scope\""</c></param>
        /// <returns>The <see cref="Headers"/> instance.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="headers"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="challenge"/> is empty.</exception>
        public static Headers AddWwwAuthenticate(this Headers headers, string challenge)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            if (string.IsNullOrWhiteSpace(challenge))
                throw new ArgumentException(SR.Format(SR.ApiHeaderChallengeEmpty), nameof(challenge));

            headers.Add("WWW-Authenticate", challenge);

            return headers;
        }
    }
}
