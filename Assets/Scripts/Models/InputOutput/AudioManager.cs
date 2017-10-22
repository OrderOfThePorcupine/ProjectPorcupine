﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FMOD;

/// <summary>
/// The Manager that handles the loading and storing of audio from streamingAssets.
/// </summary>
public static class AudioManager
{
    public static readonly FMOD.System SoundSystem;
    private static Dictionary<string, SoundClip> audioClips;

    static AudioManager()
    {
        Factory.System_Create(out SoundSystem);
        SoundSystem.setDSPBufferSize(1024, 10);
        SoundSystem.init(32, INITFLAGS.NORMAL, (IntPtr)0);

        ChannelGroups = new Dictionary<string, ChannelGroup>();
        ChannelGroup master;
        SoundSystem.getMasterChannelGroup(out master);
        Master = master;

        DSPConnection connection;
        ChannelGroups.Add("UI", null);
        ChannelGroups.Add("gameSounds", null);
        ChannelGroups.Add("alerts", null);
        ChannelGroups.Add("music", null);
        foreach (string key in ChannelGroups.Keys.ToArray())
        {
            ChannelGroup chanGroup;
            SoundSystem.createChannelGroup(key, out chanGroup);
            Master.addGroup(chanGroup, true, out connection);
            ChannelGroups[key] = chanGroup;
        }

        ChannelGroups.Add("master", Master);

        SoundSystem.set3DSettings(1f, 1f, .25f);
        audioClips = new Dictionary<string, SoundClip>();
    }

    public static Dictionary<string, ChannelGroup> ChannelGroups { get; private set; }

    public static ChannelGroup Master { get; private set; }

    /// <summary>
    /// Loads all the audio files from the specified directory.
    /// </summary>
    /// <param name="directoryPath">The path of the directory you want to load the audio files from.</param>
    public static void LoadAudioFiles(string directoryPath)
    {
        string[] subDirectories = Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            LoadAudioFiles(subDirectory);
        }

        string[] filesInDir = Directory.GetFiles(directoryPath);
        LoadAudioFile(filesInDir, directoryPath);
    }

    /// <summary>
    /// Creates a human readable string of the audioClips Dictionary.
    /// Used for debugging.
    /// </summary>
    /// <returns>String containing the information of audioClips.</returns>
    public static string GetAudioDictionaryString()
    {
        Dictionary<string, SoundClip> dictionary = audioClips;

        return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value.ToString()).ToArray()) + "}";
    }

    /// <summary>
    /// Gets an AudioCLip from the specified category, with the specified name.
    /// Throws a LogWarning and returns an Error sound if the AudioClip does not exist.
    /// </summary>
    /// <param name="categoryName">
    /// The category that the AudioClip is in. Usually the same as the 
    /// directory that the audio file was load from.
    /// </param>
    /// <param name="audioName">The name of the SoundClip.</param>
    /// <returns>SoundClip from the specified category with the specified name.</returns>
    public static SoundClip GetAudio(string categoryName, string audioName)
    {
        SoundClip clip;

        string audioNameAndCategory = categoryName + "/" + audioName;

        if (audioClips.ContainsKey(audioNameAndCategory))
        {
            clip = audioClips[audioNameAndCategory];
        }
        else
        {
            try
            {
                UnityDebugger.Debugger.LogWarning("AudioManager", "No audio available called: " + audioNameAndCategory);
                clip = audioClips["Sound/Error"];
            }
            catch
            {
                throw new FileNotFoundException("Sound/Error.ogg not found");
            }
        }

        return clip;
    }

    /// <summary>
    /// Should only be called when the game is actually ending!.
    /// </summary>
    public static void Destroy()
    {
        SoundSystem.close();

        // This will also release master, so we don't have to call master.release();
        foreach (string key in ChannelGroups.Keys)
        {
            ChannelGroups[key].release();
        }

        SoundSystem.release();
        SoundSystem.close();
        audioClips = null;
    }

    private static void LoadAudioFile(string[] filesInDir, string directoryPath)
    {
        foreach (string file in filesInDir)
        {
            string audioCategory = new DirectoryInfo(directoryPath).Name;
            string filePath = new FileInfo(file).FullName;

            if (filePath.Contains(".xml") || filePath.Contains(".meta") || filePath.Contains(".db"))
            {
                continue;
            }

            Sound clip;
            SoundSystem.createSound(filePath, MODE._3D, out clip);
            string filename = Path.GetFileNameWithoutExtension(filePath);

            // If the filename contains a period, it is part of a sequence, remove everything after the period,
            // and it will be handled appropriately.
            if (filename.Contains("."))
            {
                filename = filename.Substring(0, filename.IndexOf("."));
            }

            filename = audioCategory + "/" + filename;

            if (audioClips.ContainsKey(filename))
            {
                audioClips[filename].Add(clip);
            }
            else
            {
                audioClips[filename] = new SoundClip(clip);
            }
        }
    }

    public struct DriverInfo
    {
        public DriverInfo(int id)
        {
            this.ID = id;
            StringBuilder name = new StringBuilder(64);
            Guid guid;
            int systemRate;
            SPEAKERMODE speakerMode;
            int speakerModeChannels;

            SoundSystem.getDriverInfo(id, name, 64, out guid, out systemRate, out speakerMode, out speakerModeChannels);

            this.Guid = guid;
            this.Name = name.ToString();
        }

        public int ID { get; private set; }

        public string Name { get; private set; }

        public Guid Guid { get; private set; }

        public override string ToString()
        {
            return ID.ToString() + ", " + Name;
        }
    }
}
