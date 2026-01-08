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
                var programDataPath = AppDomain.CurrentDomain.BaseDirectory;;
                var licenceFolder = Path.Combine(programDataPath, "ITS");
                var publicKeyFilePath = Path.Combine(licenceFolder, "pubKey.pub");

                if (!File.Exists(publicKeyFilePath))
                {
                    LogHelper.LogToFile("Core: PublicKey dosyası bulunamadı!");
                    return string.Empty;
                }

                using (var reader = new StreamReader(publicKeyFilePath))
                {
                    return reader.ReadToEnd().Trim() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogToFile("Core: PublicKey dosyasını okurken hata oluştu: " + ex.Message);
                return string.Empty;
            }
        }


    }
}
