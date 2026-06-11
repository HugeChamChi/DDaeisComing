using System;
using System.Security.Cryptography;
using System.Text;

namespace Bathhouse.Save
{
    public static class CryptoUtility
    {
        // 256-bit Key, 128-bit IV
        private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("DDAE_BATHHOUSE_SECRET_KEY_256BIT"); // 32 bytes
        private static readonly byte[] DefaultIV = Encoding.UTF8.GetBytes("DDAE_IV_128BIT__"); // 16 bytes

        public static byte[] EncryptToBytes(string plainText, byte[] key = null, byte[] iv = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key ?? DefaultKey;
                aesAlg.IV = iv ?? DefaultIV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (System.IO.MemoryStream msEncrypt = new System.IO.MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (System.IO.StreamWriter swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        public static string DecryptFromBytes(byte[] cipherText, byte[] key = null, byte[] iv = null)
        {
            if (cipherText == null || cipherText.Length == 0)
                return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key ?? DefaultKey;
                aesAlg.IV = iv ?? DefaultIV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (System.IO.MemoryStream msDecrypt = new System.IO.MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (System.IO.StreamReader srDecrypt = new System.IO.StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
