using System;
using System.Collections.Generic;
using System.Text;
using LicenceController.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LicenceController.Core.Extensions
{
    public static class MyLicenceCoreExtensions
    {
        public static IServiceCollection AddMyLicenceCore(this IServiceCollection services)
        {
            services.AddMemoryCache();

            // Implementasyonlar ana projede veya MvcUI'de eklenmeli
            // services.AddSingleton<IRegistryHelper, RegistryHelper>();
            // services.AddSingleton<IHardwareHelper, HardwareHelper>();
            services.AddSingleton<LicenceValidator>();

            return services;
        }
    }
}
