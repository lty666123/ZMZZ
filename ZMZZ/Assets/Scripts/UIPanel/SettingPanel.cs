using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Button btnMusic;
    public Button btnResolution;
    public Button btnKey;
    

    public GameObject musicPanel;
    public Slider musicVs;
    public Slider soundVs;

    public GameObject resolutionPanel;

    public Button btnBack;

    SettingData settingData;

    public override void Init()
    {

        settingData = GameDataMgr.Instance.settingData;
        
        //音乐相关
        musicVs.value = settingData.musicV;
        soundVs.value = settingData.soundV;
        btnMusic.onClick.AddListener(() =>
        {
            musicPanel.SetActive(true);
            resolutionPanel.SetActive(false);

        });
        musicVs.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.settingData.musicV = v;
            AudioManager.Instance.SetMusicVolume(v);
        });
        soundVs.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.settingData.soundV = v;
            AudioManager.Instance.SetSfxVolume(v);
        });

        btnResolution.onClick.AddListener(() =>
        {
           resolutionPanel.SetActive(true);
            musicPanel.SetActive(false);
        });

        btnBack.onClick.AddListener(() => 
        {
            GameDataMgr.Instance.SaveSettingData();
            
            UIManger.Instance.HidePanel<SettingPanel>();
        });
    }
}


