using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip clickClip;
    private AudioClip attackClip;
    private AudioClip hitClip;
    private AudioClip levUpClip;
    private AudioClip pickupClip;

    private void Awake()
    {
        if (instance != null && instance != this)
        { 
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        musicSource = this.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = this.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        ApplyVolumes();

    }


    public void ApplyVolumes()
    {
        SettingData sd = GameDataMgr.Instance.settingData;
        musicSource.volume = Mathf.Clamp01(sd.musicV);
        sfxSource.volume = Mathf.Clamp01(sd.soundV);
    }
   
    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        GameDataMgr.Instance.settingData.musicV = value;
        musicSource.volume = value;
    }
    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        GameDataMgr.Instance.settingData.soundV = value;
        sfxSource.volume = value;
    }
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.Play();
    }
    public void PlayBGM(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if(clip == null) return;
        musicSource.clip = clip;
        musicSource.Play();
    }
    public void PlaySfx(AudioClip clip)
    {
        if(clip!=null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    public void PlaySfx(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
    public void PlayClick() => PlaySfx(clickClip);
    public void PlayAttack() => PlaySfx(attackClip);
    public void PlayHit() => PlaySfx(hitClip);
    public void PlayLevup() => PlaySfx(levUpClip);
    public void PlayPickup() => PlaySfx(pickupClip);


}
