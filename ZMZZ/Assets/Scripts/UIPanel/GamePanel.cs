using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : MonoBehaviour
{
    public Button btnSetting;
    public Button btnBag;
    public Button btnSkill;
    public Button btnMap;
    public Button btnPlayer;
    public Button btnFriend;
    public Text txtHp;
    public Image hpred;
    public float width = 123.44f;
    // Start is called before the first frame update


    public float lastHp;
    public float lastTotalHp;
    public bool hasHpChangedSave;

    private void Awake()
    {
    }
    private void OnEnable()
    {
        hasHpChangedSave = false;
    }
    void Start()
    {
        btnSetting.onClick.AddListener(() =>
        {
            UIManger.Instance.ShowPanel<SettingPanel>();
        });
        btnBag.onClick.AddListener(() =>
        {
            UIManger.Instance.ShowPanel<BagPanel>();
        });
        btnSkill.onClick.AddListener(() =>
        {

        });
        btnMap.onClick.AddListener(() =>
        {
            UIManger.Instance.ShowPanel<MapPanel>();
        });
        btnPlayer.onClick.AddListener(() =>
        {
            UIManger.Instance.ShowPanel<PlayerPanel>();
        });
        btnFriend.onClick.AddListener(() =>
        {
            UIManger.Instance.ShowPanel<FriendPanel>();
        });

        

    }

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = UIManger.Instance.HasPanel ? 0f : 1f;
        if (Time.timeScale == 0f)
        {
            return;
        }

        PlayerData pd = GameDataMgr.Instance.playerData;


        if(lastHp == pd.nowHp&&lastTotalHp == pd.totalHp&&hasHpChangedSave)
        {
            return;
        }
        txtHp.text = pd.nowHp + "/" + pd.totalHp;
        (hpred.transform as RectTransform).sizeDelta = new Vector2((float)pd.nowHp / pd.totalHp * width, 16);

        lastHp = pd.nowHp;
        lastTotalHp = pd.totalHp;
        hasHpChangedSave = true;
    }
}
