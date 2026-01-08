using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using LicenceController.Core.Services;
using Microsoft.Win32;
using LicenceController.Core.Helpers;

namespace LicenceController.MvcUI.Controllers
{
    [AllowAnonymous]
    public class LicenceController : Controller
    {
        private readonly LicenceValidator _licenceValidator;
        public LicenceController(LicenceValidator licenceValidator)
        {
            _licenceValidator = licenceValidator;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadLicence(string publicKey, IFormFile licenceFile)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Klasör ismini Core ile eşitleyelim (Örn: licence)
            var licenceFolder = Path.Combine(baseDir, "licence"); 

            try
            {
                if (string.IsNullOrEmpty(publicKey) || licenceFile == null) return View("Index");

                if (!Directory.Exists(licenceFolder)) Directory.CreateDirectory(licenceFolder);

                // 1. PublicKey Kaydet
                System.IO.File.WriteAllText(Path.Combine(licenceFolder, "pubKey.pub"), publicKey.Trim());

                // 2. Lisans Dosyasını Kaydet
                var filePath = Path.Combine(licenceFolder, "Licence.json");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    licenceFile.CopyTo(stream);
                }

                // 3. KRİTİK: Eski mührü temizle (Yeni donanıma uyum sağlaması için)
                var tokenPath = Path.Combine(licenceFolder, ".activation.token");
                if (System.IO.File.Exists(tokenPath)) System.IO.File.Delete(tokenPath);

                // 4. Sistemi Tetikle
                _licenceValidator.ForceLicenceCheck();

                // 5. Kontrol et ve yönlendir
                if (_licenceValidator.IsLicenceValid())
                {
                    LogHelper.LogToFile("Yeni lisans yüklendi ve doğrulandı. Yönlendiriliyor...");
                    return RedirectToAction("Index", "Home"); 
                }
                
                ViewData["ErrorL"] = "Yüklenen lisans bu makine için geçerli değil!";
                return View("Index");
            }
            catch (Exception ex)
            {
                LogHelper.LogToFile($"Yükleme Hatası: {ex.Message}");
                ViewData["ErrorL"] = "Sistemsel bir hata oluştu.";
                return View("Index");
            }
        }
    
    }
} 