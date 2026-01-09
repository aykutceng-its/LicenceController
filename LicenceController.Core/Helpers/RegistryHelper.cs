using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LicenceController.Core.Interfaces;
using LicenceController.Core.Services;
using LicenceController.Core.Helpers;
using Microsoft.Win32;

namespace LicenceController.Core.Helpers
{

    public class RegistryHelper : IRegistryHelper
    {
        public string GetPublicKey()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var licenceFolder = Path.Combine(baseDir, "licence");
                var publicKeyFilePath = Path.Combine(licenceFolder, "pubKey.pub");

                if (File.Exists(publicKeyFilePath))
                {
                    return File.ReadAllText(publicKeyFilePath).Trim();
                }
                
                LogHelper.LogToFile("Core: PublicKey dosyası bulunamadı! Aranan Yol: " + publicKeyFilePath);
                return string.Empty;

            }
            catch (Exception ex)
            {
                LogHelper.LogToFile("Core: PublicKey dosyasını okurken hata oluştu: " + ex.Message);
                return string.Empty;
            }
        }


    }
}
