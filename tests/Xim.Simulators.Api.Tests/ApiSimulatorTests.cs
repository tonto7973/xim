using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiSimulatorTests
    {
        [Test]
        public void Constructor_SetsLoggerProviderFromApiBuilder()
        {
            ILoggerProvider loggerProvider = Substitute.For<ILoggerProvider>();
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            apiBuilder.SetLoggerProvider(loggerProvider);
            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.Settings.LoggerProvider.ShouldBeSameAs(loggerProvider);
        }

        [Test]
        public void Constructor_SetsCertificateFromApiBuilder()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var certificate = new X509Certificate2();
            apiBuilder.SetCertificate(certificate);
            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.Settings.Certificate.ShouldBeSameAs(certificate);
        }

        [Test]
        public void Constructor_SetsCopyOfHandlersFromApiBuilder()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            ApiHandler handler = _ => Task.FromResult<ApiResponse>(null);
            apiBuilder.AddHandler("GET /a", handler);
            var apiSimulator = new ApiSimulator(apiBuilder);
            apiBuilder.AddHandler("GET /b", handler);

            apiSimulator.Settings.Handlers["GET /a"].ShouldBeSameAs(handler);
            apiSimulator.Settings.Handlers["GET /b"].ShouldBeNull();
        }

        [Test]
        public void Constructor_SetDefaultHandlerFromApiBuilder()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            ApiHandler handler = _ => Task.FromResult<ApiResponse>(null);
            apiBuilder.SetDefaultHandler(handler);

            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.Settings.DefaultHandler.ShouldBeSameAs(handler);
        }

        [Test]
        public void Constructor_SetXmlSettingsFromApiBuilder()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var xmlSettings = new XmlWriterSettings();
            apiBuilder.SetXmlSettings(xmlSettings);

            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.Settings.XmlSettings.ShouldBeSameAs(xmlSettings);
        }

        [Test]
        public void Constructor_SetJsonSettingsFromApiBuilder()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var jsonSettings = new JsonSerializerOptions();
            apiBuilder.SetJsonSettings(jsonSettings);

            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.Settings.JsonSettings.ShouldBeSameAs(jsonSettings);
        }

        [Test]
        public void Constructor_SetsStateToStopped()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            apiSimulator.State.ShouldBe(SimulatorState.Stopped);
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenCertificateNotSet()
        {
            ApiBuilder apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .SetCertificate(null);
            var apiSimulator = new ApiSimulator(apiBuilder);

            Action action = () => apiSimulator.Dispose();

            apiSimulator.ShouldSatisfyAllConditions(
                () => apiSimulator.Settings.Certificate.ShouldBeNull(),
                () => action.ShouldNotThrow()
            );
        }

        [Test]
        public void Dispose_DoesNotThrow_WhenCalledTwice()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            Action action = () =>
            {
                apiSimulator.Dispose();
                apiSimulator.Dispose();
            };

            action.ShouldNotThrow();
        }

        [Test]
        public async Task Dispose_DoesNotThrow_WhenSimulatorRunning()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            await Task.Delay(100);

            Action action = () => apiSimulator.Dispose();

            action.ShouldNotThrow();
        }

        [Test]
        public async Task Dispose_SetsStateToStopped()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);
            await apiSimulator.StartAsync();

            apiSimulator.Dispose();

            apiSimulator.State.ShouldBe(SimulatorState.Stopped);
        }

        [Test]
        public async Task StartAsync_SetsStateToRunning()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                apiSimulator.State.ShouldBe(SimulatorState.Running);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public void StartAsync_Fails_WhenHostCannotBeBuilt()
        {
            ILoggerProvider fakeLogger = Substitute.For<ILoggerProvider>();
            fakeLogger.CreateLogger(Arg.Any<string>()).Returns(_ => throw new NotSupportedException());

            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder.SetLoggerProvider(fakeLogger));

            apiSimulator.StartAsync().ShouldThrow<NotSupportedException>();
            apiSimulator.State.ShouldBe(SimulatorState.Stopped);
        }

        [Test]
        public async Task StartAsync_Fails_WhenPortAlreadyUsed()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var usedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
                var apiSimulator = new ApiSimulator(apiBuilder.SetPort(usedPort));

                await apiSimulator.StartAsync().ShouldThrowAsync<IOException>();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Test]
        public async Task StartAsync_ReportsActivePort()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                apiSimulator.Port.ShouldNotBe(0);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_ReportsActiveLocation()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                apiSimulator.Location.ShouldNotBeNullOrEmpty();
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartAsync_DoesNotThrow_WhenCalledMultipleTimes()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                await apiSimulator.StartAsync();
                await Task.Delay(150);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StopAsync_DoesNotThrow_WhenCalledMultipleTimes()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                await Task.Delay(150);
                await apiSimulator.StopAsync();
                await apiSimulator.StopAsync();
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StopAsync_SetsStateToStopped()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                await apiSimulator.StopAsync();
                apiSimulator.State.ShouldBe(SimulatorState.Stopped);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task Abort_DoesNotThrow_WhenCalledMultipleTimes()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                await Task.Delay(150);
                apiSimulator.Abort();
                apiSimulator.Abort();
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task Abort_SetsStateToStopped()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                apiSimulator.Abort();
                apiSimulator.State.ShouldBe(SimulatorState.Stopped);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public async Task StartStop_DoesNotThrow_WhenCalledMultipleTimes()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                await Task.Delay(150);
                await apiSimulator.StopAsync();
                await Task.Delay(150);
                await apiSimulator.StartAsync();
            }
            finally
            {
                await apiSimulator.StopAsync();
            }
        }

        [Test]
        public void Port_Throws_WhenSimulatorNotRunning()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            Action action = () => apiSimulator.Port.ShouldBe(0);

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorPropertyInvalid, nameof(ApiSimulator.Port)));
        }

        [Test]
        public void Location_Throws_WhenSimulatorNotRunning()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);

            Action action = () => apiSimulator.Location.ShouldBeNull();

            action.ShouldThrow<InvalidOperationException>()
                .Message.ShouldBe(SR.Format(SR.SimulatorPropertyInvalid, nameof(ApiSimulator.Location)));
        }

        [Test]
        public async Task StartAsync_Throws_WhenPortInUse()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            try
            {
                var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
                apiBuilder.SetPort(port);
                var apiSimulator = new ApiSimulator(apiBuilder);
                try
                {
                    await apiSimulator.StartAsync().ShouldThrowAsync<IOException>();
                }
                finally
                {
                    await apiSimulator.StopAsync();
                }
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        [Test]
        public async Task StartAsync_UsesCertificate_WhenSpecified()
        {
            using X509Certificate2 certificate = TestCertificate.Find() ?? TestCertificate.Create();
            ApiBuilder apiBuilder = new ApiBuilder(Substitute.For<ISimulation>()).SetCertificate(certificate);
            var apiSimulator = new ApiSimulator(apiBuilder);

            await apiSimulator.StartAsync();
            try
            {
                apiSimulator.Location.ShouldStartWith("https://");
            }
            finally
            {
                await apiSimulator.StopAsync();
            }

            certificate.Reset();
        }

        [Test]
        public void Request_InstantiatesMiddleware()
        {
            var apiBuilder = new ApiBuilder(Substitute.For<ISimulation>());
            var apiSimulator = new ApiSimulator(apiBuilder);
            Func<Task> action = async () =>
            {
                try
                {
                    await apiSimulator.StartAsync();

                    var port = apiSimulator.Port;

                    using var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{port}/version");
                    await httpClient.SendAsync(request);
                }
                finally
                {
                    await apiSimulator.StopAsync();
                }
            };
            action.ShouldNotThrow();
        }

        [Test]
        public async Task ReceivedApiCalls_GetsAllRecordedApiCalls()
        {
            ApiBuilder apiBuilder = new ApiBuilder(Substitute.For<ISimulation>())
                .AddHandler("GET /api/v2/books", _ => throw new Exception());
            var apiSimulator = new ApiSimulator(apiBuilder);
            try
            {
                await apiSimulator.StartAsync();
                var port = apiSimulator.Port;

                using var httpClient = new HttpClient();
                var task1 = Task.Run(async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:{port}/api/v2/books");
                    await httpClient.SendAsync(request);
                });
                var task2 = Task.Run(async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Patch, $"http://localhost:{port}/api/v2/books/32")
                    {
                        Content = new StringContent("{\"title\":\"abc\"}", Encoding.UTF8, "application/json")
                    };
                    await httpClient.SendAsync(request);
                });
                await Task.WhenAll(task1, task2);
            }
            finally
            {
                await apiSimulator.StopAsync();
            }

            System.Collections.Generic.IReadOnlyCollection<ApiCall> apiCalls = apiSimulator.ReceivedApiCalls;
            ApiCall getCall = apiCalls.FirstOrDefault(call => call.Action == "GET /api/v2/books");
            apiCalls.ShouldSatisfyAllConditions(
                () => getCall.ShouldNotBeNull(),
                () => getCall.Exception.ShouldNotBeNull(),
                () => apiCalls.Count.ShouldBe(2),
                () => apiCalls
                        .Single(call => call.Action == "PATCH /api/v2/books/32")
                        .ShouldNotBeNull()
            );
        }
    }
}
