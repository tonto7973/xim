using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Xim.Simulators.Api
{
    internal sealed class ApiSimulator : Simulator, IApiSimulator
    {
        private ConcurrentBag<ApiCall> _apiCalls = new ConcurrentBag<ApiCall>();
        private bool _disposed;
        private IWebHost _webHost;

        public ApiSimulatorSettings Settings { get; }

        public int Port => GetPort();

        public string Location => GetLocation();

        public IEnumerable<ApiCall> ReceivedApiCalls => GetApiCalls();

        internal ApiSimulator(ApiBuilder builder)
        {
            Settings = new ApiSimulatorSettings(builder);
        }

        private IWebHost BuildWebHost()
            => new WebHostBuilder()
                .UseKestrel(kestrelOptions =>
                {
                    kestrelOptions.Listen(IPAddress.Loopback, Settings.Port, listenOptions =>
                    {
                        if (Settings.Certificate != null)
                        {
                            listenOptions.UseHttps(Settings.Certificate);
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddTransient(_ => CreateApiSimulatorMiddleware());
                })
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddProvider(Settings.LoggerProvider ?? NullLoggerProvider.Instance);
                })
                .Configure(app => app.UseMiddleware<ApiSimulatorOwinMiddleware>())
                .Build();

        private ApiSimulatorOwinMiddleware CreateApiSimulatorMiddleware()
            => new ApiSimulatorOwinMiddleware(
                Settings,
                _webHost.Services.GetRequiredService<ILogger<ApiSimulator>>(),
                apiCall => _apiCalls.Add(apiCall)
            );

        private int GetPort()
            => TryGetLocation(out var location)
                ? new Uri(location).Port
                : throw new InvalidOperationException(SR.Format(SR.SimulatorPropertyInvalid, nameof(Port)));

        private string GetLocation()
            => TryGetLocation(out var location)
                ? location
                : throw new InvalidOperationException(SR.Format(SR.SimulatorPropertyInvalid, nameof(Location)));

        private IEnumerable<ApiCall> GetApiCalls()
        {
            foreach (var apiCall in _apiCalls)
                yield return apiCall;
        }

        private bool TryGetLocation(out string location)
        {
            location = State == SimulatorState.Running
                ? _webHost.ServerFeatures.Get<IServerAddressesFeature>().Addresses.FirstOrDefault()
                : null;

            return location != null;
        }

        public override async Task StartAsync()
        {
            if (TrySetState(SimulatorState.Starting))
            {
                try
                {
                    _apiCalls = new ConcurrentBag<ApiCall>();
                    _webHost = BuildWebHost();
                    await _webHost.StartAsync().ConfigureAwait(false);
                }
                catch
                {
                    _webHost?.Dispose();
                    _webHost = null;
                    SetState(SimulatorState.Stopped);
                    throw;
                }

                SetState(SimulatorState.Running);
            }
        }

        public override Task StopAsync()
            => StopAsync(TimeSpan.FromSeconds(5));

        private async Task StopAsync(TimeSpan timeout)
        {
            if (TrySetState(SimulatorState.Stopping))
            {
                try
                {
                    await _webHost.StopAsync(timeout).ConfigureAwait(false);
                    await _webHost.WaitForShutdownAsync().ConfigureAwait(false);
                }
                finally
                {
                    _webHost?.Dispose();
                    _webHost = null;
                    SetState(SimulatorState.Stopped);
                }
            }
        }

        public override void Abort()
            => StopAsync(TimeSpan.FromMilliseconds(1))
                .GetAwaiter()
                .GetResult();

        public void Dispose()
        {
            if (_disposed)
                return;

            Abort();
            _webHost?.Dispose();

            _disposed = true;
        }
    }
}