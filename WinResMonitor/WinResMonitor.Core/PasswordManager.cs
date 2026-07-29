using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinResMonitor.Core
{
    public class PasswordManager
    {
        private readonly string _configPath;

        private record PasswordConfig(string Salt, string Hash);

        public PasswordManager()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WinResMonitor");
            Directory.CreateDirectory(dir);
            _configPath = Path.Combine(dir, "auth.json");
        }

        public bool IsPasswordSet => File.Exists(_configPath);

        public void SetPassword(string password)
        {
            var salt = GenerateSalt();
            var hash = Hash(password, salt);
            var json = JsonSerializer.Serialize(new PasswordConfig(salt, hash), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public bool Verify(string password)
        {
            if (!IsPasswordSet) return false;
            try
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<PasswordConfig>(json);
                if (config == null) return false;
                return Hash(password, config.Salt) == config.Hash;
            }
            catch { return false; }
        }

        private static string GenerateSalt()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string Hash(string password, string salt)
        {
            var data = Encoding.UTF8.GetBytes(salt + password);
            var hash = SHA256.HashData(data);
            return Convert.ToBase64String(hash);
        }
    }
}
