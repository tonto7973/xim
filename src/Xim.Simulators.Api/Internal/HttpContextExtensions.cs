using System.Text;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api.Internal
{
    internal static class HttpContextExtensions
    {
        private const string ApiSimulatorSettingsKey = "ApiSimulatorSettings.Instance";
        private const string ApiSimulatorBodyEncodingKey = "ApiSimulatorBody.Encoding";

        internal static HttpContext SetApiSimulatorSettings(this HttpContext context, ApiSimulatorSettings settings)
        {
            context.Items[ApiSimulatorSettingsKey] = settings;
            return context;
        }

        internal static ApiSimulatorSettings GetApiSimulatorSettings(this HttpContext context)
        {
            context.Items.TryGetValue(ApiSimulatorSettingsKey, out var value);
            return value as ApiSimulatorSettings;
        }

        internal static HttpContext SetApiSimulatorBodyEncoding(this HttpContext context, Encoding encoding)
        {
            context.Items[ApiSimulatorBodyEncodingKey] = encoding;
            return context;
        }

        internal static Encoding GetApiSimulatorBodyEncoding(this HttpContext context)
        {
            context.Items.TryGetValue(ApiSimulatorBodyEncodingKey, out var value);
            return value as Encoding;
        }
    }
}
