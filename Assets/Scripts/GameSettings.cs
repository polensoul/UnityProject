using UnityEngine;

/// <summary>
/// 游戏全局设置 — 离线德州扑克
/// 数据通过 PlayerPrefs 持久化
/// </summary>
public static class GameSettings
{
    private const string KEY_PLAYER_NAME  = "KnightPoker_PlayerName";
    private const string KEY_SOUND_VOLUME = "KnightPoker_SoundVolume";
    private const string KEY_MUSIC_VOLUME = "KnightPoker_MusicVolume";
    private const string KEY_START_CHIPS  = "KnightPoker_StartChips";
    private const string KEY_AI_COUNT     = "KnightPoker_AICount";
    private const string KEY_TOTAL_PLAYERS = "KnightPoker_TotalPlayers";
    private const string KEY_AI_DIFFICULTY = "KnightPoker_AIDifficulty";
    private const string KEY_SAVED_CHIPS  = "KnightPoker_SavedChips";
    private const string KEY_ROUNDS_PLAYED = "KnightPoker_Rounds";
    private const string KEY_RES_WIDTH = "KnightPoker_ResW";
    private const string KEY_RES_HEIGHT = "KnightPoker_ResH";
    private const string KEY_FULLSCREEN = "KnightPoker_Fullscreen";
    private const string KEY_QUALITY = "KnightPoker_Quality";
    private const string KEY_TOTAL_EXPERIENCE = "KnightPoker_TotalXP";

    public enum Venue
    {
        FriendsHouse = 0,    // peng you jia
        Bar = 1,             // jiu ba
        CityTournament = 2,  // shi ji bi sai
        ProvinceTournament = 3, // sheng ji bi sai
        KingTournament = 4   // de zhou wang jin biao sai
    }

    public struct VenueConfig
    {
        public string Name;
        public int MinLevel, MaxLevel;
        public int MinPlayers, MaxPlayers;
        public int MinHardAI;
        public int AiChipsMin, AiChipsMax;
        public int SmallBlind;
        public int MaxRaise;
        public int MaxAllIn;
    }

    public static VenueConfig GetVenueConfig(Venue v)
    {
        return v switch
        {
            Venue.FriendsHouse => new VenueConfig
            {
                Name = "朋友家", MinLevel = 1, MaxLevel = 20,
                MinPlayers = 3, MaxPlayers = 5, MinHardAI = 1,
                AiChipsMin = 1000, AiChipsMax = 1500,
                SmallBlind = 5, MaxRaise = 10, MaxAllIn = 200
            },
            Venue.Bar => new VenueConfig
            {
                Name = "酒吧", MinLevel = 20, MaxLevel = 40,
                MinPlayers = 4, MaxPlayers = 6, MinHardAI = 2,
                AiChipsMin = 5000, AiChipsMax = 10000,
                SmallBlind = 10, MaxRaise = 100, MaxAllIn = 1000
            },
            Venue.CityTournament => new VenueConfig
            {
                Name = "市级比赛", MinLevel = 40, MaxLevel = 60,
                MinPlayers = 8, MaxPlayers = 8, MinHardAI = 4,
                AiChipsMin = 10000, AiChipsMax = 20000,
                SmallBlind = 20, MaxRaise = 200, MaxAllIn = 2000
            },
            Venue.ProvinceTournament => new VenueConfig
            {
                Name = "省级比赛", MinLevel = 60, MaxLevel = 80,
                MinPlayers = 8, MaxPlayers = 8, MinHardAI = 5,
                AiChipsMin = 20000, AiChipsMax = 100000,
                SmallBlind = 100, MaxRaise = 1000, MaxAllIn = 10000
            },
            Venue.KingTournament => new VenueConfig
            {
                Name = "德州王锦标赛", MinLevel = 80, MaxLevel = 100,
                MinPlayers = 10, MaxPlayers = 10, MinHardAI = 7,
                AiChipsMin = 100000, AiChipsMax = 600000,
                SmallBlind = 500, MaxRaise = 5000, MaxAllIn = 50000
            },
            _ => throw new System.ArgumentOutOfRangeException(nameof(v))
        };
    }

    private const string KEY_SELECTED_VENUE = "KnightPoker_Venue";

    public static Venue SelectedVenue
    {
        get => (Venue)PlayerPrefs.GetInt(KEY_SELECTED_VENUE, 0);
        set => PlayerPrefs.SetInt(KEY_SELECTED_VENUE, (int)value);
    }

        public enum AIDifficulty { Easy = 0, Medium = 1, Hard = 2 }

    public static string PlayerName
    {
        get => PlayerPrefs.GetString(KEY_PLAYER_NAME, "Player");
        set => PlayerPrefs.SetString(KEY_PLAYER_NAME, value);
    }

    public static float SoundVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SOUND_VOLUME, 0.8f);
        set => PlayerPrefs.SetFloat(KEY_SOUND_VOLUME, Mathf.Clamp01(value));
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.6f);
        set => PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, Mathf.Clamp01(value));
    }

    public static int StartChips
    {
        get => PlayerPrefs.GetInt(KEY_START_CHIPS, 1000);
        set => PlayerPrefs.SetInt(KEY_START_CHIPS, Mathf.Max(100, value));
    }

    /// <summary>保存的筹码（上次游戏结束时）</summary>
    public static int SavedChips
    {
        get => PlayerPrefs.GetInt(KEY_SAVED_CHIPS, 0);
        set => PlayerPrefs.SetInt(KEY_SAVED_CHIPS, Mathf.Max(0, value));
    }

    public static int AICount
    {
        get => PlayerPrefs.GetInt(KEY_AI_COUNT, 3);
        set => PlayerPrefs.SetInt(KEY_AI_COUNT, Mathf.Clamp(value, 1, 7));
    }

    /// <summary>总玩家数（含人类），范围 2~8</summary>
    public static int TotalPlayers
    {
        get => PlayerPrefs.GetInt(KEY_TOTAL_PLAYERS, 4);
        set => PlayerPrefs.SetInt(KEY_TOTAL_PLAYERS, Mathf.Clamp(value, 2, 8));
    }

    /// <summary>AI 难度</summary>
    public static AIDifficulty Difficulty
    {
        get => (AIDifficulty)PlayerPrefs.GetInt(KEY_AI_DIFFICULTY, 1);
        set => PlayerPrefs.SetInt(KEY_AI_DIFFICULTY, (int)value);
    }

    public static int ResolutionWidth
    {
        get => PlayerPrefs.GetInt(KEY_RES_WIDTH, 1920);
        set => PlayerPrefs.SetInt(KEY_RES_WIDTH, value);
    }

    public static int ResolutionHeight
    {
        get => PlayerPrefs.GetInt(KEY_RES_HEIGHT, 1080);
        set => PlayerPrefs.SetInt(KEY_RES_HEIGHT, value);
    }

    public static bool IsFullscreen
    {
        get => PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        set => PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0);
    }

    public enum QualityLevel { Low = 0, Medium = 1, High = 2 }

    public static QualityLevel GraphicsQuality
    {
        get => (QualityLevel)PlayerPrefs.GetInt(KEY_QUALITY, 1);
        set => PlayerPrefs.SetInt(KEY_QUALITY, (int)value);
    }

    /// <summary>Total accumulated experience (never resets on new game)</summary>
    public static int TotalExperience
    {
        get => PlayerPrefs.GetInt(KEY_TOTAL_EXPERIENCE, 0);
        set => PlayerPrefs.SetInt(KEY_TOTAL_EXPERIENCE, Mathf.Max(0, value));
    }

    /// <summary>Player level derived from TotalExperience</summary>
    public static int Level
    {
        get
        {
            int xp = TotalExperience;
            if (xp < 1000) return 1;
            float n = (Mathf.Sqrt(1 + 8f * xp / 1000f) - 1f) / 2f;
            return Mathf.FloorToInt(n) + 1;
        }
    }

    /// <summary>Total XP required to reach next level</summary>
    public static int XPForNextLevel
    {
        get
        {
            int lv = Level;
            return lv * (lv + 1) / 2 * 1000;
        }
    }

    /// <summary>XP progress within current level</summary>
    public static int XPInCurrentLevel
    {
        get
        {
            int lv = Level;
            int baseXP = (lv - 1) * lv / 2 * 1000;
            return TotalExperience - baseXP;
        }
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
        PlayerPrefs.DeleteKey(KEY_SOUND_VOLUME);
        PlayerPrefs.DeleteKey(KEY_MUSIC_VOLUME);
        PlayerPrefs.DeleteKey(KEY_START_CHIPS);
        PlayerPrefs.DeleteKey(KEY_AI_COUNT);
        PlayerPrefs.DeleteKey(KEY_TOTAL_PLAYERS);
        PlayerPrefs.DeleteKey(KEY_AI_DIFFICULTY);
        PlayerPrefs.DeleteKey(KEY_SAVED_CHIPS);
        PlayerPrefs.DeleteKey(KEY_ROUNDS_PLAYED);
        PlayerPrefs.DeleteKey(KEY_RES_WIDTH);
        PlayerPrefs.DeleteKey(KEY_RES_HEIGHT);
        PlayerPrefs.DeleteKey(KEY_FULLSCREEN);
        PlayerPrefs.DeleteKey(KEY_QUALITY);
        PlayerPrefs.DeleteKey(KEY_TOTAL_EXPERIENCE);
        PlayerPrefs.DeleteKey(KEY_SELECTED_VENUE);
        PlayerPrefs.Save();
    }

    /// <summary>有效的筹码数（优先读取保存值）</summary>
    /// <summary>已参加的局数</summary>
    public static int RoundsPlayed
    {
        get => PlayerPrefs.GetInt(KEY_ROUNDS_PLAYED, 0);
        set => PlayerPrefs.SetInt(KEY_ROUNDS_PLAYED, Mathf.Max(0, value));
    }

    public static int EffectiveChips => SavedChips > 0 ? SavedChips : StartChips;

    /// <summary>重置保存的筹码</summary>
    public static void ResetSavedChips() { SavedChips = 0; Save(); }

    public static void Save() => PlayerPrefs.Save();
}
