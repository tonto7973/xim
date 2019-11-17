using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xim.Simulators.Api.Routing;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Represents the collection of api handlers.
    /// </summary>
    public sealed class ApiHandlerCollection : IEnumerable<ApiHandler>, ICloneable
    {
        private readonly Dictionary<string, Route> _handlers;

        /// <summary>
        /// Gets an api handler registered with api action.
        /// </summary>
        /// <param name="action">The action</param>
        /// <returns>The <see cref="ApiHandler"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="action"/> is null.</exception>
        [IndexerName("Handler")]
        public ApiHandler this[string action]
            => Get(action);

        /// <summary>
        /// Gets all actions registered with the collection.
        /// </summary>
        public IEnumerable<string> Keys => _handlers.Keys;

        /// <summary>
        /// Creates a new instance of <see cref="ApiHandlerCollection"/>.
        /// </summary>
        public ApiHandlerCollection()
        {
            _handlers = new Dictionary<string, Route>();
        }

        private ApiHandlerCollection(IDictionary<string, Route> handlers)
        {
            _handlers = new Dictionary<string, Route>(handlers);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="ApiHandlerCollection"/>.</returns>
        public IEnumerator<ApiHandler> GetEnumerator()
        {
            foreach (var item in _handlers.Values)
                yield return item.Handler;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        object ICloneable.Clone()
            => new ApiHandlerCollection(_handlers);

        internal void Set(string action, ApiHandler handler)
        {
            var route = new Route(
                action ?? throw new ArgumentNullException(nameof(action)),
                handler ?? throw new ArgumentNullException(nameof(handler))
            );
            _handlers[route.Action] = route;
        }

        internal void Set<T>(string action, ApiHandler<T> handler)
        {
            var route = new Route<T>(
                action ?? throw new ArgumentNullException(nameof(action)),
                handler ?? throw new ArgumentNullException(nameof(handler))
            );
            _handlers[route.Action] = route;
        }

        private ApiHandler Get(string action)
            => action == null
                ? throw new ArgumentNullException(nameof(action))
                : _handlers.Values
                    .Select(route => new { Order = route.Match(action), route.Handler })
                    .Where(item => item.Order != RouteOrder.None)
                    .OrderBy(item => item.Order)
                    .FirstOrDefault()?
                    .Handler;
    }
}
