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
            if (path.Contains("/licence") || 
                path.StartsWith("/lib") || 
                path.StartsWith("/assets") ||
                path.Contains(".js") || 
                path.Contains(".css") || 
                path.Contains(".png") || 
                path.Contains(".jpg"))
            {
                await _next(context);
                return;
            }

            if (!_licenceValidator.IsLicenceValid())
            {

                _licenceValidator.ForceLicenceCheck();

                if (!_licenceValidator.IsLicenceValid())
                {
                    context.Response.Redirect("/Licence/Index");
                    return;
                }
            }

            await _next(context);
        }
    }
} 