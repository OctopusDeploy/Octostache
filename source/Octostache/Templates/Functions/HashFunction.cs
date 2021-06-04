using System;
using System.Security.Cryptography;
using System.Text;

namespace Octostache.Templates.Functions
{
    static class HashFunction
    {
        public static string? Md5(string? argument, string[] options)
        {
            return CalculateHash(MD5.Create, argument, options);
        }

        public static string? Sha1(string? argument, string[] options)
        {
            return CalculateHash(SHA1.Create, argument, options);
        }

        public static string? Sha256(string? argument, string[] options)
        {
            return CalculateHash(SHA256.Create, argument, options);
        }

        public static string? Sha384(string? argument, string[] options)
        {
            return CalculateHash(SHA384.Create, argument, options);
        }

        public static string? Sha512(string? argument, string[] options)
        {
            return CalculateHash(SHA512.Create, argument, options);
        }

        static string? CalculateHash(Func<HashAlgorithm> algorithm, string? argument, string[] options)
        {
            if (argument == null)
            {
                return null;
            }

            var hashOptions = new HashOptions(options);
            if (!hashOptions.IsValid)
            {
                return null;
            }

            try
            {
                var argumentBytes = hashOptions.GetBytes(argument);

                var hsh = algorithm();
                return HexDigest(hsh.ComputeHash(argumentBytes), hashOptions);
            }
            catch
            {
                // Likely invalid input
                return null;
            }
        }

        static string HexDigest(byte[] bytes, HashOptions options)
        {
            var size = options.DigestSize.GetValueOrDefault(bytes.Length);
            if (size > bytes.Length)
            {
                size = bytes.Length;
            }
            var sb = new StringBuilder(size * 2);

            for (var i = 0; i < size; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }

        class HashOptions
        {
            public HashOptions(string[] options)
            {
                if (options.Length == 0)
                {
                    return;
                }
                if (options.Length > 2)
                {
                    IsValid = false;
                    return;
                }

                if (int.TryParse(options[0], out var size) && size > 0)
                {
                    DigestSize = size;
                    if (options.Length > 1)
                    {
                        var encoding = GetEncoding(options[1]);
                        if (encoding == null)
                        {
                            IsValid = false;
                        }
                        else
                        {
                            GetBytes = encoding;
                        }
                    }
                }
                else
                {
                    var encoding = GetEncoding(options[0]);
                    if (encoding == null)
                    {
                        IsValid = false;
                    }
                    else
                    {
                        GetBytes = encoding;
                    }

                    if (IsValid && options.Length > 1)
                    {
                        if (int.TryParse(options[1], out size) && size > 0)
                        {
                            DigestSize = size;
                        }
                        else
                        {
                            IsValid = false;
                        }
                    }
                }
            }

            public Func<string, byte[]> GetBytes { get; } = Encoding.UTF8.GetBytes;
            public int? DigestSize { get; }

            public bool IsValid { get; } = true;

            static Func<string, byte[]>? GetEncoding(string encoding)
            {
                switch (encoding.ToLowerInvariant())
                {
                    case "base64":
                        return Convert.FromBase64String;
                    case "utf8":
                    case "utf-8":
                        return Encoding.UTF8.GetBytes;
                    case "unicode":
                        return Encoding.Unicode.GetBytes;
                }

                return null;
            }
        }
    }
}
