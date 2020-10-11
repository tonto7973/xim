using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xim.Simulators.Api.Routing
{
    internal class Route
    {
        private static readonly Regex IsValidHttpVerbRx = new Regex("^[a-zA-Z0-9!#$%&'*+\\-.^_`|~]+$", RegexOptions.Compiled);
        private static readonly Uri BaseUri = new UriBuilder { Scheme = "http", Host = "localhost" }.Uri;
        protected static readonly Regex IsAttributeRx = new Regex("\\{[a-zA-Z_]\\w*\\}", RegexOptions.Compiled);

        protected readonly string Method;
        protected readonly Uri Uri;

        public readonly string Action;
        public readonly ApiHandler Handler;
        public readonly Route Previous;
        public bool Invoked;

        protected Route(string action, Func<Route, ApiHandler> handlerFactory, Route previous)
        {
            Action = Normalize(action, out Method, out Uri);
            Handler = handlerFactory(this);
            Previous = previous;
        }

        public Route(string action, ApiHandler handler, Route previous)
            : this(action, _ => handler, previous)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
        }

        public RouteOrder Match(string action)
        {
            string method;
            Uri uri;

            try
            {
                action = Normalize(action, out method, out uri);
            }
            catch (ArgumentException)
            {
                return RouteOrder.None;
            }

            return OnMatch(action, method, uri);
        }

        protected virtual RouteOrder OnMatch(string action, string method, Uri uri)
        {
            if (ExactMatch(action))
                return RouteOrder.Action;

            if (PathOnlyMatch(action))
                return RouteOrder.ActionNoQuery;

            return RouteOrder.None;
        }

        private bool ExactMatch(string action)
            => action.Equals(Action, StringComparison.InvariantCulture);

        private bool PathOnlyMatch(string action)
            => PathOnly(action).Equals(Action, StringComparison.InvariantCulture);

        private static string PathOnly(string action)
            => action.Split('#')[0].Split('?')[0];

        private static string Normalize(string action, out string method, out Uri uri)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var parts = action.Split(new[] { ' ' }, 2);
            var route = parts.Skip(1).FirstOrDefault();
            method = parts[0].ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException(SR.Format(SR.ApiRouteInvalidAction, action), nameof(action));
            if (string.IsNullOrWhiteSpace(method) || !IsValidHttpVerbRx.IsMatch(method))
                throw new ArgumentException(SR.Format(SR.ApiRouteInvalidVerb, method), nameof(action));

            if (!route.StartsWith("/", StringComparison.InvariantCulture))
                route = "/" + route;

            var uriBuilder = new UriBuilder(new Uri(BaseUri, route));

            if (!string.IsNullOrWhiteSpace(uriBuilder.Query))
                uriBuilder.Query = "?" + string.Join("&", uriBuilder.Query.Substring(1).Split('&').OrderBy(query => query));

            uri = uriBuilder.Uri;

            return method + " " + uriBuilder.Uri.LocalPath + uriBuilder.Query;
        }

        internal IEnumerable<Route> AsEnumerable()
        {
            Route current = this;
            do
            {
                yield return current;
                current = current.Previous;
            } while (current != null);
        }
    }
}
