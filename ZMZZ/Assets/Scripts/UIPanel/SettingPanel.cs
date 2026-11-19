using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Button btnMusic;//音乐按钮
    public Button btnResolution;//分辨率按钮
    public Button btnKey;
    

    public GameObject musicPanel;
    public Slider musicVs;
    public Slider soundVs;

    public GameObject resolutionPanel;//分辨率面板
    public Button btn1280;
    public Button btn1600;
    public Button btn1920;

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

        //分辨率相关
        btnResolution.onClick.AddListener(() =>
        {
           resolutionPanel.SetActive(true);
            musicPanel.SetActive(false);
        });
        btn1280.onClick.AddListener(() => { ChangeResolution(1280, 720); });
        btn1600.onClick.AddListener(() => { ChangeResolution(1600, 960); });
        btn1920.onClick.AddListener(() => { ChangeResolution(1920, 1080); });


        btnBack.onClick.AddListener(() => 
        {
            GameDataMgr.Instance.SaveSettingData();
            
            UIManger.Instance.HidePanel<SettingPanel>();
        });
    }

    private void ChangeResolution(int width,int height)
    {
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
    }

}


