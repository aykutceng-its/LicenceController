using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                LogHelper.LogToFile("Core-IsLicenceValid: Public key alındı: " + publicKey);

                var hardwareId = CachedInfo.GetHardwareId(_hardwareHelper);
                if (string.IsNullOrEmpty(hardwareId))
                {
                    LogHelper.LogToFile("Core-IsLicenceValid: Hardware ID boş geldi.");
                    return false;
                }
                LogHelper.LogToFile("Core-IsLicenceValid: Hardware ID alındı: " + hardwareId);

                var cacheKey = CacheKeyProvider.GetCacheKey(publicKey, hardwareId);
                LogHelper.LogToFile("Core-IsLicenceValid: Bakılan Cache Name: " + cacheKey);

                bool isCached = _cache.TryGetValue(cacheKey, out LicenceCacheEntry entry);
                if (isCached)
                {
                    LogHelper.LogToFile($"Core-IsLicenceValid: Cache'de veri bulundu. \nData: {entry.Data}\n:{entry.Signature}");
                    var expectedSignature = CacheSignatureHelper.ComputeHmac(entry.Data, publicKey);
                    LogHelper.LogToFile($"Core-IsLicenceValid: Beklenen imza: {expectedSignature}");
                    if (entry.Signature == expectedSignature && entry.Data.StartsWith("True"))
                    {
                        LogHelper.LogToFile("Core-IsLicenceValid: Cache'deki veri geçerli.");
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

                LogHelper.LogToFile("ForceLicenceCheck: Public key alındı: " + publicKey);
                LogHelper.LogToFile("ForceLicenceCheck: Hardware ID alındı: " + hardwareID);
                if (string.IsNullOrEmpty(publicKey))
                {
                    LogHelper.LogToFile("ForceLicenceCheck: Public key boş geldi.");
                    SaveResultToCache(false, "", "");
                    return;
                }
                if (string.IsNullOrEmpty(hardwareID))
                {
                    LogHelper.LogToFile("ForceLicenceCheck: HardwareID boş geldi.");
                    SaveResultToCache(false, "", "");
                    return;
                }


                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string licenceFolder = Path.Combine(appDataPath, "ITS");

                if (!Directory.Exists(licenceFolder))
                {
                    Directory.CreateDirectory(licenceFolder);
                }

                string licenceFilePath = Path.Combine(licenceFolder, "Licence.json");
                if (!File.Exists(licenceFilePath))
                {
                    LogHelper.LogToFile("ForceLicenceCheck: Licence.json dosyası bulunamadı. - File Path: " + licenceFilePath);
                    SaveResultToCache(false, publicKey, hardwareID);
                    return;
                }
                var encryptedJSON = File.ReadAllText(licenceFilePath);
                var signedJsonText = rsaHelper.DecryptString(encryptedJSON);
                if(string.IsNullOrEmpty(signedJsonText))
                {
                    LogHelper.LogToFile("ForceLicenceCheck: DecryptString boş veya hatalı çıktı verdi.");
                    SaveResultToCache(false, "", "");
                    return;
                }

                var jsonLicence = JsonSerializer.Deserialize<Licence>(signedJsonText);

                if (jsonLicence == null)
                {
                    LogHelper.LogToFile($"ForceLicenceCheck: JSON çözümleme başarısız veya lisans bilgisi bulunamadı.");
                    SaveResultToCache(false, "", "");
                    return;
                }

                string JSONData = $"{jsonLicence.CPUID.ToString().Trim()}" +
                    $"|||{jsonLicence.MAC.ToString().Trim()}" +
                    $"|||{jsonLicence.DISKNO.ToString().Trim()}" +
                    $"|||{jsonLicence.TYPE.ToString().Trim()}" +
                    $"|||{jsonLicence.DURATION.ToString().Trim()}" +
                    $"|||{jsonLicence.CREATED.ToString().Trim()}" +
                    $"|||{jsonLicence.EXPIRED.ToString().Trim()}";

                var signFromJSON = jsonLicence.SIGN;
                LogHelper.LogToFile($"ForceLicenceCheck: Doğrulanan Veri => {JSONData}");
                LogHelper.LogToFile($"ForceLicenceCheck: İmzalı Veri => {signFromJSON}");

                bool isVerified = RsaHelper.VerifySignatureWithHash(JSONData, signFromJSON, publicKey);
                if (!isVerified)
                {
                    LogHelper.LogToFile("ForceLicenceCheck: İmza doğrulaması başarısız. JSON verisi geçersiz veya değiştirilmiş.");
                    SaveResultToCache(false, publicKey, hardwareID);
                    return;
                }

                var hardwareId = hardwareID;


                // Örnek içerik: CPUID|||DISK|||MAC|||TYPE|||CREATE|||EXPIRE
                var isHardwareMatch = hardwareId == $"{jsonLicence.CPUID}|||{jsonLicence.MAC}|||{jsonLicence.DISKNO}";

                var licenceType = jsonLicence.TYPE;
                var createDate = DateTime.Parse(jsonLicence.CREATED);
                var expireDate = licenceType == 0 ? createDate.AddDays(30) : DateTime.Parse(jsonLicence.EXPIRED);

                var isValid = isHardwareMatch && DateTime.Now >= createDate && DateTime.Now <= expireDate;
                LogHelper.LogToFile($"ForceLicenceCheck: Donanım eşleşmesi: {isHardwareMatch}, Geçerlilik durumu: {isValid}, " +
                    $"CPU: {jsonLicence.CPUID}, " +
                    $"MAC:{jsonLicence.MAC}, " +
                    $"DISK:{jsonLicence.DISKNO}, " +
                    $"Lisans tipi: {licenceType}, " +
                    $"LisansSüresi:{jsonLicence.DURATION}, " +
                    $"Oluşturulma tarihi: {createDate}, Bitiş tarihi: {expireDate}");
                SaveResultToCache(isValid, publicKey, hardwareId);
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("ForceLicenceCheck-ERROR: " + ex.Message);
                return;
            }
        }

        private void SaveResultToCache(bool isValid, string publicKey, string hardwareId)
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
                var signature = CacheSignatureHelper.ComputeHmac(rawData, publicKey);

                var entry = new LicenceCacheEntry
                {
                    Data = rawData,
                    Signature = signature
                };

                _cache.Set(getCacheKey, entry, TimeSpan.FromHours(25));
                LogHelper.LogToFile($"SaveResultToCache: Cache'e kaydedildi. Key: {getCacheKey}, Data: {rawData}, Signature: {signature}");
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("SaveResultToCache-ERROR: " + ex.Message);
                return;
            }
        }
    }
}
