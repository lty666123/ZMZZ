using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    public Button btnStart;
    public Button btnQuit;
    public Button btnSetting;
    public Button btnAbout;

    public override void Init()
    {
        btnStart.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GameScene1");
            UIManger.Instance.HidePanel<BeginPanel>();

            //加载游戏场景
            
        });
        btnQuit.onClick.AddListener(() =>
        {
            //退出游戏
            Application.Quit();
        });
        btnSetting.onClick.AddListener(() =>
        {
            //切换至游戏设置界面 
            
            UIManger.Instance.ShowPanel<SettingPanel>();
        });
        btnAbout.onClick.AddListener(() =>
        {
            //切换至关于此游戏界面
            UIManger.Instance.HidePanel<BeginPanel>();
            UIManger.Instance.ShowPanel<AboutGamePanel>();
            
        });
    }

  
}
