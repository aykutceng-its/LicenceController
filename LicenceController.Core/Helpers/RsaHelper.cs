using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LicenceController.Core.Interfaces;

namespace LicenceController.Core.Helpers
{
    public class RsaHelper
    {
        private readonly IHardwareHelper _hardwareHelper;
        private readonly IRegistryHelper _registryHelper;
        public RsaHelper(IHardwareHelper hardwareHelper, IRegistryHelper registryHelper)
        {
            _hardwareHelper = hardwareHelper;
            _registryHelper = registryHelper;
        }

        public static bool VerifySignatureWithHash(string data, string signature, string publicKeyBase64)
        {
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
                    rsa.ImportRSAPublicKey(publicKeyBytes, out _);

                    byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                    byte[] signatureBytes = Convert.FromBase64String(signature);

                    return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("VerifySignatureWithHash: " + ex.Message);
                return false;
            }
        }


        public string DecryptString(string cipherTextBase64)
        {
            try
            {
                LogHelper.LogToFile($"DecryptString: Başladı. CipherText - {cipherTextBase64}");

                var fullCipher = Convert.FromBase64String(cipherTextBase64);
                if (fullCipher.Length < 16)
                {
                    LogHelper.LogToFile("DecryptString: Cipher text IV eksik!");
                    return string.Empty;
                }

                var secretKey = GenerateSecretKey();
                if (secretKey.Length == 0)
                {
                    LogHelper.LogToFile("DecryptString: Secret key oluşturulamadı!");
                    return string.Empty;
                }
                LogHelper.LogToFile($"DecryptString: SecretKey - {Convert.ToBase64String(secretKey)}");

                byte[] iv = fullCipher.Take(16).ToArray();
                byte[] cipher = fullCipher.Skip(16).ToArray();
                LogHelper.LogToFile($"DecryptString: IV - {Convert.ToBase64String(iv)}");

                using var aes = Aes.Create();
                aes.Key = secretKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(cipher);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);
                var result = reader.ReadToEnd();
                LogHelper.LogToFile($"DecryptString: Başarılı. \n -={result}=-");
                return result;
            }
            catch (Exception ex)
            {
                LogHelper.LogToFile($"DecryptString-ERROR: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }
        private byte[] GenerateSecretKey()
        {
            try
            {
                var secret = CachedInfo.GetHardwareId(_hardwareHelper) + _registryHelper.GetPublicKey();
                return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
            }
            catch(Exception ex)
            {
                LogHelper.LogToFile("GenerateSecretKey: " + ex.Message);
                return Array.Empty<byte>();
            }
        }
    }
}
