using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RichPresenceApp.Classes
{
    public static class Utils
    {
        // Cache for Steam path
        private static string? _cachedSteamPath = null;

        // Cache for CS2 directory
        private static string? _cachedCS2Directory = null;

        // Cache expiration
        private static DateTime _cacheExpiration = DateTime.MinValue;

        // Cache duration in minutes
        private const int CACHE_DURATION_MINUTES = 5;

        // Get CS2 directory with caching
        public static string? GetCS2Directory()
        {
            try
            {
                // Check if cache is valid
                if (_cachedCS2Directory != null && DateTime.Now < _cacheExpiration)
                {
                    return _cachedCS2Directory;
                }

                // Try to get CS2 directory from Steam
                string? steamPath = GetSteamPath();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    string cs2Path = Path.Combine(steamPath, "steamapps", "common", "Counter-Strike Global Offensive");
                    if (Directory.Exists(cs2Path))
                    {
                        // Update cache
                        _cachedCS2Directory = cs2Path;
                        _cacheExpiration = DateTime.Now.AddMinutes(CACHE_DURATION_MINUTES);
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
                            string? directory = Path.GetDirectoryName(processPath);

                            // Update cache
                            _cachedCS2Directory = directory;
                            _cacheExpiration = DateTime.Now.AddMinutes(CACHE_DURATION_MINUTES);

                            return directory;
                        }
                    }
                    catch
                    {
                        // Ignore access denied errors
                    }
                    finally
                    {
                        // Dispose processes
                        foreach (var process in processes)
                        {
                            process.Dispose();
                        }
                    }
                }

                // Reset cache if not found
                _cachedCS2Directory = null;
                return null;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error getting CS2 directory: {ex.Message}", ConsoleColor.Red);
                return null;
            }
        }

        // Get Steam path from registry with caching
        private static string? GetSteamPath()
        {
            try
            {
                // Return cached path if available
                if (_cachedSteamPath != null)
                {
                    return _cachedSteamPath;
                }

                using RegistryKey? key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam");
                if (key != null)
                {
                    string? steamPath = key.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        // Cache the result
                        _cachedSteamPath = steamPath.Replace("/", "\\");
                        return _cachedSteamPath;
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

        // Check if CS2 is running - optimized with process name array
        public static bool IsGameRunning()
        {
            try
            {
                // Check for cs2.exe process and other possible names
                string[] processNames = { "cs2", "csgo", "Counter-Strike 2", "Counter-Strike Global Offensive" };

                foreach (string processName in processNames)
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        // Dispose processes to prevent resource leaks
                        foreach (var process in processes)
                        {
                            process.Dispose();
                        }
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error checking if CS2 is running: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }
    }
}

