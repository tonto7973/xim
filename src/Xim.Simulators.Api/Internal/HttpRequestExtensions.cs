using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api.Internal
{
    internal static class HttpRequestExtensions
    {
        internal static Encoding GetCharset(this HttpRequest request)
        {
            if (string.IsNullOrEmpty(request?.ContentType))
                return null;
            try
            {
                var charset = new ContentType(request.ContentType).CharSet;
                if (string.IsNullOrEmpty(charset))
                    return null;
                return Encoding
                    .GetEncodings()
                    .Select(info => info.GetEncoding())
                    .FirstOrDefault(encoding => charset.Equals(encoding.HeaderName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            when (ex is ArgumentException || ex is FormatException)
            {
                return null;
            }
        }
    }
}
