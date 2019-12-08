using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

#pragma warning disable RCS1210 // Return Task.FromResult instead of returning null.

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiHandlerCollectionTests
    {
        [Test]
        public void Set_Throws_WhenActionNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set(null, _ => null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("action");
        }

        [Test]
        public void Set_Throws_WhenHandlerNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set("Get /x", null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("handler");
        }

        [Test]
        public void SetT_Throws_WhenActionNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set<int>(null, (_, __) => null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("action");
        }

        [Test]
        public void SetT_Throws_WhenHandlerNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set<DateTime>("Get /x", null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("handler");
        }

        [Test]
        public void Set_SetsHandlerForAction()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("GET /a", _ => null);

            apiHandlers["GET /a"].ShouldNotBeNull();
        }

        [Test]
        public void SetT_SetsHandlerForAction()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set<bool>("GET /{item}", (_, __) => null);

            apiHandlers["GET /a"].ShouldNotBeNull();
        }

        [Test]
        public void SetT_Throws_WhenActionWithoutTemplate()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set<bool>("GET /item", (_, __) => null);

            action.ShouldThrow<ArgumentException>()
                .Message.ShouldBe(SR.Format(SR.ApiHandlerTemplateNoParameters));
        }

        [Test]
        public void Get_ReturnsNull_WhenHandlerDoesNotExist()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("GET /b", _ => null);

            apiHandlers["POST /a"].ShouldBeNull();
        }

        [Test]
        public void Get_Throws_WhenActionIsNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("GET /b", _ => null);

            Action action = () => apiHandlers[null].ShouldBeNull();

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("action");
        }

        [Test]
        public void Keys_ReturnsAllKeys()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("GET /a", _ => null);
            apiHandlers.Set("GET /b", _ => null);
            apiHandlers.Set<int>("GET /b/{id}", (_, __) => null);

            var allKeys = apiHandlers.Keys.OrderBy(key => key).ToArray();

            allKeys.ShouldBe(new[] { "GET /a", "GET /b", "GET /b/{id}" });
        }

        [Test]
        public void IEnumerableGetEnumerator_EnumeratesAllItems()
        {
            var apiHandlers = new ApiHandlerCollection();
            ApiHandler handler = _ => null;

            apiHandlers.Set("PUT /a", handler);
            apiHandlers.Set("POST /b", handler);
            apiHandlers.Set("PATCH /c", handler);

            var enumerable = (IEnumerable)apiHandlers;

            var actions = enumerable.OfType<ApiHandler>()
                .ToArray();

            actions.ShouldBe(new[] { handler, handler, handler });
        }

        [TestCase("PIP")]
        [TestCase("POP ")]
        [TestCase("POST./3287")]
        public void Set_Throws_WhenActionInvalid(string invalidAction)
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Set(invalidAction, _ => null);

            var exception = action.ShouldThrow<ArgumentException>();
            exception.ParamName.ShouldBe("action");
            exception.Message.ShouldStartWith(SR.Format(SR.ApiRouteInvalidAction, invalidAction));
        }

        [TestCase("GET /")]
        [TestCase("HEAD /")]
        [TestCase("POST /forms")]
        [TestCase("PUT /index.html")]
        [TestCase("DELETE /forms/32")]
        [TestCase("CONNECT /")]
        [TestCase("OPTIONS /")]
        [TestCase("TRACE /")]
        [TestCase("PATCH /")]
        public void Set_SetsHandler_WhenActionValid(string validAction)
        {
            var apiHandlers = new ApiHandlerCollection();
            ApiHandler handler = _ => null;

            apiHandlers.Set(validAction, handler);

            apiHandlers.Single().ShouldBeSameAs(handler);
        }

        [Test]
        public void Set_NormalizesActionsHttpMethod()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("Get /", _ => null);

            apiHandlers["GET /"].ShouldNotBeNull();
        }

        [Test]
        public void Set_RootsActionsPath()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("Get index.html", _ => null);

            apiHandlers["GET /index.html"].ShouldNotBeNull();
        }

        [Test]
        public void Set_OrdersQueryString()
        {
            var apiHandlers = new ApiHandlerCollection();

            apiHandlers.Set("Get animals?name=a&age=32&data=bu%26da", _ => null);

            apiHandlers["GET /animals?age=32&data=bu%26da&name=a"].ShouldNotBeNull();
        }

        [Test]
        public void Clone_CreatesNewCollection()
        {
            var apiHandlers = new ApiHandlerCollection();
            apiHandlers.Set("PUT /a", _ => null);

            var clonedApiHandlers = (ApiHandlerCollection)((ICloneable)apiHandlers).Clone();
            apiHandlers.Set("PUT /b", _ => null);

            clonedApiHandlers.Keys.ToArray().ShouldBe(new[] { "PUT /a" });
        }

        [Test]
        public void Get_GetsNonTemplateHandlerFirst_WhenBothMatchAction()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "PUT";
            httpContext.Request.Path = "/a/32";
            var apiHandlers = new ApiHandlerCollection();
            apiHandlers.Set("PUT /a/32", _ => Task.FromResult(new ApiResponse(403)));
            apiHandlers.Set<int>("PUT /a/{id}", (_, __) => Task.FromResult(new ApiResponse(200)));
            apiHandlers["PUT /a/32"](httpContext).Result.StatusCode.ShouldBe(403);
        }

        [Test]
        public void Get_GetsTemplateHandlerFirst_WhenNoExactMatchForAction()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/x/33";
            var apiHandlers = new ApiHandlerCollection();
            apiHandlers.Set("GET /x/32", _ => Task.FromResult(new ApiResponse(403)));
            apiHandlers.Set<int>("GET /x/{id}", (_, __) => Task.FromResult(new ApiResponse(201)));
            apiHandlers["GET /x/33"](httpContext).Result.StatusCode.ShouldBe(201);
        }

        [Test]
        public void Next_Throws_WhenActionNull()
        {
            var apiHandlers = new ApiHandlerCollection();

            Action action = () => apiHandlers.Next(null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Next_GetsSameActionHandlersInRegisteredOrder()
        {
            var firstResponse = new ApiResponse(502);
            var secondResponse = new ApiResponse(500);
            var thirdResponse = new ApiResponse(200);
            var apiHandlers = new ApiHandlerCollection();
            apiHandlers.Set("GET /x/32", _ => Task.FromResult(firstResponse));
            apiHandlers.Set("GET /x/32", _ => Task.FromResult(secondResponse));
            apiHandlers.Set("GET /x/32", _ => Task.FromResult(thirdResponse));

            var first = apiHandlers.Next("GET /x/32");
            var second = apiHandlers.Next("GET /x/32");
            var third = apiHandlers.Next("GET /x/32");
            var fourth = apiHandlers.Next("GET /x/32");

            apiHandlers.ShouldSatisfyAllConditions(
                () => first(null).Result.ShouldBeSameAs(firstResponse),
                () => second(null).Result.ShouldBeSameAs(secondResponse),
                () => third(null).Result.ShouldBeSameAs(thirdResponse),
                () => fourth(null).Result.ShouldBeSameAs(thirdResponse)
            );
        }
    }
}
