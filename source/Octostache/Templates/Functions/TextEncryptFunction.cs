using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Octostache.Templates.Functions
{
    internal class TextEncryptFunction
    {
        public static string RSAEncrypt(string data, string[] arguments)
        {
            if (arguments == null || arguments.Length == 0 || arguments.Length > 2)
            {
                return null;
            }

            try
            {

                var certificate = new X509Certificate2(Convert.FromBase64String(arguments[0]));
#if NET40
                var publicKey = (RSACryptoServiceProvider)certificate.PublicKey.Key;
#else
                var publicKey = certificate.GetRSAPublicKey();
#endif
                using (publicKey)
                {
                    var encoding = Encoding.GetEncoding(arguments.Length > 1 ? arguments[1] : "utf-8");
                    var byteData = encoding.GetBytes(data);
#if NET40
                    var encryptedBytes = publicKey.Encrypt(byteData, true);
#else
                    var encryptedBytes = publicKey.Encrypt(byteData, RSAEncryptionPadding.OaepSHA1);
#endif
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}