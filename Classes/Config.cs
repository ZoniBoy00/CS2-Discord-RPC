using System;
using System.IO;
using Newtonsoft.Json;

namespace RichPresenceApp.Classes
{
    public class Config
    {
        // Current configuration
        public static Config? Current { get; private set; }

        // Configuration file path
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CS2RPC",
            "config.json"
        );

        // JSON serializer settings - create once and reuse
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        // Discord application ID - hardcoded, not serialized
        [JsonIgnore]
        public string ApplicationId { get; } = "1352354388399882333"; // Using the application ID from the reference code

        // HTTP server host
        public string Host { get; set; } = "127.0.0.1";

        // HTTP server port
        public int HttpPort { get; set; } = 3000;

        // Display settings
        public bool ShowMap { get; set; } = true;
        public bool ShowGameMode { get; set; } = true;
        public bool ShowScore { get; set; } = true;
        public bool ShowTeam { get; set; } = true;

        // Load configuration
        public static void Load()
        {
            try
            {
                // Create config directory if it doesn't exist
                EnsureConfigDirectoryExists();

                // Check if config file exists
                if (File.Exists(ConfigFilePath))
                {
                    LoadExistingConfig();
                }
                else
                {
                    CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error loading configuration", ex);

                // Create default config
                Current = new Config();
            }
        }

        // Ensure config directory exists
        private static void EnsureConfigDirectoryExists()
        {
            string? directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        // Load existing config
        private static void LoadExistingConfig()
        {
            try
            {
                // Read config file
                string json = File.ReadAllText(ConfigFilePath);

                // Deserialize config
                var config = JsonConvert.DeserializeObject<Config>(json, _serializerSettings);

                // Set current config
                Current = config;

                ConsoleManager.LogImportant("Configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error loading existing config", ex);
                CreateDefaultConfig();
            }
        }

        // Create default config
        private static void CreateDefaultConfig()
        {
            // Create default config
            Current = new Config();

            // Save default config
            Save();

            ConsoleManager.LogImportant("Default configuration created.");
        }

        // Save configuration
        public static void Save()
        {
            try
            {
                if (Current == null)
                {
                    ConsoleManager.LogError("Configuration not loaded, cannot save");
                    return;
                }

                // Create config directory if it doesn't exist
                EnsureConfigDirectoryExists();

                // Serialize config with options
                string json = JsonConvert.SerializeObject(Current, _serializerSettings);

                // Write config file
                File.WriteAllText(ConfigFilePath, json);

                ConsoleManager.LogImportant("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error saving configuration", ex);
            }
        }
    }
}

