using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq; // OrderBy için şart
using System.Globalization; // CultureInfo için şart
using System.Runtime.InteropServices;
using LicenceController.Core.Helpers;
using LicenceController.Core.Interfaces;
using LicenceController.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LicenceController.Core.Services
{
    public class LicenceValidator
    {
        private readonly IMemoryCache _cache;
        private readonly IRegistryHelper _registryHelper;
        private readonly IHardwareHelper _hardwareHelper;

        public LicenceValidator(IMemoryCache cache, IRegistryHelper registryHelper, IHardwareHelper hardwareHelper)
        {
            _cache = cache;
            _registryHelper = registryHelper;
            _hardwareHelper = hardwareHelper;
        }

        public bool IsLicenceValid()
        {
            try
            {
                var publicKey = _registryHelper.GetPublicKey();

                if (string.IsNullOrEmpty(publicKey))
                {
                    LogHelper.LogToFile("Core-IsLicenceValid: Public key boş geldi.");
                    return false;
                }

                var hardwareId = CachedInfo.GetHardwareId(_hardwareHelper);
                if (string.IsNullOrEmpty(hardwareId))
                {
                    LogHelper.LogToFile("Core-IsLicenceValid: Hardware ID boş geldi.");
                    return false;
                }

                var cacheKey = CacheKeyProvider.GetCacheKey(publicKey, hardwareId);

                bool isCached = _cache.TryGetValue(cacheKey, out LicenceCacheEntry entry);
                if (isCached)
                {
                    var moduleList = JsonSerializer.Deserialize<Dictionary<string, bool>>(entry.ModulesJson ?? "{}") 
                     ?? new Dictionary<string, bool>();

                    // 2. Tıpkı yazarken yaptığın gibi string formatına getir (Büyük harf dikkat!)
                    string modulesString = string.Join(",", moduleList.Select(x => $"{x.Key}:{x.Value.ToString().ToUpper()}"));

                    // 3. dataToSign'ı imza atılırkenki orijinal haline getiriyoruz
                    var dataToReSign = entry.Data + (moduleList.Any() ? "|" + modulesString : "");

                    // 4. Şimdi imzayı hesapla
                    var expectedSignature = CacheSignatureHelper.ComputeHmac(dataToReSign, publicKey);
                    
                    LogHelper.LogToFile($"Core-IsLicenceValid: Bulunan imza: {entry.Signature}");
                    LogHelper.LogToFile($"Core-IsLicenceValid: Beklenen imza: {expectedSignature}");
                    if (entry.Signature == expectedSignature && entry.Data.StartsWith("True"))
                    {
                        return true;
                    }

                }
                LogHelper.LogToFile("Core-IsLicenceValid: Cache'de veri bulunamadı veya geçersiz imza.");
                return false;
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("IsLicenceValid-ERROR: " + ex.Message);
                return false;
            }
        }

        public void ForceLicenceCheck()
        {
            try
            {
                var rsaHelper = new RsaHelper(_hardwareHelper, _registryHelper);
                var publicKey = _registryHelper.GetPublicKey();
                var hardwareID = CachedInfo.GetHardwareId(_hardwareHelper);

                // --- YOL DÜZENLEMESİ ---
                // Docker'da /app/data gibi bir yere volume bağladığımız için 
                // Öncelikle bir ortam değişkeni veya sabit bir linux yolu kontrolü yapmalıyız.
                string baseDirectory = AppContext.BaseDirectory;
                string licenceFolder = Path.Combine(baseDirectory, "licence");

                if (!Directory.Exists(licenceFolder)) 
                {
                    Directory.CreateDirectory(licenceFolder);
                }

                // Dosya yollarını bu güvenli klasör üzerinden kuruyoruz
                string licenceFilePath = Path.Combine(licenceFolder, "Licence.json");
                string tokenPath = Path.Combine(licenceFolder, ".activation.token");
                // -----------------------

                if (!File.Exists(licenceFilePath))
                {
                    LogHelper.LogToFile($"ForceLicenceCheck: Licence.json bulunamadı. Path: {licenceFilePath}");
                    SaveResultToCache(false, publicKey, hardwareID);
                    return;
                }

                var encryptedJSON = File.ReadAllText(licenceFilePath);
                var signedJsonText = rsaHelper.DecryptString(encryptedJSON);

                if (string.IsNullOrEmpty(signedJsonText))
                {
                    LogHelper.LogToFile("ForceLicenceCheck: Decrypt başarısız.");
                    SaveResultToCache(false, "", "");
                    return;
                }

                // --- MÜHÜR (TOKEN) KONTROLÜ ---
                if (File.Exists(tokenPath))
                {
                    var existingToken = File.ReadAllText(tokenPath);
                    var expectedToken = CacheSignatureHelper.ComputeHmac(hardwareID, publicKey);
                    
                    if (existingToken != expectedToken)
                    {
                        LogHelper.LogToFile("KRİTİK: Donanım uyuşmazlığı! Token eşleşmiyor.");
                        SaveResultToCache(false, publicKey, hardwareID);
                        return;
                    }
                }
                else
                {
                    var newToken = CacheSignatureHelper.ComputeHmac(hardwareID, publicKey);
                    File.WriteAllText(tokenPath, newToken);
                    // Linux'ta Hidden attribute farklı işler, o yüzden sadece yazıyoruz
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        File.SetAttributes(tokenPath, FileAttributes.Hidden | FileAttributes.ReadOnly);
                }

                var jsonLicence = JsonSerializer.Deserialize<Licence>(signedJsonText);
                if (jsonLicence == null) return;

                string modulesString = "";
                if (jsonLicence.MODULES != null && jsonLicence.MODULES.Count > 0)
                {
                    // Generator tarafında OrderBy kullandıysan burada da kullanmalısın
                    modulesString = string.Join(",", jsonLicence.MODULES
                        .OrderBy(x => x.Key)
                        .Select(x => $"{x.Key.ToUpper()}:{x.Value.ToString().ToLower().Trim()}"));
                }

                // --- VERİ DOĞRULAMA ---
                // Şimdi tam JSONData (İmza Kontrolü İçin)
                string JSONData = $"{jsonLicence.CPUID.ToString().Trim()}|||" +
                                $"{jsonLicence.MAC.ToString().Trim()}|||" +
                                $"{jsonLicence.DISKNO.ToString().Trim()}|||" +
                                $"{jsonLicence.TYPE.ToString().Trim()}|||" +
                                $"{jsonLicence.DURATION.ToString().Trim()}|||" +
                                $"{jsonLicence.CREATED.ToString().Trim()}|||" +
                                $"{jsonLicence.EXPIRED.ToString().Trim()}|||" +
                                $"{modulesString}"; // Modüller en sona eklendi

                bool isVerified = RsaHelper.VerifySignatureWithHash(JSONData, jsonLicence.SIGN, publicKey);
                
                if (!isVerified)
                {
                    LogHelper.LogToFile($"ForceLicenceCheck: İmza geçersiz.");
                    SaveResultToCache(false, publicKey, hardwareID);
                    return;
                }

                // Donanım eşleşmesi (CPUID + MAC + DISK)
                var isHardwareMatch = hardwareID == $"{jsonLicence.CPUID}|||{jsonLicence.MAC}|||{jsonLicence.DISKNO}";
                
                // Tarih Kontrolü
                DateTime createDate = DateTime.ParseExact(jsonLicence.CREATED, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                DateTime expireDate = jsonLicence.TYPE == 0 ? createDate.AddDays(30) : DateTime.ParseExact(jsonLicence.EXPIRED, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                
                var isValid = isHardwareMatch && DateTime.Now >= createDate && DateTime.Now <= expireDate;

                LogHelper.LogToFile($"Sonuç: {isValid}. Match: {isHardwareMatch}. Expire: {expireDate}");
                SaveResultToCache(isValid, publicKey, hardwareID, jsonLicence.MODULES);
            }
            catch (Exception ex)
            {
                LogHelper.LogToFile("ForceLicenceCheck-ERROR: " + ex.Message);
            }
        }

        private void SaveResultToCache(bool isValid, string publicKey, string hardwareId, Dictionary<string, bool>? modules = null)
        {
            try
            {
                if(string.IsNullOrEmpty(publicKey))
                {
                    LogHelper.LogToFile("SaveResultToCache: Public key boş geldi.");
                    return;
                }
                if (string.IsNullOrEmpty(hardwareId))
                {
                    LogHelper.LogToFile("SaveResultToCache: Hardware ID boş geldi.");
                    return;
                }
                var getCacheKey = CacheKeyProvider.GetCacheKey(publicKey, hardwareId);
                var rawData = (isValid ? "True" : "False") + "-" + getCacheKey;

                // 1. Önce veriyi hazırla
                var moduleList = modules ?? new Dictionary<string, bool>();

                // 2. Modülleri string formatına getir (Key:Value,Key:Value)
                string modulesString = string.Join(",", moduleList.Select(x => $"{x.Key}:{x.Value.ToString().ToUpper()}"));

                // 3. dataToSign oluştur (Parantezlere dikkat!)
                var dataToSign = rawData + (moduleList.Any() ? "|" + modulesString : "");

                // 4. En son JSON'a çevir (Eğer Licence.json içine koyacaksan)
                var modulesJson = JsonSerializer.Serialize(moduleList);

                var signature = CacheSignatureHelper.ComputeHmac(dataToSign, publicKey);

                var entry = new LicenceCacheEntry
                {
                    Data = rawData,
                    Signature = signature,
                    ModulesJson = modulesJson
                };

                _cache.Set(getCacheKey, entry, TimeSpan.FromHours(25));
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("SaveResultToCache-ERROR: " + ex.Message);
                return;
            }
        }

        public bool HasModule(string moduleName)
        {
            var publicKey = _registryHelper.GetPublicKey();
            var hardwareId = CachedInfo.GetHardwareId(_hardwareHelper);
            var cacheKey = CacheKeyProvider.GetCacheKey(publicKey, hardwareId);

            if (_cache.TryGetValue(cacheKey, out LicenceCacheEntry entry))
            {
                // Önce cache imzasını doğrula (Modüller manipüle edilmiş mi?)
                var dataToSign = entry.Data + "|" + entry.ModulesJson;
                var expectedSignature = CacheSignatureHelper.ComputeHmac(dataToSign, publicKey);
                
                if (entry.Signature == expectedSignature)
                {
                    var modules = JsonSerializer.Deserialize<Dictionary<string, bool>>(entry.ModulesJson ?? "{}");
                    return modules != null && modules.TryGetValue(moduleName, out bool active) && active;
                }
            }
            return false;
        }
    }
}
