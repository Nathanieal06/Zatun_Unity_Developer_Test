using UnityEngine;

public static class VolumeManager
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string SFXVolumeKey = "SFXVolume";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            ApplyVolume();
        }
    }

    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat(SFXVolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SFXVolumeKey, value);
            // In a more complex project, you would update an AudioMixer or notify listeners here.
        }
    }

    public static void Initialize()
    {
        ApplyVolume();
    }

    public static void ApplyVolume()
    {
        AudioListener.volume = MasterVolume;
    }
}
