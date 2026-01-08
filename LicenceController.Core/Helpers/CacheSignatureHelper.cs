using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LicenceController.Core.Helpers
{
    public static class CacheSignatureHelper
    {
        public static string ComputeHmac(string data, string key)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash) ?? string.Empty;
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("CacheSignatureHelper-ERROR: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
