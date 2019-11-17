using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiSimulatorOwinMiddlewareTests
    {
        [TestCase("GET", "/any.htm")]
        [TestCase("PUT", "/books/323")]
        public async Task InvokeAsync_LogsRequest(string method, string path)
        {
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Received.InOrder(() =>
            {
                logger.Received(1).Log(LogLevel.Debug, 0, Arg.Is<FormattedLogValues>(value => value.ToString() == $"Request \"{method} {path}\" started"), null, Arg.Any<Func<object, Exception, string>>());
                logger.Received(1).Log(LogLevel.Debug, 0, Arg.Is<FormattedLogValues>(value => value.ToString() == $"Request \"{method} {path}\" completed"), null, Arg.Any<Func<object, Exception, string>>());
            });
        }

        [Test]
        public async Task InvokeAsync_InvokesApiCall()
        {
            ApiCall recordedApiCall = null;
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "PATCH";
            context.Request.Path = "/vehicle/32";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger,
                apiCall => recordedApiCall = apiCall);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            recordedApiCall.Request.ShouldBeSameAs(context.Request);
            recordedApiCall.Response.ShouldBeSameAs(context.Response);
            recordedApiCall.Exception.ShouldBeNull();
        }

        [Test]
        public async Task InvokeAsync_UsesDefaultHandler_WhenRegisteredHandlerNotFound()
        {
            var defaultHandlerCalled = false;
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetDefaultHandler(_ =>
                {
                    defaultHandlerCalled = true;
                    return Task.FromResult(new ApiResponse(403));
                });
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/any.htm";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            defaultHandlerCalled.ShouldBeTrue();
            context.Response.StatusCode.ShouldBe(403);
        }

        [Test]
        public async Task InvokeAsync_UsesRegisteredHandler_WhenHandlerAvailable()
        {
            var handlerCalled = false;
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .AddHandler("GET /users/32", _ =>
                {
                    handlerCalled = true;
                    return Task.FromResult(new ApiResponse(200));
                });
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/users/32";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            handlerCalled.ShouldBeTrue();
            context.Response.StatusCode.ShouldBe(200);
        }

        [Test]
        public async Task InvokeAsync_Returns502_WhenHandlerReturnsNullResponse()
        {
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetDefaultHandler(null);
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/vals";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            context.Response.StatusCode.ShouldBe(502);
            context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase.ShouldBe("Invalid Handler");
        }

        [Test]
        public async Task InvokeAsync_LogsError_WhenHandlerThrowsError()
        {
            var exception = new InvalidOperationException();
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .AddHandler("GET /vals", _ => throw exception);
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/vals";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask).ShouldThrowAsync<InvalidOperationException>();

            logger.Received(1).Log(LogLevel.Error, 0,
                Arg.Is<FormattedLogValues>(value => value.ToString() == "Error processing request \"GET /vals\""),
                exception,
                Arg.Any<Func<object, Exception, string>>());
        }

        [Test]
        public async Task InvokeAsync_InvokesApiCall_WhenHandlerThrowsError()
        {
            ApiCall recordedApiCall = null;
            var exception = new InvalidOperationException();
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .AddHandler("GET /vals", _ => throw exception);
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/vals";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, apiCall => recordedApiCall = apiCall);

            await middleware.InvokeAsync(context, _ => Task.CompletedTask).ShouldThrowAsync<InvalidOperationException>();

            recordedApiCall.Request.ShouldBeSameAs(context.Request);
            recordedApiCall.Response.ShouldBeSameAs(context.Response);
            recordedApiCall.Exception.ShouldBeSameAs(exception);
        }

        [Test]
        public async Task InvokeAsync_DisposesApiResponse()
        {
            var body = Substitute.ForPartsOf<Body>("foo", "text/plain");
            var response = new ApiResponse(202, body: body);
            var logger = Substitute.For<ILogger>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .AddHandler("PUT /test.ob", _ => Task.FromResult(response));
            var apiSettings = new ApiSimulatorSettings(apiBuilder);
            var context = new DefaultHttpContext();
            context.Request.Method = "PUT";
            context.Request.Path = "/test.ob";

            var middleware = new ApiSimulatorOwinMiddleware(apiSettings, logger, _ => { });

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            body.Received(1).Dispose();
        }
    }
}
