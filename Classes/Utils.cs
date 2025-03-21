using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace RichPresenceApp.Classes
{
    public static class Utils
    {
        // Get CS2 directory
        public static string? GetCS2Directory()
        {
            try
            {
                // Try to get CS2 directory from Steam
                string? steamPath = GetSteamPath();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    string cs2Path = Path.Combine(steamPath, "steamapps", "common", "Counter-Strike Global Offensive");
                    if (Directory.Exists(cs2Path))
                    {
                        return cs2Path;
                    }
                }

                // Try to find CS2 process
                Process[] processes = Process.GetProcessesByName("cs2");
                if (processes.Length > 0)
                {
                    try
                    {
                        string? processPath = processes[0].MainModule?.FileName;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            return Path.GetDirectoryName(processPath);
                        }
                    }
                    catch
                    {
                        // Ignore access denied errors
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error getting CS2 directory: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

        // Get Steam path from registry
        private static string? GetSteamPath()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam");
                if (key != null)
                {
                    string? steamPath = key.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        return steamPath.Replace("/", "\\");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error getting Steam path: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

        // Check if CS2 is running
        public static bool IsGameRunning()
        {
            try
            {
                // Check for cs2.exe process
                Process[] processes = Process.GetProcessesByName("cs2");
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking if CS2 is running: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        // Check if startup is enabled
        public static bool IsStartupEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                {
                    string? value = key.GetValue(Program.AppName) as string;
                    return !string.IsNullOrEmpty(value);
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking startup: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        // Enable startup
        public static bool EnableStartup()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                {
                    string appPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(appPath))
                    {
                        key.SetValue(Program.AppName, $"\"{appPath}\"");
                        ConsoleManager.WriteLine("Startup enabled.", ConsoleColor.Green);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error enabling startup: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        // Disable startup
        public static bool DisableStartup()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key != null)
                {
                    key.DeleteValue(Program.AppName, false);
                    ConsoleManager.WriteLine("Startup disabled.", ConsoleColor.Yellow);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error disabling startup: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }
    }
}