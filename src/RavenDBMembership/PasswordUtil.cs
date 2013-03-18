using System;
using System.Security.Cryptography;
using System.Text;
using CryptSharp;
using CryptSharp.Utility;

namespace RavenDBMembership
{
    public static class PasswordUtil
    {
        public static string CreateRandomSalt()
        {
            var rng = new RNGCryptoServiceProvider();
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);

            var str = new String(UnixBase64.Encode(saltBytes));
            return str;
        }

        public static string HashPassword(string pass, string salt)
        {
            // these number must not be changed, otherwise strings generated will be different and user's won't be able to login.
            var derivedBytes = new byte[128];   // 128 is a number of bytes taken back from the encoding stream
            const int cost = 512; // cost of the algorithm - how many time we go round. Must be power of 2
            const int blockSize = 8;    
            const int parallel = 16;    // maximum number of threads to use. 
            SCrypt.ComputeKey(Encoding.Unicode.GetBytes(pass), Encoding.Unicode.GetBytes(salt), cost, blockSize, parallel, null, derivedBytes);

            var hashPassword = new string(UnixBase64.Encode(derivedBytes));
            return hashPassword;
        }
    }
}
