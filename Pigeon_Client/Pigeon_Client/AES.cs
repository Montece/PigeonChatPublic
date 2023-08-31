using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pigeon_Client
{
    public static class AES
    {
        public static string AESKey = "PigeonMontece";
        public static byte[] TheIV = Encoding.UTF8.GetBytes("7344400870123456");

        public static string EncodeString(string input)
        {
            return _EncodeString(input);
        }

        public static string DecodeString(string input)
        {
            return _DecodeString(input);
        }

        static string _EncodeString(string input)
        {
            using (var myAes = Aes.Create())
            {
                myAes.IV = TheIV;
                myAes.Padding = PaddingMode.None;
                var enc = new UTF8Encoding();
                myAes.Key = enc.GetBytes(ToMD5(AESKey));
                byte[] encrypted = AES.EncryptStringToBytesAes(input, myAes.Key, myAes.IV);

                return ByteArrayToString(encrypted);
            }
        }

        static string _DecodeString(string input)
        {
            using (var myAes = Aes.Create())
            {
                myAes.IV = TheIV;
                var enc = new UTF8Encoding();
                myAes.Key = enc.GetBytes(ToMD5(AESKey));
                string roundtrip = DecryptStringFromBytesAes(StringToByteArray(input), myAes.Key, myAes.IV);

                return roundtrip;
            }
        }

        static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        static string ToMD5(string input)
        {
            MD5 md5Hasher = MD5.Create();

            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        static byte[] EncryptStringToBytesAes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }

        static string DecryptStringFromBytesAes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            string plaintext;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }
    }
}