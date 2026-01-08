using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LicenceController.Core.Services; // A projesinden

namespace LicenceController.MvcUI.Middleware
{
    public class LicenceValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LicenceValidator _licenceValidator;

        public LicenceValidationMiddleware(RequestDelegate next, LicenceValidator licenceValidator)
        {
            _next = next;
            _licenceValidator = licenceValidator;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Static ve licence view isteklerini atla
            if (path.StartsWith("/licence") || path.StartsWith("/css") || path.StartsWith("/js") || path.Contains(".css") || path.Contains(".js"))
            {
                await _next(context);
                return;
            }

            if (!_licenceValidator.IsLicenceValid())
            {
                context.Response.Redirect("/Licence/Index");
                return;
            }

            await _next(context);
        }
    }
} 