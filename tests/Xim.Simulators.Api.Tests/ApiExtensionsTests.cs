using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiExtensionsTests
    {
        [Test]
        public void AddApi_Throws_WhenSimulationNull()
        {
            Action action = () => ApiBuilderExtensions.AddApi(null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("simulation");
        }

        [Test]
        public void AddApi_ReturnsApiBuilderInstance()
        {
            var simulation = Substitute.For<ISimulation>();

            var apiBuilder = simulation.AddApi();

            apiBuilder.ShouldNotBeNull();
        }

        [Test]
        public void AddHandler1_Throws_WhenApiBuilderNull()
        {
            Action action = () => ApiBuilderExtensions.AddHandler(null, "POP /w", _ => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("apiBuilder");
        }

        [Test]
        public void AddHandler1_Throws_WhenActionNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler(null, _ => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("action");
        }

        [Test]
        public void AddHandler1_Throws_WhenHandlerNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler("GET /x", (Func<HttpContext, ApiResponse>)null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("handler");
        }

        [Test]
        public async Task AddHandler1_AddsHandler()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            apiBuilder.AddHandler("GET /a", _ => new ApiResponse(302));

            var response = await apiBuilder.Handlers["GET /a"](new DefaultHttpContext());

            response.StatusCode.ShouldBe(302);
        }

        [Test]
        public void AddHandler2_Throws_WhenApiBuilderNull()
        {
            Action action = () => ApiBuilderExtensions.AddHandler(null, "PUSH /f", ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("apiBuilder");
        }

        [Test]
        public void AddHandler2_Throws_WhenActionNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler(null, ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("action");
        }

        [Test]
        public void AddHandler2_Throws_WhenApiResponseNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler("GET /x", (ApiResponse)null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("response");
        }

        [Test]
        public async Task AddHandler2_AddsHandler()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            apiBuilder.AddHandler("GET /z", new ApiResponse(303));

            var response = await apiBuilder.Handlers["GET /z"](new DefaultHttpContext());

            response.StatusCode.ShouldBe(303);
        }

        [Test]
        public void AddHandler3_Throws_WhenApiBuilderNull()
        {
            Action action = () => ApiBuilderExtensions.AddHandler<int>(null, "PUT /z", (_, __) => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("apiBuilder");
        }

        [Test]
        public void AddHandler3_Throws_WhenActionNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler<int>(null, (_, __) => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("action");
        }

        [Test]
        public void AddHandler3_Throws_WhenHandlerNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler("LIST /we", (Func<int, HttpContext, ApiResponse>)null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("handler");
        }

        [Test]
        public async Task AddHandler3_AddsHandler()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var context = new DefaultHttpContext();
            context.Request.Method = "TO";
            context.Request.Path = "/z/784";

            apiBuilder.AddHandler<int>("TO /z/{id}", (value, _) => ApiResponse.Ok($"Value={value}"));

            var response = await apiBuilder.Handlers["TO /z/{id}"](context);

            response.StatusCode.ShouldBe(200);
            ((Body<string>)response.Body).Content.ShouldBe("Value=784");
        }

        [Test]
        public void AddHandler4_Throws_WhenApiBuilderNull()
        {
            Action action = () => ApiBuilderExtensions.AddHandler<int>(null, "PUT /z", _ => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("apiBuilder");
        }

        [Test]
        public void AddHandler4_Throws_WhenActionNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler<int>(null, _ => ApiResponse.Ok());

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("action");
        }

        [Test]
        public void AddHandler4_Throws_WhenHandlerNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            Action action = () => apiBuilder.AddHandler("LIST /we", (Func<int, ApiResponse>)null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("handler");
        }

        [Test]
        public async Task AddHandler4_AddsHandler()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var context = new DefaultHttpContext();
            context.Request.Method = "FROM";
            context.Request.Path = "/gate/true/3258";

            apiBuilder.AddHandler<(bool Open, int Id)>("FROM /gate/{open}/{id}", value => ApiResponse.Ok($"Value={value.Open}/{value.Id}"));

            var response = await apiBuilder.Handlers["FROM /gate/{open}/{id}"](context);

            response.StatusCode.ShouldBe(200);
            ((Body<string>)response.Body).Content.ShouldBe("Value=True/3258");
        }
    }
}
