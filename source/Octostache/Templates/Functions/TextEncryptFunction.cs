using System;
using System.Security.Cryptography;
using System.Text;

namespace Octostache.Templates.Functions
{
    static class TextEncryptFunction
    {
        // OAEP with SHA-256 costs two hashes plus two bytes of the modulus, leaving the rest for the payload.
        const int OaepSha256Overhead = (2 * 32) + 2;

        public static string? RsaEncrypt(string? argument, string[] options)
        {
            if (argument == null)
                return null;

            if (options.Length != 1)
                return Error("expected a single argument holding a base64 encoded RSA public key");

#if NET462
            return Error("not supported when Octostache is running on .NET Framework");
#else
            byte[] subjectPublicKeyInfo;
            try
            {
                subjectPublicKeyInfo = Convert.FromBase64String(options[0]);
            }
            catch (FormatException)
            {
                return Error("the public key is not valid base64");
            }

            using (var rsa = RSA.Create())
            {
                try
                {
                    rsa.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
                }
                catch (CryptographicException)
                {
                    return Error("the public key could not be read as a SubjectPublicKeyInfo structure");
                }

                var data = Encoding.UTF8.GetBytes(argument);
                var maximum = (rsa.KeySize / 8) - OaepSha256Overhead;
                if (data.Length > maximum)
                    return Error($"the input is {data.Length} bytes, which is more than the {maximum} bytes a {rsa.KeySize} bit key can encrypt directly");

                try
                {
                    return Convert.ToBase64String(rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256));
                }
                catch (CryptographicException e)
                {
                    return Error(e.Message);
                }
            }
#endif
        }

        // Encryption failures are reported in the output rather than returned as null, which would leave the
        // raw `#{...}` in place and read as though no encryption had been asked for. Matches UriPart.
        static string Error(string message) => $"[RsaEncrypt error: {message}]";
    }
}
