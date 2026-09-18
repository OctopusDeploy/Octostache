using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Octostache.Templates.Functions
{
    static class HashBucketFunction
    {
        /// <summary>
        /// Deterministically maps a value into one of <c>bucketCount</c> buckets, returning an integer in <c>0..bucketCount-1</c>.
        /// The mapping is stable across platforms and processes: it is derived from the SHA256 digest of the UTF-8 bytes of the
        /// value, rather than from <see cref="object.GetHashCode" />, which is randomised per process.
        /// SHA256 is also used in preference to MD5/SHA1 because those are unavailable when FIPS mode is enforced.
        /// Balance between buckets is statistical rather than exact.
        /// An invalid bucket count returns an embedded error rather than echoing the template, so that a typo is visible
        /// instead of silently producing a plausible-looking result.
        /// </summary>
        public static string? HashBucket(string? argument, string[] options)
        {
            if (options.Length != 1)
            {
                return Error.Format("expected a single bucket count");
            }

            if (!int.TryParse(options[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bucketCount) || bucketCount < 1)
            {
                return Error.Format($"bucket count '{options[0]}' is not a positive integer");
            }

            if (string.IsNullOrEmpty(argument))
            {
                return string.Empty;
            }

            using (var algorithm = SHA256.Create())
            {
                var digest = algorithm.ComputeHash(Encoding.UTF8.GetBytes(argument));

                // An unsigned reading of a fixed slice of the digest, so the bucket can never be negative.
                var value = ((uint) digest[0] << 24) | ((uint) digest[1] << 16) | ((uint) digest[2] << 8) | digest[3];

                return (value % (uint) bucketCount).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
