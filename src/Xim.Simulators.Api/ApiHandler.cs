using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Xim.Simulators.Api
{
    /// <summary>
    /// Api handler.
    /// </summary>
    /// <param name="context">Http context.</param>
    /// <returns>Task with api response.</returns>
    public delegate Task<ApiResponse> ApiHandler(HttpContext context);

    /// <summary>
    /// Templated api handler.
    /// </summary>
    /// <typeparam name="TArg">Template type. Can be primitive or value tuple.</typeparam>
    /// <param name="arg">Template value extracted from http request.</param>
    /// <param name="context">Http context.</param>
    /// <returns>Task with api response.</returns>
    public delegate Task<ApiResponse> ApiHandler<TArg>(TArg arg, HttpContext context);
}
