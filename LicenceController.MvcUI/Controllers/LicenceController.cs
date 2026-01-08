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
            try
            {
                if (string.IsNullOrEmpty(publicKey))
                {
                    LogHelper.LogToFile("Public Key boş gönderildi!");
                    return View("Index");
                }
                if (licenceFile == null || licenceFile.Length == 0)
                {
                    LogHelper.LogToFile("Lisans dosyası seçilmedi!");
                    return View("Index");
                }
                // 1. PublicKey'i dosyaya yaz
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var licenceFolder = Path.Combine(baseDir, "ITS");
                    if (!Directory.Exists(licenceFolder))
                    {
                        Directory.CreateDirectory(licenceFolder);
                        LogHelper.LogToFile($"Klasör oluşturuldu: {licenceFolder}");
                    }
                    var publicKeyFilePath = Path.Combine(licenceFolder, "pubKey.pub");
                    using (var writer = new StreamWriter(publicKeyFilePath, false))
                    {
                        writer.Write(publicKey);
                    }
                    LogHelper.LogToFile($"PublicKey dosyaya kaydedildi: {publicKeyFilePath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.LogToFile($"PublicKey dosyası yazma izni hatası: {ex.Message}");
                    return View("Index");
                }
                catch (Exception ex)
                {
                    LogHelper.LogToFile($"PublicKey dosyası kaydedilirken hata: {ex.Message}");
                    return View("Index");
                }
                // 2. Lisans dosyasını kaydet
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var licenceFolder = Path.Combine(baseDir, "ITS");
                    if (!Directory.Exists(licenceFolder))
                    {
                        Directory.CreateDirectory(licenceFolder);
                        LogHelper.LogToFile($"Klasör oluşturuldu: {licenceFolder}");
                    }
                    var licenceFilePath = Path.Combine(licenceFolder, "Licence.json");
                    using (var stream = new FileStream(licenceFilePath, FileMode.Create))
                    {
                        licenceFile.CopyTo(stream);
                    }
                    LogHelper.LogToFile($"Lisans dosyası kaydedildi: {licenceFilePath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.LogToFile($"Lisans dosyası yazma izni hatası: {ex.Message}");
                    return View("Index");
                }
                catch (Exception ex)
                {
                    LogHelper.LogToFile($"Lisans dosyası kaydedilirken hata: {ex.Message}");
                    return View("Index");
                }
                // 3. Cache'i güncelle
                LogHelper.LogToFile("Cache güncellemesi başlatıldı (ForceLicenceCheck çağrıldı)");
                _licenceValidator.ForceLicenceCheck();
                LogHelper.LogToFile("Cache güncellemesi tamamlandı");
                LogHelper.LogToFile("Lisans başarıyla yüklendi!");
                return View("Index");
            }
            catch (Exception ex)
            {
                LogHelper.LogToFile($"Beklenmeyen hata: {ex.Message}");
                return View("Index");
            }
        }
    }
} 