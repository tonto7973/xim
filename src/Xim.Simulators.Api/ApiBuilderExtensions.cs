using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Extension methods for <see cref="ApiBuilder"/>.
    /// </summary>
    public static class ApiBuilderExtensions
    {
        /// <summary>
        /// Registers new <see cref="ApiBuilder"/> with the <see cref="ISimulation"/>.
        /// </summary>
        /// <param name="simulation">The simulation.</param>
        /// <returns>New instance of <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If simulation is null.</exception>
        public static ApiBuilder AddApi(this ISimulation simulation)
            => new ApiBuilder(simulation);

        /// <summary>
        /// Sets a simple api handler.
        /// </summary>
        /// <param name="apiBuilder">The <see cref="ApiBuilder"/> instance.</param>
        /// <param name="action">The action to register the handler with, i.e. "GET /books".</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If one or more parameters is null.</exception>
        public static ApiBuilder AddHandler(this ApiBuilder apiBuilder, string action, Func<HttpContext, ApiResponse> handler)
        {
            if (apiBuilder == null)
                throw new ArgumentNullException(nameof(apiBuilder));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            apiBuilder.AddHandler(action, context => Task.FromResult(handler(context)));
            return apiBuilder;
        }

        /// <summary>
        /// Sets a simple api response handler.
        /// </summary>
        /// <param name="apiBuilder">The <see cref="ApiBuilder"/> instance.</param>
        /// <param name="action">The action to register the handler with, i.e. "GET /books".</param>
        /// <param name="response">The response for the handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If one or more parameters is null.</exception>
        public static ApiBuilder AddHandler(this ApiBuilder apiBuilder, string action, ApiResponse response)
        {
            if (apiBuilder == null)
                throw new ArgumentNullException(nameof(apiBuilder));
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            apiBuilder.AddHandler(action, _ => Task.FromResult(response));
            return apiBuilder;
        }

        /// <summary>
        /// Sets a simple templated api handler.
        /// </summary>
        /// <typeparam name="T">The template type.</typeparam>
        /// <param name="apiBuilder">The <see cref="ApiBuilder"/> instance.</param>
        /// <param name="action">The action template to register the handler with, i.e. "GET /books/{id}".</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If one or more parameters is null.</exception>
        public static ApiBuilder AddHandler<T>(this ApiBuilder apiBuilder, string action, Func<T, HttpContext, ApiResponse> handler)
        {
            if (apiBuilder == null)
                throw new ArgumentNullException(nameof(apiBuilder));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            apiBuilder.AddHandler<T>(action, (value, context) => Task.FromResult(handler(value, context)));
            return apiBuilder;
        }

        /// <summary>
        /// Sets a simple templated api handler.
        /// </summary>
        /// <typeparam name="T">The template type.</typeparam>
        /// <param name="apiBuilder">The <see cref="ApiBuilder"/> instance.</param>
        /// <param name="action">The action template to register the handler with, i.e. "GET /books/{id}".</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The <see cref="ApiBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">If one or more parameters is null.</exception>
        public static ApiBuilder AddHandler<T>(this ApiBuilder apiBuilder, string action, Func<T, ApiResponse> handler)
        {
            if (apiBuilder == null)
                throw new ArgumentNullException(nameof(apiBuilder));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            apiBuilder.AddHandler<T>(action, (value, _) => Task.FromResult(handler(value)));
            return apiBuilder;
        }
    }
}
