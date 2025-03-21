using System.Text.Json;
using System.Text.Json.Serialization;

namespace RichPresenceApp.Classes
{
    public class GameState
    {
        // Map name - extracted from nested structure
        private string _currentMap = "Unknown";

        [JsonIgnore]
        public string CurrentMap
        {
            get => _currentMap;
            set => _currentMap = string.IsNullOrEmpty(value) ? "Unknown" : value;
        }

        // Game mode - extracted from nested structure
        private string _currentGameMode = "Unknown";

        [JsonIgnore]
        public string CurrentGameMode
        {
            get => _currentGameMode;
            set => _currentGameMode = string.IsNullOrEmpty(value) ? "Unknown" : value;
        }

        // Activity - from player object
        private string _currentActivity = "menu";

        [JsonIgnore]
        public string CurrentActivity
        {
            get => _currentActivity;
            set => _currentActivity = string.IsNullOrEmpty(value) ? "menu" : value;
        }

        // Team - from player object
        private string _currentTeam = "Spectator";

        [JsonIgnore]
        public string CurrentTeam
        {
            get => _currentTeam;
            set => _currentTeam = string.IsNullOrEmpty(value) ? "Spectator" : value;
        }

        // CT score - from map.team_ct.score
        [JsonIgnore]
        public int CurrentCTScore { get; set; } = 0;

        // T score - from map.team_t.score
        [JsonIgnore]
        public int CurrentTScore { get; set; } = 0;

        // Map object from CS2 GSI
        [JsonPropertyName("map")]
        public MapInfo? Map { get; set; }

        // Player object from CS2 GSI
        [JsonPropertyName("player")]
        public PlayerInfo? Player { get; set; }

        // Process the raw JSON data after deserialization
        public void ProcessRawData()
        {
            // Extract map name
            if (Map != null && !string.IsNullOrEmpty(Map.Name))
            {
                CurrentMap = Map.Name;
            }

            // Extract game mode
            if (Map != null && !string.IsNullOrEmpty(Map.Mode))
            {
                CurrentGameMode = Map.Mode;
            }

            // Extract team scores
            if (Map != null)
            {
                if (Map.TeamCT != null)
                {
                    CurrentCTScore = Map.TeamCT.Score;
                }

                if (Map.TeamT != null)
                {
                    CurrentTScore = Map.TeamT.Score;
                }
            }

            // Extract player activity
            if (Player != null && !string.IsNullOrEmpty(Player.Activity))
            {
                CurrentActivity = Player.Activity;
            }

            // Extract player team
            if (Player != null && !string.IsNullOrEmpty(Player.Team))
            {
                CurrentTeam = Player.Team;
            }
        }
    }

    // Map information from CS2 GSI
    public class MapInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unknown";

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "Unknown";

        [JsonPropertyName("phase")]
        public string Phase { get; set; } = "Unknown";

        [JsonPropertyName("round")]
        public int Round { get; set; } = 0;

        [JsonPropertyName("team_ct")]
        public TeamInfo? TeamCT { get; set; }

        [JsonPropertyName("team_t")]
        public TeamInfo? TeamT { get; set; }
    }

    // Team information from CS2 GSI
    public class TeamInfo
    {
        [JsonPropertyName("score")]
        public int Score { get; set; } = 0;

        [JsonPropertyName("consecutive_round_losses")]
        public int ConsecutiveRoundLosses { get; set; } = 0;

        [JsonPropertyName("timeouts_remaining")]
        public int TimeoutsRemaining { get; set; } = 0;

        [JsonPropertyName("matches_won_this_series")]
        public int MatchesWonThisSeries { get; set; } = 0;
    }

    // Player information from CS2 GSI
    public class PlayerInfo
    {
        [JsonPropertyName("steamid")]
        public string SteamID { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("activity")]
        public string Activity { get; set; } = "menu";

        [JsonPropertyName("team")]
        public string Team { get; set; } = "Spectator";
    }
}