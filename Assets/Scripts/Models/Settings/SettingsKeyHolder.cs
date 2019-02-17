#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using UnityEngine;

// This whole class is a nice one since it'll always ensure a value
// This means if we change defaults in our program we also need to change them here
// But it also means that generally it's 'better' code
// Movement
public static partial class SettingsKeyHolder
{
    public static float ZoomLerp
    {
        get
        {
            float temp;
            return Settings.GetSetting("general_movement_zoomLerp", out temp) ? temp : 10f;
        }

        set
        {
            Settings.SetSetting("general_movement_zoomLerp", value);
        }
    }

    public static float ZoomSensitivity
    {
        get
        {
            float temp;
            return Settings.GetSetting("general_movement_zoomSensitivity", out temp) ? temp : 3f;
        }

        set
        {
            Settings.SetSetting("general_movement_zoomSensitivity", value);
        }
    }
}

// General
public static partial class SettingsKeyHolder
{
    public static string SelectedLanguage
    {
        get
        {
            string temp;
            return Settings.GetSetting("general_localization_language", out temp) ? temp : "en_US";
        }

        set
        {
            Settings.SetSetting("general_localization_language", value);
        }
    }

    public static bool AutoUpdateLocalization
    {
        get
        {
            bool temp;
            return Settings.GetSetting("general_localization_autoUpdate", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("general_localization_autoUpdate", value);
        }
    }

    public static int AutosaveInterval
    {
        get
        {
            int temp;
            return Settings.GetSetting("general_autosave_interval", out temp) ? temp : 10;
        }

        set
        {
            Settings.SetSetting("general_autosave_interval", value);
        }
    }

    public static int AutosaveMaxFiles
    {
        get
        {
            int temp;
            return Settings.GetSetting("general_autosave_maxFiles", out temp) ? temp : 5;
        }

        set
        {
            Settings.SetSetting("general_autosave_maxFiles", value);
        }
    }
}

// Sound
public static partial class SettingsKeyHolder
{
    public static float MasterVolume
    {
        get
        {
            float temp;
            return Settings.GetSetting("sound_volume_master", out temp) ? temp : 1;
        }

        set
        {
            Settings.SetSetting("sound_volume_master", value);
        }
    }

    public static float MusicVolume
    {
        get
        {
            float temp;
            return Settings.GetSetting("sound_volume_music", out temp) ? temp : 1;
        }

        set
        {
            Settings.SetSetting("sound_volume_music", value);
        }
    }

    public static float GameVolume
    {
        get
        {
            float temp;
            return Settings.GetSetting("sound_volume_game", out temp) ? temp : 1;
        }

        set
        {
            Settings.SetSetting("sound_volume_game", value);
        }
    }

    public static float AlertsVolume
    {
        get
        {
            float temp;
            return Settings.GetSetting("sound_volume_alerts", out temp) ? temp : 1;
        }

        set
        {
            Settings.SetSetting("sound_volume_alerts", value);
        }
    }

    public static float UIVolume
    {
        get
        {
            float temp;
            return Settings.GetSetting("sound_volume_ui", out temp) ? temp : 1;
        }

        set
        {
            Settings.SetSetting("sound_volume_ui", value);
        }
    }

    public static int SelectedSoundDevice
    {
        get
        {
            int temp;
            return Settings.GetSetting("sound_advanced_device", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("sound_advanced_device", value);
        }
    }

    public static bool LocationalSound
    {
        get
        {
            bool temp;
            return Settings.GetSetting("sound_advanced_locational", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("sound_advanced_locational", value);
        }
    }
}

// Video
public static partial class SettingsKeyHolder
{
    public static int UISkin
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_general_uiSkin", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("video_general_uiSkin", value);
        }
    }

    public static int Quality
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_general_quality", out temp) ? temp : 2;
        }

        set
        {
            Settings.SetSetting("video_general_quality", value);
        }
    }

    public static int Vsync
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_general_vsync", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("video_general_vsync", value);
        }
    }

    public static float UIScale
    {
        get
        {
            float temp;
            return Settings.GetSetting("video_general_ui_scale", out temp) ? temp : 1f;
        }

        set
        {
            Settings.SetSetting("video_general_ui_scale", value);
        }
    }

    public static float UIAnimationSpeed
    {
        get
        {
            float temp;
            return Settings.GetSetting("video_general_ui_animation_speed", out temp) ? temp : 5f;
        }

        set
        {
            Settings.SetSetting("video_general_ui_animation_speed", value);
        }
    }

    public static string TooltipOptions
    {
        get
        {
            string temp;
            return Settings.GetSetting("video_general_tooltip_options", out temp) ? temp : string.Empty;
        }

        set
        {
            Settings.SetSetting("video_general_tooltip_options", value);
        }
    }

    public static bool SoftParticles
    {
        get
        {
            bool temp;
            return Settings.GetSetting("video_advanced_particles", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("video_advanced_particles", value);
        }
    }

    public static int Shadows
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_advanced_shadows", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("video_advanced_shadows", value);
        }
    }

    public static int AnisotropicFiltering
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_advanced_anisotropicFiltering", out temp) ? temp : 2;
        }

        set
        {
            Settings.SetSetting("video_advanced_anisotropicFiltering", value);
        }
    }

    public static int AA
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_advanced_aa", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("video_advanced_aa", value);
        }
    }

    public static bool Fullscreen
    {
        get
        {
            bool temp;
            return Settings.GetSetting("video_window_mode", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("video_window_mode", value);
        }
    }

    public static int Resolution
    {
        get
        {
            int temp;
            return Settings.GetSetting("video_window_resoultion", out temp) ? temp : 0;
        }

        set
        {
            Settings.SetSetting("video_window_resoultion", value);
        }
    }
}

public static partial class SettingsKeyHolder
{
    public static bool DeveloperMode
    {
        get
        {
            bool temp;
            return Settings.GetSetting("developer_general_developerMode", out temp) ? temp : false;
        }

        set
        {
            Settings.SetSetting("developer_general_developerMode", value);
        }
    }

    public static bool LoggingLevel
    {
        get
        {
            bool temp;
            return Settings.GetSetting("developer_general_loggingLevel", out temp) ? temp : false;
        }

        set
        {
            Settings.SetSetting("developer_general_loggingLevel", value);
        }
    }

    public static int ScrollSensitivity
    {
        get
        {
            int temp;
            return Settings.GetSetting("developer_console_scrollSensitivity", out temp) ? temp : 6;
        }

        set
        {
            Settings.SetSetting("developer_console_scrollSensitivity", value);
        }
    }

    public static bool EnableDevConsole
    {
        get
        {
            bool temp;
            return Settings.GetSetting("developer_console_enableDevConsole", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("developer_console_enableDevConsole", value);
        }
    }

    public static bool TimeStamps
    {
        get
        {
            bool temp;
            return Settings.GetSetting("developer_console_timeStamp", out temp) ? temp : true;
        }

        set
        {
            Settings.SetSetting("developer_console_timeStamp", value);
        }
    }

    public static int FontSize
    {
        get
        {
            int temp;
            return Settings.GetSetting("developer_console_fontSize", out temp) ? temp : 15;
        }

        set
        {
            Settings.SetSetting("developer_console_fontSize", value);
        }
    }

    public static string PerformanceHUD
    {
        get
        {
            string temp;
            return Settings.GetSetting("developer_utilities_performanceHUD", out temp) ? temp : "none";
        }

        set
        {
            Settings.SetSetting("developer_utilities_performanceHUD", value);
        }
    }
}