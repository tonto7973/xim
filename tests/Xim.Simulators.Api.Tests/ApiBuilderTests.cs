using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

#pragma warning disable RCS1210 // Return Task.FromResult instead of returning null.

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiBuilderTests
    {
        [Test]
        public void Constructor_Throws_WhenSimulationNull()
        {
            Action action = () => new ApiBuilder(null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("simulation");
        }

        [Test]
        public void SetLoggerProvider_SetsLoggerProvider()
        {
            ILoggerProvider loggerProvider = Substitute.For<ILoggerProvider>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            ApiBuilder self = apiBuilder.SetLoggerProvider(loggerProvider);

            apiBuilder.LoggerProvider.ShouldBeSameAs(loggerProvider);
            self.ShouldBe(apiBuilder);
        }

        [Test]
        public void SetPort_SetsPort()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            ApiBuilder self = apiBuilder.SetPort(1234);

            apiBuilder.Port.ShouldBe(1234);
            self.ShouldBe(apiBuilder);
        }

        [Test]
        public void SetCertificate_SetsCertificate()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var cert = new X509Certificate2();
            ApiBuilder self = apiBuilder.SetCertificate(cert);

            apiBuilder.Certificate.ShouldBeSameAs(cert);
            self.ShouldBe(apiBuilder);
        }

        [Test]
        public void JsonSettings_ContainsDefaultValue()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            apiBuilder.JsonSettings.ShouldNotBeNull();
            apiBuilder.JsonSettings.WriteIndented.ShouldBeTrue();
            apiBuilder.JsonSettings.DictionaryKeyPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
            apiBuilder.JsonSettings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        }

        [Test]
        public void SetJsonSettings_SetsJsonSettings()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var jsonSettings = new JsonSerializerOptions();

            ApiBuilder self = apiBuilder.SetJsonSettings(jsonSettings);

            apiBuilder.JsonSettings.ShouldBeSameAs(jsonSettings);
            self.ShouldBeSameAs(apiBuilder);
        }

        [Test]
        public void XmlSettings_ContainsDefaultValue()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            apiBuilder.XmlSettings.ShouldNotBeNull();
            apiBuilder.XmlSettings.Encoding.ShouldBe(Encoding.UTF8);
            apiBuilder.XmlSettings.Indent.ShouldBeTrue();
        }

        [Test]
        public void SetXmlSettings_SetsXmlSettings()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var xmlSettings = new XmlWriterSettings
            {
                Indent = false
            };

            ApiBuilder self = apiBuilder.SetXmlSettings(xmlSettings);

            apiBuilder.XmlSettings.ShouldBeSameAs(xmlSettings);
            self.ShouldBeSameAs(apiBuilder);
        }

        [Test]
        public void AddHandler_Throws_WhenActionNull()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            Action action = () => apiBuilder.AddHandler(null, _ => null);

            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("action");
        }

        [TestCase("PIP")]
        [TestCase("POP ")]
        [TestCase("POST./3287")]
        public void AddHandler_Throws_WhenActionInvalid(string invalidAction)
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            Action action = () => apiBuilder.AddHandler(invalidAction, _ => null);

            ArgumentException exception = action.ShouldThrow<ArgumentException>();
            exception.ParamName.ShouldBe("action");
            exception.Message.ShouldStartWith(SR.Format(SR.ApiRouteInvalidAction, invalidAction));
        }

        [TestCase("LÚ")]
        [TestCase("")]
        public void AddHandler_Throws_WhenActionMethodInvalid(string invalidHttpMethod)
        {
            var invalidAction = $"{invalidHttpMethod} /index";
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            Action action = () => apiBuilder.AddHandler(invalidAction, _ => null);

            ArgumentException exception = action.ShouldThrow<ArgumentException>();
            exception.ParamName.ShouldBe("action");
            exception.Message.ShouldStartWith(SR.Format(SR.ApiRouteInvalidVerb, invalidHttpMethod));
        }

        [TestCase("GET /")]
        [TestCase("HEAD /")]
        [TestCase("PUT /index.html")]
        [TestCase("DELETE /forms/32")]
        public void AddHandler_AddsHandler_WhenActionValid(string validAction)
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            ApiHandler handler = _ => null;

            ApiBuilder self = apiBuilder.AddHandler(validAction, handler);

            apiBuilder.Handlers.Single().ShouldBe(handler);
            apiBuilder.Handlers[validAction].ShouldBe(handler);
            self.ShouldBeSameAs(apiBuilder);
        }

        [Test]
        public void AddHandlerT_AddsHandler_WhenActionValid()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiResponse = new ApiResponse(401);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "PUT";
            httpContext.Request.Path = "/data/32";
            ApiHandler<int> handler = (_, __) => Task.FromResult(apiResponse);

            ApiBuilder self = apiBuilder.AddHandler("PUT /data/{id}", handler);

            apiBuilder.Handlers.Single()(httpContext).Result.ShouldBeSameAs(apiResponse);
            apiBuilder.Handlers["PUT /data/{id}"].ShouldNotBeNull();
            self.ShouldBeSameAs(apiBuilder);
        }

        [Test]
        public void AddHandler_UpCasesHttpVerbInAction_WhenActionValid()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());

            ApiBuilder self = apiBuilder.AddHandler("Get /", _ => null);

            self.Handlers["GET /"].ShouldNotBeNull();
            self.ShouldBeSameAs(apiBuilder);
        }

        [Test]
        public void Build_AddsSimulatorToSimulation()
        {
            ISimulation simulation = Substitute.For<ISimulation, IAddSimulator>();
            var apiBuilder = new ApiBuilder(simulation);

            ((IAddSimulator)simulation).Add(Arg.Any<ApiSimulator>())
                .Returns(info => info.ArgAt<ApiSimulator>(0));

            var apiSimulator = (ApiSimulator)apiBuilder.Build();

            ((IAddSimulator)simulation).Received(1).Add(apiSimulator);
        }

        [Test]
        public void SetDefaultHandler_SetsDefaultHandler()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            ApiHandler defaultHandler = _ => null;

            ApiBuilder self = apiBuilder.SetDefaultHandler(defaultHandler);

            apiBuilder.DefaultHandler.ShouldBeSameAs(defaultHandler);
            self.ShouldBeSameAs(apiBuilder);
        }
    }
}
