using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.Text;

namespace ECRS_API.DTOs.Security
{
    public static class PasswordHasher
    {
        // 將 Hash_Key 宣告為 nullable 以修正 CS8618
        private static string? Hash_Key { get; set; }

        // 格式: {iterations}.{saltBase64}.{hashBase64}
        public static string Hash(string password, string _hash_key)
        {
            Hash_Key = _hash_key;

            byte[] inputArray = Encoding.UTF8.GetBytes(password);
            var tripleDES = TripleDES.Create();
            var byteKey = Encoding.UTF8.GetBytes(Hash_Key);
            tripleDES.Key = byteKey;
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            return $"{Convert.ToBase64String(resultArray, 0, resultArray.Length)}";
        }

        public static bool Verify(string password, string stored, string _hash_key)
        {
            Hash_Key = _hash_key;
            string EncodePassword = PasswordHasher.Hash(password, Hash_Key);
            bool result = EncodePassword == stored ? true : false;
            return result;
        }
    }
}




