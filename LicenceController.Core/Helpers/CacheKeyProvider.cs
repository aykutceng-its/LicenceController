using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LicenceController.Core.Helpers
{
    public static class CacheKeyProvider
    {
        public static string GetCacheKey(string privateKey, string hardwareId)
        {
            try
            {
                var rawKey = privateKey + hardwareId;
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
                var base64 = Convert.ToBase64String(hash);
                return "Licence:" + base64.Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 32) ?? string.Empty;
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("CacheKeyProvider-ERROR: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
