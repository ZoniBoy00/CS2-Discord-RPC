using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace RichPresenceApp.Classes
{
    public static class ApplicationSetup
    {
        // Setup application
        public static void Setup()
        {
            try
            {
                // Create GSI config
                CreateGSIConfig();
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error during application setup", ex);
            }
        }

        // Create GSI config
        private static void CreateGSIConfig()
        {
            try
            {
                if (Config.Current == null)
                {
                    ConsoleManager.LogError("Configuration not loaded, cannot create GSI config");
                    return;
                }

                // Define possible CS2 installation paths - reduced to most common paths
                var possiblePaths = new List<string>
                {
                    // Original CS:GO path
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg"),
                    
                    // CS2 paths
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike 2", "game", "csgo", "cfg"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike 2", "csgo", "cfg")
                };

                // Try to get CS2 directory from Utils
                string? cs2Dir = Utils.GetCS2Directory();
                if (!string.IsNullOrEmpty(cs2Dir))
                {
                    // Add potential cfg paths based on detected CS2 directory
                    possiblePaths.Add(Path.Combine(cs2Dir, "game", "csgo", "cfg"));
                    possiblePaths.Add(Path.Combine(cs2Dir, "csgo", "cfg"));
                }

                // Check if any of the paths exist
                string? existingPath = possiblePaths.FirstOrDefault(Directory.Exists);

                // If no path exists, return
                if (existingPath == null)
                {
                    ConsoleManager.LogError("Could not find CS2 installation directory");
                    return;
                }

                // Create gamestate_integration directory if it doesn't exist
                string gsiDir = Path.Combine(existingPath, "gamestate_integration");
                if (!Directory.Exists(gsiDir))
                {
                    try
                    {
                        Directory.CreateDirectory(gsiDir);
                        existingPath = gsiDir;
                    }
                    catch (Exception ex)
                    {
                        ConsoleManager.LogError("Failed to create gamestate_integration directory", ex);
                        // Continue with the base cfg directory
                    }
                }
                else
                {
                    // Use the gamestate_integration directory if it exists
                    existingPath = gsiDir;
                }

                // Create GSI config file path
                string gsiConfigPath = Path.Combine(existingPath, "gamestate_integration_cs2rpc.cfg");

                // Check if GSI config already exists
                if (File.Exists(gsiConfigPath))
                {
                    ConsoleManager.LogImportant($"CS2 GSI config already exists at: {gsiConfigPath}");
                    return;
                }

                // Create GSI config content
                var gsiConfig = new StringBuilder(512); // Pre-allocate capacity
                gsiConfig.AppendLine("\"CS2 Rich Presence\"");
                gsiConfig.AppendLine("{");
                gsiConfig.AppendLine($"    \"uri\"          \"http://{Config.Current.Host}:{Config.Current.HttpPort}\"");
                gsiConfig.AppendLine("    \"timeout\"      \"5.0\"");
                gsiConfig.AppendLine("    \"buffer\"       \"0.1\"");
                gsiConfig.AppendLine("    \"throttle\"     \"0.5\""); // Increased throttle to reduce updates
                gsiConfig.AppendLine("    \"heartbeat\"    \"60.0\""); // Increased heartbeat interval
                gsiConfig.AppendLine("    \"data\"");
                gsiConfig.AppendLine("    {");
                gsiConfig.AppendLine("        \"provider\"             \"1\"");
                gsiConfig.AppendLine("        \"map\"                  \"1\"");
                gsiConfig.AppendLine("        \"round\"                \"1\"");
                gsiConfig.AppendLine("        \"player_id\"            \"1\"");
                gsiConfig.AppendLine("        \"player_state\"         \"1\"");
                gsiConfig.AppendLine("        \"player_weapons\"       \"0\""); // Disabled to reduce data
                gsiConfig.AppendLine("        \"player_match_stats\"   \"0\""); // Disabled to reduce data
                gsiConfig.AppendLine("    }");
                gsiConfig.AppendLine("}");

                // Write GSI config to file
                File.WriteAllText(gsiConfigPath, gsiConfig.ToString());

                ConsoleManager.LogImportant($"CS2 GSI config created at: {gsiConfigPath}");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error creating GSI config", ex);
            }
        }
    }
}

