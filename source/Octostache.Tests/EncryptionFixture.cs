using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Octostache.Tests
{
    public class EncryptionFixture : BaseFixture
    {
        [Fact]
        public void MissingArgumentIsReported()
        {
            var result = Evaluate("#{foo | RsaEncrypt}", new Dictionary<string, string> { { "foo", "Test" } });
            result.Should().Be("[RsaEncrypt error: expected a single argument holding a base64 encoded RSA public key]");
        }

        [Fact]
        public void TooManyArgumentsAreReported()
        {
            var result = Evaluate(@"#{foo | RsaEncrypt abc def}", new Dictionary<string, string> { { "foo", "Test" } });
            result.Should().Be("[RsaEncrypt error: expected a single argument holding a base64 encoded RSA public key]");
        }

        [Fact]
        public void MissingInputLeavesTheTemplateUnevaluated()
        {
            var result = Evaluate("#{missing | RsaEncrypt abc}", new Dictionary<string, string>())
                .Replace("\"", ""); // function parameters have quotes added when evaluated back to a string, so we need to remove them
            result.Should().Be("#{missing | RsaEncrypt abc}");
        }

#if NETFRAMEWORK
        [Fact]
        public void EncryptionIsReportedAsUnsupportedOnDotNetFramework()
        {
            using (var rsa = RSA.Create(2048))
            {
                var result = Encrypt("Test", PublicKeyOf(rsa));
                result.Should().Be("[RsaEncrypt error: not supported when Octostache is running on .NET Framework]");
            }
        }
#else
        [Fact]
        public void EncryptedValueRoundTripsWithThePrivateKey()
        {
            using (var rsa = RSA.Create(2048))
            {
                var result = Encrypt("Test", PublicKeyOf(rsa));

                var plainText = rsa.Decrypt(Convert.FromBase64String(result), RSAEncryptionPadding.OaepSHA256);
                Encoding.UTF8.GetString(plainText).Should().Be("Test");
            }
        }

        [Fact]
        public void EncryptingTheSameValueTwiceProducesDifferentCipherText()
        {
            using (var rsa = RSA.Create(2048))
            {
                var publicKey = PublicKeyOf(rsa);

                Encrypt("Test", publicKey).Should().NotBe(Encrypt("Test", publicKey));
            }
        }

        [Fact]
        public void NonBase64PublicKeyIsReported()
        {
            var result = Encrypt("Test", "not base64!");
            result.Should().Be("[RsaEncrypt error: the public key is not valid base64]");
        }

        [Fact]
        public void PublicKeyThatIsNotSubjectPublicKeyInfoIsReported()
        {
            var result = Encrypt("Test", Convert.ToBase64String(Encoding.UTF8.GetBytes("this is not a key")));
            result.Should().Be("[RsaEncrypt error: the public key could not be read as a SubjectPublicKeyInfo structure]");
        }

        [Fact]
        public void InputLongerThanTheKeyCanEncryptIsReported()
        {
            using (var rsa = RSA.Create(2048))
            {
                var result = Encrypt(new string('a', 200), PublicKeyOf(rsa));
                result.Should().Be("[RsaEncrypt error: the input is 200 bytes, which is more than the 190 bytes a 2048 bit key can encrypt directly]");
            }
        }

        [Fact]
        public void InputAtTheLimitOfWhatTheKeyCanEncryptIsAccepted()
        {
            using (var rsa = RSA.Create(2048))
            {
                var value = new string('a', 190);
                var result = Encrypt(value, PublicKeyOf(rsa));

                var plainText = rsa.Decrypt(Convert.FromBase64String(result), RSAEncryptionPadding.OaepSHA256);
                Encoding.UTF8.GetString(plainText).Should().Be(value);
            }
        }

        static string PublicKeyOf(RSA rsa) => Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
#endif

        string Encrypt(string value, string publicKey)
            => Evaluate("#{foo | RsaEncrypt #{key}}",
                new Dictionary<string, string>
                {
                    { "foo", value },
                    { "key", publicKey },
                });
    }
}
