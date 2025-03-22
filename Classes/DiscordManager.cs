using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Windows.Forms;

namespace RichPresenceApp.Classes
{
    public class DiscordManager : IDisposable
    {
        // Discord RPC client
        private DiscordRpcClient? _client;

        // Last updated presence for comparison
        private string? _lastPresenceHash;

        // Flag to track if presence is cleared
        private volatile bool _isPresenceCleared = true;

        // Lock object for thread safety
        private readonly object _presenceLock = new object();

        // Timestamp when CS2 was first detected as running
        private DateTime _cs2StartTime;

        // Flag to track if player is in a match
        private bool _isInMatch = false;

        // Last known map
        private string _lastKnownMap = "Unknown";

        // Game mode mapping - use ConcurrentDictionary for thread safety
        private readonly ConcurrentDictionary<string, string> _gameModeMap;

        // Reusable StringBuilder for string operations
        private readonly System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder(128);

        // Throttle presence updates to reduce Discord API calls
        private DateTime _lastPresenceUpdateTime = DateTime.MinValue;
        private const int PRESENCE_UPDATE_THROTTLE_MS = 1000; // Limit updates to once per second

        // Constructor
        public DiscordManager()
        {
            // Initialize game mode map
            _gameModeMap = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            InitializeGameModeMap();
        }

        // Initialize game mode map
        private void InitializeGameModeMap()
        {
            var modes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "casual", "Casual" },
                { "competitive", "Competitive" },
                { "deathmatch", "Deathmatch" },
                { "gungameprogressive", "Arms Race" },
                { "gungametrbomb", "Demolition" },
                { "training", "Training" },
                { "custom", "Custom" },
                { "cooperative", "Cooperative" },
                { "coopmission", "Guardian" },
                { "skirmish", "Skirmish" },
                { "survival", "Danger Zone" },
                { "scrimcomp2v2", "Wingman" },
                { "scrimcomp5v5", "Premier" },
                { "teamdeathmatch", "Team Deathmatch" },
                { "retakes", "Retakes" },
                // Reduced number of entries to save memory
                { "dangerzone", "Danger Zone" },
                { "premier", "Premier" },
                { "wingman", "Wingman" },
                { "matchmaking", "Competitive" },
                { "workshop", "Workshop" }
            };

            foreach (var kvp in modes)
            {
                _gameModeMap[kvp.Key] = kvp.Value;
            }
        }

        // Initialize Discord RPC
        public void Initialize()
        {
            try
            {
                ConsoleManager.LogImportant("Initializing Discord RPC...");

                if (Config.Current == null)
                {
                    ConsoleManager.LogError("Configuration not loaded, cannot initialize Discord RPC");
                    return;
                }

                // Dispose existing client if any
                _client?.Dispose();

                // Get the application ID from config
                string appId = Config.Current.ApplicationId;

                // Validate application ID - only check if it's empty or placeholder
                if (string.IsNullOrEmpty(appId) || appId == "DISCORD_CLIENT_ID")
                {
                    ConsoleManager.LogError("Invalid Discord Application ID. Please update the ApplicationId in Config.cs");
                    MessageBox.Show(
                        "Invalid Discord Application ID. Please update the ApplicationId in Config.cs with your Discord application ID.",
                        "Configuration Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }

                ConsoleManager.LogImportant($"Using Discord Application ID: {appId}");

                // Create client using the DiscordRichPresence library with custom logger
                _client = new DiscordRpcClient(appId)
                {
                    Logger = new DiscordLogger()
                };

                // Subscribe to events
                _client.OnReady += (_, e) =>
                    ConsoleManager.LogImportant($"Connected to Discord successfully as {e.User.Username}");

                _client.OnConnectionFailed += (_, e) =>
                    ConsoleManager.LogError($"Connection to Discord failed: {e.FailedPipe}");

                _client.OnConnectionEstablished += (_, e) =>
                    ConsoleManager.LogDebug($"Connection to Discord established on pipe: {e.ConnectedPipe}");

                _client.OnPresenceUpdate += (_, e) =>
                    ConsoleManager.LogDebug($"Presence updated: {e.Presence?.Details}");

                // Initialize the client
                bool initialized = _client.Initialize();

                // Check if client is initialized
                if (initialized)
                {
                    ConsoleManager.LogImportant("Discord RPC initialized successfully.");

                    // Clear presence initially - don't show anything until game is detected
                    ClearPresence();
                }
                else
                {
                    ConsoleManager.LogError("Failed to initialize Discord RPC client. Make sure Discord is running.");
                    MessageBox.Show(
                        "Failed to connect to Discord. Please make sure Discord is running and try again.",
                        "Connection Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error initializing Discord RPC", ex);
            }
        }

        // Optimize UpdatePresence method to reduce unnecessary updates
        public void UpdatePresence(GameState gameState)
        {
            if (_client == null || !_client.IsInitialized)
                return;

            // Throttle updates to reduce Discord API calls
            if ((DateTime.UtcNow - _lastPresenceUpdateTime).TotalMilliseconds < PRESENCE_UPDATE_THROTTLE_MS)
            {
                return;
            }

            try
            {
                lock (_presenceLock)
                {
                    // Only update presence if CS2 is running
                    if (!Program.IsGameRunning)
                    {
                        if (!_isPresenceCleared)
                        {
                            ClearPresence();
                            // Reset match state
                            _isInMatch = false;
                            _lastKnownMap = "Unknown";
                        }
                        return;
                    }

                    // Check if we have map data
                    bool hasMapData = !string.IsNullOrEmpty(gameState.CurrentMap) && gameState.CurrentMap != "Unknown";

                    // Update match state
                    bool matchStateChanged = UpdateMatchState(gameState, hasMapData);

                    // Special handling for ESC menu
                    bool isEscMenu = _isInMatch && gameState.CurrentActivity == "menu" && hasMapData;

                    // Build presence based on game state and match status
                    RichPresence presence = BuildPresence(gameState, isEscMenu);

                    // Calculate hash for comparison (excluding timestamp)
                    string presenceHash = CalculatePresenceHash(presence);

                    // Only update if presence has changed or match state changed
                    if (presenceHash != _lastPresenceHash || matchStateChanged)
                    {
                        // Update presence - don't call Invoke() here
                        _client.SetPresence(presence);

                        _lastPresenceHash = presenceHash;
                        _isPresenceCleared = false;
                        _lastPresenceUpdateTime = DateTime.UtcNow;

                        // Log significant presence changes
                        if (matchStateChanged)
                        {
                            LogPresenceChange();
                        }
                        else
                        {
                            ConsoleManager.LogDebug($"Updated presence: {presence.Details} | {presence.State}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error updating presence", ex);
                // Try to reinitialize Discord client
                TryReinitialize();
            }
        }

        // Update match state - modified to return if state changed
        private bool UpdateMatchState(GameState gameState, bool hasMapData)
        {
            bool stateChanged = false;

            // If we have map data and we're not in a match, enter match state
            if (hasMapData && !_isInMatch)
            {
                _isInMatch = true;
                _lastKnownMap = gameState.CurrentMap;
                ConsoleManager.LogImportant($"Player entered match on map: {_lastKnownMap}");
                stateChanged = true;
            }
            // If we don't have map data and we're in a match, exit match state
            else if (!hasMapData && _isInMatch)
            {
                _isInMatch = false;
                ConsoleManager.LogImportant("Player returned to main menu");
                stateChanged = true;
            }
            // If we're in a match and have map data, check if map changed
            else if (hasMapData && _isInMatch && _lastKnownMap != gameState.CurrentMap)
            {
                ConsoleManager.LogImportant($"Map changed from {_lastKnownMap} to {gameState.CurrentMap}");
                _lastKnownMap = gameState.CurrentMap;
                stateChanged = true;
            }

            return stateChanged;
        }

        // Log presence change - simplified
        private void LogPresenceChange()
        {
            if (_isInMatch)
            {
                ConsoleManager.LogImportant($"Updated presence: In match on {_lastKnownMap}");
            }
            else
            {
                ConsoleManager.LogImportant("Updated presence: In main menu");
            }
        }

        // Try to reinitialize Discord client if it fails
        private void TryReinitialize()
        {
            try
            {
                // Dispose old client
                _client?.Dispose();
                _client = null;

                // Wait a moment
                Thread.Sleep(1000);

                // Reinitialize
                Initialize();
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error reinitializing Discord client", ex);
            }
        }

        // Set default presence when not in game
        public void SetDefaultPresence()
        {
            try
            {
                if (_client == null || !_client.IsInitialized)
                    return;

                // Only set default presence if CS2 is running
                if (!Program.IsGameRunning)
                {
                    ClearPresence();
                    return;
                }

                // Reset match state when setting default presence
                _isInMatch = false;
                _lastKnownMap = "Unknown";

                // Set the start time to now when game is first detected
                _cs2StartTime = DateTime.UtcNow;

                ConsoleManager.LogImportant("Setting default Discord presence");

                var presence = new RichPresence
                {
                    Details = "In Main Menu",
                    State = "Waiting for a match",
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };

                // Update presence - don't call Invoke() here
                _client.SetPresence(presence);

                _lastPresenceHash = CalculatePresenceHash(presence);
                _isPresenceCleared = false;
                _lastPresenceUpdateTime = DateTime.UtcNow;
                ConsoleManager.LogImportant("Default presence set successfully.");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error setting default presence", ex);

                // Try to reinitialize Discord client
                TryReinitialize();
            }
        }

        // Build presence based on game state
        private RichPresence BuildPresence(GameState gameState, bool isEscMenu)
        {
            // If player is in a match
            if (_isInMatch)
            {
                // Use the last known map
                string mapToShow = _lastKnownMap;

                // In-game presence
                var presence = new RichPresence
                {
                    Details = BuildDetailsString(gameState, mapToShow),
                    State = BuildStateString(gameState),
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };

                // Add map image if available
                string mapImageKey = GetMapImageKey(mapToShow);
                if (!string.IsNullOrEmpty(mapImageKey) && mapImageKey != "cs2_logo")
                {
                    presence.Assets.SmallImageKey = mapImageKey;
                    presence.Assets.SmallImageText = mapToShow;
                }

                return presence;
            }
            else
            {
                // Menu presence
                return new RichPresence
                {
                    Details = "In Main Menu",
                    State = "Waiting for a match",
                    Assets = new Assets
                    {
                        LargeImageKey = "cs2_logo",
                        LargeImageText = "Counter-Strike 2"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _cs2StartTime
                    }
                };
            }
        }

        // Clear presence with thread safety
        public void ClearPresence()
        {
            if (_client == null || !_client.IsInitialized)
                return;

            try
            {
                lock (_presenceLock)
                {
                    if (!_isPresenceCleared)
                    {
                        // Clear presence - don't call Invoke() here
                        _client.ClearPresence();

                        _lastPresenceHash = null;
                        _isPresenceCleared = true;
                        ConsoleManager.LogImportant("Discord presence cleared.");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error clearing presence", ex);
            }
        }

        // Shutdown Discord RPC
        public void Shutdown()
        {
            try
            {
                lock (_presenceLock)
                {
                    if (_client != null)
                    {
                        ClearPresence();
                        _client.Dispose();
                        _client = null;
                        ConsoleManager.LogImportant("Discord RPC shut down.");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error shutting down Discord RPC", ex);
            }
        }

        // Build details string - optimized to reuse StringBuilder
        private string BuildDetailsString(GameState gameState, string? mapName = null)
        {
            if (Config.Current == null)
                return "Playing CS2";

            _stringBuilder.Clear();

            // Use provided map name
            string mapToShow = mapName ?? "Unknown";

            if (Config.Current.ShowMap && !string.IsNullOrEmpty(mapToShow) && mapToShow != "Unknown")
            {
                _stringBuilder.Append($"Map: {mapToShow}");
            }

            if (Config.Current.ShowGameMode && !string.IsNullOrEmpty(gameState.CurrentGameMode) && gameState.CurrentGameMode != "Unknown")
            {
                if (_stringBuilder.Length > 0)
                    _stringBuilder.Append(" | ");

                // Get formatted game mode name
                string formattedGameMode = GetFormattedGameMode(gameState.CurrentGameMode);
                _stringBuilder.Append($"Mode: {formattedGameMode}");
            }

            return _stringBuilder.Length > 0 ? _stringBuilder.ToString() : "Playing CS2";
        }

        // Get formatted game mode name
        private string GetFormattedGameMode(string gameMode)
        {
            if (string.IsNullOrEmpty(gameMode))
                return "Unknown";

            // Try to get mapped game mode
            if (_gameModeMap.TryGetValue(gameMode, out string? mappedMode))
                return mappedMode;

            // Fallback to original game mode with first letter capitalized
            return char.ToUpper(gameMode[0]) + gameMode.Substring(1);
        }

        // Build state string - optimized to reuse StringBuilder
        private string BuildStateString(GameState gameState)
        {
            if (Config.Current == null)
                return "In a match";

            _stringBuilder.Clear();

            // Add score if enabled
            if (Config.Current.ShowScore && _isInMatch)
            {
                _stringBuilder.Append($"Score: CT {gameState.CurrentCTScore} - T {gameState.CurrentTScore}");
            }

            // Show team information
            if (Config.Current.ShowTeam && _isInMatch)
            {
                if (_stringBuilder.Length > 0)
                    _stringBuilder.Append(" | ");

                // Check if player is a spectator or on a team
                if (gameState.CurrentTeam.Equals("Spectator", StringComparison.OrdinalIgnoreCase))
                {
                    // If player activity is "playing" but team is "Spectator", they might be dead
                    if (gameState.CurrentActivity.Equals("playing", StringComparison.OrdinalIgnoreCase) &&
                        gameState.HasPlayerState && gameState.PlayerAlive == false)
                    {
                        // Player is dead but still on a team - get team from player state
                        if (!string.IsNullOrEmpty(gameState.PlayerTeam) &&
                            !gameState.PlayerTeam.Equals("Spectator", StringComparison.OrdinalIgnoreCase))
                        {
                            _stringBuilder.Append($"Team: {gameState.PlayerTeam} (Dead)");
                        }
                        else
                        {
                            _stringBuilder.Append("Dead");
                        }
                    }
                    else
                    {
                        // Pure spectator (not playing)
                        _stringBuilder.Append("Spectating");
                    }
                }
                else
                {
                    // Player is on a team (CT or T)
                    _stringBuilder.Append($"Team: {gameState.CurrentTeam}");

                    // Add dead status if player is not alive
                    if (gameState.HasPlayerState && gameState.PlayerAlive == false)
                    {
                        _stringBuilder.Append(" (Dead)");
                    }
                }
            }

            if (_stringBuilder.Length == 0)
            {
                return _isInMatch ? "In a match" : "Waiting for a match";
            }

            return _stringBuilder.ToString();
        }

        // Get map image key
        private string GetMapImageKey(string mapName)
        {
            if (string.IsNullOrEmpty(mapName) || mapName == "Unknown")
                return "cs2_logo";

            // Extract the base map name (remove prefixes like de_, cs_, etc.)
            string baseName = mapName;
            if (mapName.Contains("_"))
            {
                string[] parts = mapName.Split('_');
                if (parts.Length > 1)
                {
                    // Keep the prefix for standard maps
                    if (parts[0] == "de" || parts[0] == "cs" || parts[0] == "ar" || parts[0] == "gg")
                    {
                        baseName = mapName;
                    }
                    else
                    {
                        // For workshop maps, just use the base name
                        baseName = parts[1];
                    }
                }
            }

            return baseName.ToLower();
        }

        // Calculate a hash for presence comparison
        private string CalculatePresenceHash(RichPresence presence)
        {
            // Exclude timestamp from hash to ensure it's updated regularly
            return $"{presence.Details}|{presence.State}|{presence.Assets?.LargeImageKey}|{presence.Assets?.SmallImageText}";
        }

        // Implement IDisposable
        public void Dispose()
        {
            Shutdown();
            GC.SuppressFinalize(this);
        }
    }

    // Custom logger to suppress Discord RPC errors
    public class DiscordLogger : DiscordRPC.Logging.ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Warning;

        public void Trace(string message, params object[] args)
        {
            // Ignore trace messages
        }

        public void Info(string message, params object[] args)
        {
            // Ignore info messages
        }

        public void Warning(string message, params object[] args)
        {
            // Only log warnings if they're not about Invoke
            if (!message.Contains("Invoke"))
            {
                ConsoleManager.LogDebug($"[Discord Warning] {string.Format(message, args)}");
            }
        }

        public void Error(string message, params object[] args)
        {
            // Only log errors if they're not about Invoke
            if (!message.Contains("Invoke"))
            {
                ConsoleManager.LogError($"[Discord Error] {string.Format(message, args)}");
            }
        }
    }
}

