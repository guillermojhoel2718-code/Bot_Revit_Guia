using System;

namespace RevitTutor
{
    public class PluginConfig
    {
        public string GeminiApiKey { get; set; } = "";
        public string Language { get; set; } = "es-MX";
        public bool ShowEducationalTips { get; set; } = true;
        public bool ShowStartupHelp { get; set; } = true;
    }
}
