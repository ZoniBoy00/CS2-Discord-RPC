using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        // Discord application ID - hardcoded, not serialized
        [JsonIgnore]
        public string ApplicationId { get; } = "DISCORD_CLIENT_ID"; // Replace with your actual Discord application ID

        // HTTP server host
        public string Host { get; set; } = "127.0.0.1";

        // HTTP server port
        public int HttpPort { get; set; } = 3000;

        // Display settings
        public bool ShowMap { get; set; } = true;
        public bool ShowGameMode { get; set; } = true;
        public bool ShowScore { get; set; } = true;
        public bool ShowTeam { get; set; } = true;

        // Application settings
        public bool MinimizeToTray { get; set; } = false;
        public bool StartWithWindows { get; set; } = true;

        // Load configuration
        public static void Load()
        {
            try
            {
                // Create config directory if it doesn't exist
                string? directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if config file exists
                if (File.Exists(ConfigFilePath))
                {
                    // Read config file
                    string json = File.ReadAllText(ConfigFilePath);

                    // Deserialize config
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var config = JsonSerializer.Deserialize<Config>(json, options);

                    // Set current config
                    Current = config;

                    ConsoleManager.WriteLine("Configuration loaded successfully.", ConsoleColor.Green, true);
                }
                else
                {
                    // Create default config
                    Current = new Config();

                    // Save default config
                    Save();

                    ConsoleManager.WriteLine("Default configuration created.", ConsoleColor.Green, true);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error loading configuration: {ex.Message}", ConsoleColor.Red, true);

                // Create default config
                Current = new Config();
            }
        }

        // Save configuration
        public static void Save()
        {
            try
            {
                if (Current == null)
                {
                    ConsoleManager.WriteLine("Configuration not loaded, cannot save", ConsoleColor.Red, true);
                    return;
                }

                // Create config directory if it doesn't exist
                string? directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize config with options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(Current, options);

                // Write config file
                File.WriteAllText(ConfigFilePath, json);

                ConsoleManager.WriteLine("Configuration saved successfully.", ConsoleColor.Green, true);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error saving configuration: {ex.Message}", ConsoleColor.Red, true);
            }
        }
    }
}