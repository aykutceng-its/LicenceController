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
        public IActionResult Index(string? error = null)
        {
            ViewData["Error"] = error;
            return View();
        }

        [HttpPost]
        public IActionResult UploadLicence(string publicKey, IFormFile licenceFile)
        {
            var baseDir = AppContext.BaseDirectory;
            // Klasör ismini Core ile eşitleyelim (Örn: licence)
            var licenceFolder = Path.Combine(baseDir, "licence"); 

            try
            {
                if (string.IsNullOrEmpty(publicKey)) 
                {
                    return Json(new { success = false, message = "Public key boş bırakılamaz!" });
                }
                if (licenceFile == null) 
                {
                    return Json(new { success = false, message = "Lisans dosyası seçilmedi!" });
                }

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
                    return Json(new 
                    { 
                        success = true, 
                        message = "Lisans başarıyla yüklendi ve doğrulandı." 
                        redirectUrl = Url.Action("Index", "Home")
                    });
                }
                
                return Json(new { success = false, message = "Yüklenen lisans bu makine için geçerli değil!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sistemsel bir hata oluştu." });
            }
        }
    
    }
} 