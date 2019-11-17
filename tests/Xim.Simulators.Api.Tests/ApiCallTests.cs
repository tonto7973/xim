using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiCallTests
    {
        [TestCase("GET /index")]
        [TestCase("PUT /books/32")]
        public void Start_CreatesApiCallWithCorrectProperties(string action)
        {
            var httpContext = new DefaultHttpContext();

            var apiCall = ApiCall.Start(action, httpContext);

            apiCall.ShouldSatisfyAllConditions(
                () => apiCall.Id.ShouldBe(httpContext.TraceIdentifier),
                () => apiCall.Action.ShouldBe(action),
                () => apiCall.Request.ShouldBeSameAs(httpContext.Request),
                () => apiCall.Response.ShouldBeSameAs(httpContext.Response),
                () => apiCall.Exception.ShouldBeNull(),
                () => apiCall.StartTimeUtc.ShouldNotBe(default),
                () => apiCall.Duration.ShouldBe(TimeSpan.Zero)
            );
        }

        [Test]
        public void Fail_SetsTheException()
        {
            var exception = new InvalidOperationException("foo");
            var httpContext = new DefaultHttpContext();
            var apiCall = ApiCall.Start("ACT /", httpContext);

            apiCall.Fail(exception);

            apiCall.Exception.ShouldBeSameAs(exception);
        }

        [Test]
        public void Stop_SetsTheDuration()
        {
            var httpContext = new DefaultHttpContext();
            var apiCall = ApiCall.Start("ACT /", httpContext);

            Thread.Sleep(1);
            apiCall.Stop();

            apiCall.Duration.ShouldNotBe(TimeSpan.Zero);
        }
    }
}
