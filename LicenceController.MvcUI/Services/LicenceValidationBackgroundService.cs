using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using LicenceController.Core.Services; // A projesinden

namespace LicenceController.MvcUI.Services
{
    public class LicenceValidationBackgroundService : BackgroundService
    {
        private readonly LicenceValidator _licenceValidator;

        public LicenceValidationBackgroundService(LicenceValidator licenceValidator)
        {
            _licenceValidator = licenceValidator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Uygulama başlatıldığında kontrol
            _licenceValidator.ForceLicenceCheck();

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(now.Hour >= 3 ? 1 : 0).AddHours(3);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                _licenceValidator.ForceLicenceCheck();
            }
        }
    }
} 