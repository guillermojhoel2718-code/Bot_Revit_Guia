using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace RevitTutor
{
    public static class ConfigService
    {
        private static string GetPluginFolder()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation);
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetPluginFolder(), "RevitTutor.apikey");
        }

        public static PluginConfig LoadConfig()
        {
            var config = new PluginConfig();
            try
            {
                var path = GetConfigPath();
                if (!File.Exists(path))
                {
                    // Fallback a ProgramData
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Autodesk", "Revit", "Addins", "2025", "RevitTutor.apikey");
                }

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("geminiApiKey", out var keyProp))
                    {
                        string key = keyProp.GetString() ?? "";
                        if (key.StartsWith("Alza")) key = "AI" + key.Substring(2);
                        config.GeminiApiKey = key;
                    }
                    
                    if (root.TryGetProperty("language", out var langProp))
                        config.Language = langProp.GetString() ?? "es-MX";
                    
                    if (root.TryGetProperty("showEducationalTips", out var tipsProp))
                        config.ShowEducationalTips = tipsProp.GetBoolean();
                    
                    if (root.TryGetProperty("showStartupHelp", out var helpProp))
                        config.ShowStartupHelp = helpProp.GetBoolean();
                }
            }
            catch { }
            return config;
        }

        public static void SaveConfig(PluginConfig config)
        {
            try
            {
                var path = GetConfigPath();
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        // Legacy support for just the key
        public static string LoadApiKey() => LoadConfig().GeminiApiKey;
        public static void SaveApiKey(string key)
        {
            var config = LoadConfig();
            config.GeminiApiKey = key;
            SaveConfig(config);
        }
    }
}
