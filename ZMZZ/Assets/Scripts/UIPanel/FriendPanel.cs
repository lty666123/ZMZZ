using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FriendPanel : BasePanel
{
    #region 一些按钮
    public Button btnBack;
    public Button btnAccept;
    public Button btnReject;
    public Button btnRefresh;
    #endregion

    //好友申请信息
    public Text txtRequest;
    //聊天信息
    public Text txtChatMessage;
    private ChatClient chatClient;
    LoginTest loginTest;
    //聊天输入框
    public InputField inputChat;
    //聊天信息发送按钮
    public Button btnSend;
    //聊天区域的滚动组件
    public ScrollRect chatScroll;

    //好友关系展示
    public Text txtFriendRelation;

    //这里先做测试
    public string chatTargetPlayerId = "";

    private bool isSending;

    ////目前做测试
    //public Button btnFriend;
    //public Button btnFriend2;

    public Button btnFriendPrefab;
    private Dictionary<string, Button> btnFriendsDic = new Dictionary<string, Button>();
    public Transform btnFriends_Pos;

    public override void Init()
    {
        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<FriendPanel>();
        });
        loginTest = FindFirstObjectByType<LoginTest>();
        if(loginTest == null)
        {
            Debug.LogError("没有好友申请");
            btnAccept.interactable = false;
            btnReject.interactable = false;
            return;
        }

        #region 按钮相关
        btnAccept.onClick.AddListener(loginTest.OnAcceptClicked);
        btnReject.onClick.AddListener(loginTest.OnRejectClicked);
        btnRefresh.onClick.AddListener(loginTest.OnRefreshClicked);
        #endregion

        loginTest.FriendRequestsChanged += ViewRefresh;
        loginTest.FriendsChanged += ViewRefresh;
        ViewRefresh();

        chatClient = FindFirstObjectByType<ChatClient>();
        if(chatClient != null)
        {
            //为事件添加订阅 调用的时机是 ChatClient中接收到数据之后对数据进行解析完毕之后调用的
            chatClient.MessageReceived += OnChatMessageReceived;
            btnSend.onClick.AddListener(OnSendClicked);

            RefreshHistoryChatMessages();
        }
        else
        {
            btnSend.interactable = false;
        }


        //btnFriend.onClick.AddListener(() =>
        //{
        //    SelectChatPlayer(btnFriend.GetComponentInChildren<Text>().text);
        //});
        //btnFriend2.onClick.AddListener(() =>
        //{
        //    SelectChatPlayer(btnFriend2.GetComponentInChildren<Text>().text);
        //});
    }

    //更新好友申请界面 以及好友界面
    public void ViewRefresh()
    {
        bool hasRequest = loginTest.requestNum > 0;
        btnAccept.interactable = hasRequest;
        btnReject.interactable = hasRequest;
        txtRequest.text = hasRequest ? "好友申请数" + loginTest.requestNum + "第一个的申请名字" + loginTest.firstNickName : "无待处理申请";

        txtFriendRelation.text = "";
        

        //foreach(var item in loginTest.list.items)
        //{
        //    //显示好友id
        //    txtFriendRelation.text += loginTest.GetFriendPlayerId(item) + "\n";
        //}
        if(loginTest.list == null||loginTest.list.items == null||loginTest.list.items.Length == 0)
        {
            return;
        }
        foreach(var item in loginTest.list.items)
        {
            string playerId = loginTest.GetFriendPlayerId(item);
            if(string.IsNullOrEmpty(playerId))
            {
                continue;
            }
            if(btnFriendsDic.TryGetValue(playerId,out _))
            {
                continue;
            }
            Button button = Instantiate(btnFriendPrefab, btnFriends_Pos, false);
            Text txt = button.GetComponentInChildren<Text>();
            txt.text = playerId;
            button.onClick.AddListener(() => {
            
                    SelectChatPlayer(playerId);
            }
            );
            btnFriendsDic.Add(playerId, button);
        }
        //Text txt = btnFriend.GetComponentInChildren<Text>();
        //Text txt2 = btnFriend2.GetComponentInChildren<Text>();
        //txt.text = loginTest.GetFriendPlayerId(loginTest.list.items[0]);
        //txt2.text = loginTest.GetFriendPlayerId(loginTest.list.items[1]);
    }

    //取得聊天信息 并展示
    private void OnChatMessageReceived(ChatClient.ChatReceiveMessage message)
    {
        if(message.fromPlayerId != chatTargetPlayerId)
        {
            return;
        }
        RefreshHistoryChatMessages();
    }
    //发送消息
    public async void OnSendClicked()
    {
        if(isSending||chatClient == null)
        {
           // Debug.Log("聊天组件未找到");
            return;
        }
        string content = inputChat.text.Trim();
        if(string.IsNullOrEmpty(content))
        {
            Debug.Log("不能发送空消息");
            return;
        }
        if(string.IsNullOrWhiteSpace(chatTargetPlayerId))
        {
            Debug.Log("请选择聊天对象");
        }
        isSending = true;
        btnSend.interactable = false;
        try
        {
            //给服务器发送消息 成功代表的是 发送成功 而不是服务器一定接收到了
            bool success = await chatClient.SendMessageToServer(chatTargetPlayerId, content);
            if (this == null)
                return;
            if (success)
            {
                //在聊天框中显示我说了什么 因为每次向服务器发送消息的时候 已经把我说的话 加入到history字典中了
                //所以直接刷新聊天记录就可以了
                RefreshHistoryChatMessages();
                //对输入框进行 清空
                if(inputChat.text == content)
                {
                    inputChat.text = "";
                }
            }
        }
        finally
        {
            isSending = false;
            if(this!= null)
            {
                btnSend.interactable = true;
            }
        }
        
    }

    //更新历史消息的方法 使得 每次关闭好友面板时 聊天记录不删除
    private void RefreshHistoryChatMessages()
    {
        txtChatMessage.text = chatClient.GetChatHistory(chatTargetPlayerId);
        StartCoroutine(ScrollChatToBottom());
    }

    //选择要聊天的对象 
    private void SelectChatPlayer(string targetPlayerId)
    {
        if(string.IsNullOrEmpty(targetPlayerId)||chatClient == null)
        {
            return;
        }

        if(btnFriendsDic.TryGetValue(chatTargetPlayerId,out var button))
        {
            button.interactable = true;
        }

        chatTargetPlayerId = targetPlayerId;
        txtChatMessage.text = "";

        if (btnFriendsDic.TryGetValue(chatTargetPlayerId, out var button1))
        {
            button1.interactable = false;
        }

        RefreshHistoryChatMessages();
        Debug.Log("当前的聊天对象为" + chatTargetPlayerId);

    }


    private IEnumerator ScrollChatToBottom()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        chatScroll.StopMovement();

        chatScroll.verticalNormalizedPosition = 0f;
    }


    private void OnDestroy()
    {
        if(loginTest!=null)
        {
            loginTest.FriendRequestsChanged-= ViewRefresh;
            loginTest.FriendsChanged-= ViewRefresh;
        }
        if(chatClient!=null)
        {
            chatClient.MessageReceived -= OnChatMessageReceived;
        }
    }
}
