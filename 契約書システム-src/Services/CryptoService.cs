using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ContractManager.Services
{
    public static class CryptoService
    {
        private static readonly string KeyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Accounts", "admin_ecc_key.json");

        private class EccKeyData
        {
            public string PrivateKeyBase64 { get; set; } = string.Empty;
            public string PublicKeyBase64 { get; set; } = string.Empty;
        }

        /// <summary>
        /// Admin用のECC鍵ペアを初期化・取得する（なければ生成）
        /// </summary>
        public static ECDiffieHellman GetAdminKey()
        {
            if (!File.Exists(KeyFilePath))
            {
                var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                var keyData = new EccKeyData
                {
                    PrivateKeyBase64 = Convert.ToBase64String(ecdh.ExportECPrivateKey()),
                    PublicKeyBase64 = Convert.ToBase64String(ecdh.ExportSubjectPublicKeyInfo())
                };
                Directory.CreateDirectory(Path.GetDirectoryName(KeyFilePath)!);
                File.WriteAllText(KeyFilePath, JsonSerializer.Serialize(keyData));
                return ecdh;
            }
            else
            {
                var json = File.ReadAllText(KeyFilePath);
                var keyData = JsonSerializer.Deserialize<EccKeyData>(json)!;
                var ecdh = ECDiffieHellman.Create();
                ecdh.ImportECPrivateKey(Convert.FromBase64String(keyData.PrivateKeyBase64), out _);
                return ecdh;
            }
        }

        public static byte[] GetAdminPublicKey()
        {
            var adminKey = GetAdminKey();
            return adminKey.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// 画像（本人確認書類）をECC+AESハイブリッド暗号化する
        /// </summary>
        /// <returns>暗号化された画像データと、クライアントの公開鍵（AESキーの代わり）</returns>
        public static (string encryptedDataBase64, string clientPublicKeyBase64) EncryptIdDocument(byte[] imageData)
        {
            // 1. クライアントの一時的なECC鍵ペアを生成
            using var clientEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var clientPublicKey = clientEcdh.ExportSubjectPublicKeyInfo();

            // 2. Adminの公開鍵を取得
            using var adminPublicKeyEcdh = ECDiffieHellman.Create();
            adminPublicKeyEcdh.ImportSubjectPublicKeyInfo(GetAdminPublicKey(), out _);

            // 3. 共有シークレット（AESキー）を導出
            byte[] derivedAesKey = clientEcdh.DeriveKeyMaterial(adminPublicKeyEcdh.PublicKey);

            // 4. AES-256-GCMで画像を暗号化
            byte[] nonce = new byte[12]; // GCM nonce
            RandomNumberGenerator.Fill(nonce);

            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[imageData.Length];

            using (var aesGcm = new AesGcm(derivedAesKey, tagSizeInBytes: 16))
            {
                aesGcm.Encrypt(nonce, imageData, ciphertext, tag);
            }

            // 保存用に結合 (Nonce + Tag + Ciphertext)
            byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return (Convert.ToBase64String(result), Convert.ToBase64String(clientPublicKey));
        }

        /// <summary>
        /// 管理者が暗号化された画像を復号する
        /// </summary>
        public static byte[] DecryptIdDocument(string encryptedDataBase64, string clientPublicKeyBase64)
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedDataBase64);
            byte[] clientPublicKey = Convert.FromBase64String(clientPublicKeyBase64);

            // 1. クライアントの公開鍵をインポート
            using var clientPublicKeyEcdh = ECDiffieHellman.Create();
            clientPublicKeyEcdh.ImportSubjectPublicKeyInfo(clientPublicKey, out _);

            // 2. Adminの秘密鍵を取得して共有シークレット（AESキー）を導出
            using var adminEcdh = GetAdminKey();
            byte[] derivedAesKey = adminEcdh.DeriveKeyMaterial(clientPublicKeyEcdh.PublicKey);

            // 3. データを分解 (Nonce 12 bytes | Tag 16 bytes | Ciphertext)
            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[encryptedData.Length - 12 - 16];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, 12);
            Buffer.BlockCopy(encryptedData, 12, tag, 0, 16);
            Buffer.BlockCopy(encryptedData, 28, ciphertext, 0, ciphertext.Length);

            // 4. AES-256-GCMで復号
            byte[] plaintext = new byte[ciphertext.Length];
            using (var aesGcm = new AesGcm(derivedAesKey, tagSizeInBytes: 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }

        public static string ComputeSha256(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
