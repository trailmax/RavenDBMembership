using System;
using System.Collections.Generic;
using System.Text;
using CryptSharp;

namespace RavenDBMembership
{
    public static class PasswordUtil
    {
        public static string CreateRandomSalt()
        {
            return Crypter.Blowfish.GenerateSalt(128);
        }

        public static string HashPassword(string pass, string salt, string hashAlgorithm, string macKey)
        {
            // these number must not be changed, otherwise strings generated will be different and user's won't be able to login.
            var derivedBytes = new byte[128];   // 128 is a number of bytes taken back from the encoding stream
            const int cost = 1024; // cost of the algorithm - how many time we go round. Must be power of 2
            const int blockSize = 8;    
            const int parallel = 16;    // maximum number of threads to use. 
            CryptSharp.Utility.SCrypt.ComputeKey(Encoding.ASCII.GetBytes(pass), Encoding.ASCII.GetBytes(salt), cost, blockSize, parallel, null, derivedBytes);

            return derivedBytes.ToString();

        }
    }
}
