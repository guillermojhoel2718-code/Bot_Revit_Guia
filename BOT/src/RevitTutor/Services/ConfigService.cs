using System;
using System.IO;
using System.Text.Json;

namespace RevitTutor
{
    public class TutorConfig
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public static class ConfigService
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitTutor");
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

        public static void SaveApiKey(string apiKey)
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            var config = new TutorConfig { ApiKey = apiKey };
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static string LoadApiKey()
        {
            if (!File.Exists(ConfigPath))
            {
                return string.Empty;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<TutorConfig>(json);
                return config?.ApiKey ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
