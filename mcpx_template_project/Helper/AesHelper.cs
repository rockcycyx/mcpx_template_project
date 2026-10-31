using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace mcpx_template_project.Helper
{
    /// <summary>
    /// AES 双向加密工具（CBC + PKCS7，256 位密钥）。
    /// 用于授权到期时间 dt 等敏感字段的加密存储与解密读取。
    /// </summary>
    /// <author>Rudeus</author>
    public static class AesHelper
    {
        // 32 字节密钥（256 位）。生产环境建议从安全配置读取，此处为固定密钥保证加解密一致性。
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("mcpx_template_project.License.2026.AES.32.Key");

        // 16 字节 IV（CBC 模式）。固定 IV 保证相同明文产生相同密文，便于 INI 中稳定存储比对。
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("mcpx_template_projectIV16Byte");

        /// <summary>
        /// 加密明文，返回 Base64 字符串。空串原样返回空串。
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(cipherBytes);
        }

        /// <summary>
        /// 解密 Base64 密文，返回明文。空串原样返回空串；解密失败返回原串（兼容明文旧数据）。
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // 兼容：若原数据是明文（非 Base64 或非本密钥加密），直接返回原串
                return cipherText;
            }
        }
    }
}
