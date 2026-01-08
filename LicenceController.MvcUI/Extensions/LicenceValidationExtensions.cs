using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LicenceController.Core.Services; // A projesinden
using LicenceController.Core.Interfaces;
using LicenceController.Core.Helpers;
using LicenceController.MvcUI.Middleware;
using LicenceController.MvcUI.Services;

namespace LicenceController.MvcUI.Extensions
{
    public static class LicenceValidationExtensions
    {
        public static IServiceCollection AddLicenceValidation(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<LicenceValidator>();
            services.AddHostedService<LicenceValidationBackgroundService>();
            services.AddSingleton<IRegistryHelper, RegistryHelper>();
            services.AddSingleton<IHardwareHelper, HardwareHelper>();
            return services;
        }

        public static IApplicationBuilder UseLicenceValidation(this IApplicationBuilder app)
        {
            app.UseMiddleware<LicenceValidationMiddleware>();
            return app;
        }
    }
} 