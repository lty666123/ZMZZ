using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class LoginTest : MonoBehaviour
{
    private string accessToken;
    private string targetPlayer;
    private FriendRequest[] incomingRequests = new FriendRequest[0];
    private string requestId;
    private bool isClicked;

    //负责好友聊天相关
    public ChatClient chatClient;
    //负责读取存档相关
    public CloudSaveClient cloudSaveClient;

    //好友申请表 变化时 要通知界面做的事
    public event Action FriendRequestsChanged;

    //想准备一个好友关系列表 还未获取内容 等 方法查找好友关系时 进行添加 之后可以供给friendPanel使用
    public FriendRelationList list = new FriendRelationList { items = new FriendRelation[0] };
    public event Action FriendsChanged;

    public event Action LoginSucceed;

    public static bool isLoginSucced;
    public event Action RegisterSucceed;

    public string playerId;

    public int requestNum => incomingRequests.Length;
    public string firstNickName => incomingRequests.Length > 0 ? incomingRequests[0].fromNickname : "";

    private void Start()
    {
        //yield return Login("rin", "Demo123!");
        //yield return GetMyInfo();
        //yield return SearchPlayer("ZMZZ-RIN-002");
       // yield return FriendsRequests(targetPlayer);
        //yield return CheckFriendsRequests();
        //yield return CheckFriends();
        DontDestroyOnLoad(gameObject);
    }
    //登录
    private IEnumerator Login(string userName, string password)
    {
        LoginRequest data = new LoginRequest{userName = userName,password = password };
        string json = JsonUtility.ToJson(data);

        using(UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5080/auth/login","Post"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("登录失败" + request.error + request.downloadHandler.text);
                yield break;
            }
            LoginResponse res = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text); 
            if(res == null|| string.IsNullOrEmpty(res.accessToken))
            {
                Debug.LogError("登录令牌无效");
                yield break;
            }
            accessToken = res.accessToken;
            if(chatClient!=null)
            {
                chatClient.Connect(accessToken);
            }
            else
            {
                Debug.LogError("LoginTest没有绑定ChatClient组件");
            }
            playerId = res.playerId;//获取当前登录的玩家id


            if(cloudSaveClient == null)
            {
                Debug.LogError("没有绑定 CloudSaveClient 无法读取存档");
                yield break;
            }
            //进行读档操作
            yield return cloudSaveClient.DownLoadSave(accessToken);
            if(!cloudSaveClient.DownLoadSucceed)
            {
                Debug.LogError("账号验证成功 但存档下载失败 请重试");
                yield break;
            }
            //进行解析并Gamedatamgr使用存档 成功返回true
            bool applied = cloudSaveClient.ApplyDownLoadedave();
            if(!applied)
            {
                Debug.LogError("存档解析或使用失败 停止进入游戏");
                yield break;
            }
            cloudSaveClient.EnableCloudSave(accessToken);
            Debug.Log("登录及读档成功"+ res.nickName);
            isLoginSucced = true;
            LoginSucceed?.Invoke();
            yield return GetMyInfo();
            yield return CheckFriendsRequests();
            yield return CheckFriends();
        }
    }

    //注册
    private IEnumerator Register(string nickname, string userName,string password)
    {
        RegisterRequest data = new RegisterRequest { nickname = nickname, userName = userName, password = password };
        string json = JsonUtility.ToJson(data);
        using(UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5080/auth/register","Post"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5;
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("注册失败"+request.downloadHandler.text);
                yield break;
            }
            RegisterSucceed?.Invoke();
            Debug.Log("注册成功");
        }
    }

    //获取自己的信息
    private IEnumerator GetMyInfo()
    {
        
        using (UnityWebRequest request =  UnityWebRequest.Get("http://127.0.0.1:5080/auth/me"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success)
            {
                Debug.LogError("失败"+request.error+request.downloadHandler.text);
                yield break;
            }
            Debug.Log("用户为" + request.downloadHandler.text);
        }
    }
    //根据好友码查找好友
    private IEnumerator SearchPlayer(string friendCode)
    {
        string url = "http://127.0.0.1:5080/players/search?friendCode=" + UnityWebRequest.EscapeURL(friendCode);
        using(UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success)
            {
                Debug.LogError("失败"+request.error + request.downloadHandler.text);
                yield break;
            }
            SearchFriendInfo sfi = JsonUtility.FromJson<SearchFriendInfo>(request.downloadHandler.text);
            if(sfi==null||string.IsNullOrEmpty(sfi.playerID))
            {
                yield break;
            }
            targetPlayer = sfi.playerID;
            Debug.Log("找到玩家 id为" + sfi.playerID
                + "好友码为" + sfi.friendCode
                + "名字为" + sfi.nickname
                + "等级为" + sfi.level);
        }
    }

    //发送好友申请
    private IEnumerator FriendsRequests(string playerId)
    {
        FriendRequestCreate frc = new FriendRequestCreate { targetPlayerId = playerId};
        string json = JsonUtility.ToJson(frc);
        using (UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:5080/friends/requests", "Post"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("申请失败" + request.error + request.downloadHandler.text);
                yield break;
            }
            FriendRequest fr = JsonUtility.FromJson<FriendRequest>(request.downloadHandler.text);
            if(fr == null)
            {
                yield break;
            }
            Debug.Log("申请成功"
                + "\n申请记录" + fr.requestId
                + "\n申请人id" + fr.fromPlayerId
                
                + "\n目标玩家id" + fr.toPlayerId
                + "\n状态" + fr.status
                + "\n申请时间" + fr.createdAtUtc);
        }
    }
    
    //查看好友申请
    private IEnumerator CheckFriendsRequests()
    {
        incomingRequests = new FriendRequest[0];
        FriendRequestsChanged?.Invoke();
        using(UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:5080/friends/requests"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();

            if(request.result!=UnityWebRequest.Result.Success)
            {
                Debug.LogError("申请获取失败"+request.error+request.downloadHandler.text);
                yield break;
            }

            string json = request.downloadHandler.text;
            if(string.IsNullOrEmpty(json)||json == "[]")
            {
                Debug.Log("没有待处理的好友申请");
                yield break;
            }
            //这里要注意JsonUtlity不能解析顶层数组 要给数组加上对象外壳再解析
            FriendRequestList list = JsonUtility.FromJson<FriendRequestList>("{\"items\":"+json+"}");
            if(list == null||list.items == null)
            {
                Debug.LogError("好友申请数据解析失败" + json);
                yield break;
            }
            foreach(FriendRequest item in list.items)
            {
                Debug.Log("收到好友申请"
                    + "\n申请记录" + item.requestId
                    + "\n申请者id" + item.fromPlayerId
                    + "\n申请者名字" + item.fromNickname
                    + "\n申请状态" + item.status
                    + "\n创建时间" + item.createdAtUtc);
            }
            if(list.items.Length > 0)
            {
                incomingRequests = list.items;
            }
            FriendRequestsChanged?.Invoke();
            Debug.Log("待处理的申请数量" + incomingRequests.Length);
        }
    }
    
   

    //同意好友申请
    private IEnumerator AcceptFriendsRequests(string requestId)
    {
        string url = "http://127.0.0.1:5080/friends/requests/" + requestId + "/accept";
        using (UnityWebRequest request = new UnityWebRequest(url, "Post"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if(request.result!=UnityWebRequest.Result.Success)
            {
                Debug.LogError("处理失败" + request.error + request.downloadHandler.text);
                yield break;
            }
            FriendRelation fr = JsonUtility.FromJson<FriendRelation>(request.downloadHandler.text);
            Debug.Log("已同意申请"
                    + "\n好友关系" + fr.relationId
                    + "\n玩家A" + fr.playerAId
                    + "\n玩家B" + fr.playerBId
                    + "\n申请状态" + fr.status
                    + "\n创建时间" + fr.createdAtUtc);
        }
    }
  
    //拒绝好友申请
    private IEnumerator RejectFriendsRequests(string requestId)
    {
        string url = "http://127.0.0.1:5080/friends/requests/" + requestId + "/reject";
        using(UnityWebRequest request = new UnityWebRequest(url,"Post"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("处理失败"+request.error+ request.downloadHandler.text);
                yield break;
            }
            FriendRequest fr = JsonUtility.FromJson<FriendRequest>(request.downloadHandler.text);
            Debug.Log("已拒绝申请"
                    +"\n申请记录"+fr.requestId
                    + "\n申请状态" + fr.status
                    + "\n创建时间" + fr.createdAtUtc);
        }
    }

    //查看好友列表
    private IEnumerator CheckFriends()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("http://127.0.0.1:5080/friends"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.timeout = 5;
            yield return request.SendWebRequest();
            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("查看失败" + request.error + request.downloadHandler.text);
                yield break;
            }

            string json = request.downloadHandler.text;
            if(json== null||string.IsNullOrEmpty(json))
            {
                Debug.Log("没有游戏好友");
                yield break;
            }
            
            FriendRelationList result = JsonUtility.FromJson<FriendRelationList>("{\"items\":" + json + "}");
            if(result == null||result.items == null)
            {
                Debug.LogError("好友列表解析失败");
                yield break;
            }
            list = result;
            FriendsChanged?.Invoke();
            foreach (FriendRelation item in list.items)
            {
                Debug.Log("获取好友列表成功"+
                    "好友关系"+item.relationId
                    +"玩家A"+item.playerAId
                    +"玩家B"+item.playerBId
                    +"建立时间"+item.createdAtUtc
                    );
            }
        }
    }

    public void OnAcceptClicked()
    {
        StartCoroutine(HandleFriendsRequests(true));
    }
    public void OnRejectClicked()
    {
        StartCoroutine (HandleFriendsRequests(false));
    }

    //通过按钮处理申请
    private IEnumerator HandleFriendsRequests(bool accept)
    {
        if (isClicked)
            yield break;
        if(accessToken == null)
        {
            Debug.Log("请先登录");
            yield break;
        }
        if(incomingRequests.Length==0)
        {
            Debug.Log("没有要处理的好友申请");
            yield break;
        }
        string selectRequestId = incomingRequests[0].requestId;
        isClicked = true;
        if (accept)
            yield return AcceptFriendsRequests(selectRequestId);
        else
            yield return RejectFriendsRequests(selectRequestId);

        yield return CheckFriendsRequests();
        yield return CheckFriends();
        isClicked = false;
    }


    //更新好友申请列表
    private IEnumerator RefreshCheckFriendsRequests()
    {
        if (isClicked)
        {
            yield break;
        }
        if (accessToken == null)
        {
            Debug.Log("请先登录");
            yield break;
        }

        isClicked = true;
        yield return CheckFriendsRequests();
        isClicked = false;
    }
    public void OnRefreshClicked()
    {
        StartCoroutine(RefreshCheckFriendsRequests());
    }


    public string GetFriendPlayerId(FriendRelation relation)
    {
        if(string.IsNullOrEmpty(relation.relationId)||string.IsNullOrEmpty(playerId))
        {
            return null;
        }
       if(relation.playerAId == playerId)
             return  relation.playerBId; 
       else if(relation.playerBId == playerId)
             return relation.playerAId;
        return null;
    }

    [Serializable]
    private class LoginRequest //登录 相关数据结构 向服务器提供账号名 及 密码
    {
        public string userName;
        public string password;
    }

    [Serializable]
    private class LoginResponse //登录成功后 服务器返回的 相关数据结构 有令牌 id 名字
    {
        public string accessToken;
        public string playerId;
        public string nickName;
    }

    [Serializable]
    private class SearchFriendInfo //查找好友 相关的数据结构 id 好友码 名字
    {
        public string playerID;
        public string friendCode;
        public string nickname;
        public int level;
    }
    [Serializable]
    private class FriendRequestCreate
    {
        public string targetPlayerId;
    }
    [Serializable]
    public class FriendRequest
    {
       public string requestId; // 申请的id 令牌
        public string fromPlayerId;// 申请者id

        public string fromNickname;
        public string toPlayerId; //被申请者id
        public string status; //申请状态
        public string createdAtUtc;
    }

    [Serializable]
    public class FriendRequestList
    {
        public FriendRequest[] items;
    }

    [Serializable]
    public class FriendRelation
    {
        public string relationId;
        public string playerAId;
        public string playerBId;
        public string status;
        public string createdAtUtc;

    }
    [Serializable]
    public class FriendRelationList//这里只是为了使jsonutility解析的 作为解析外壳
    {
       public FriendRelation[] items;
    }
    [Serializable]
    public class RegisterRequest
    {
        public string userName;
        public string password;
        public string nickname;
    }
   
    public void OnLoginClicked(string userName,string password)
    {
        StartCoroutine(Login(userName,password));
     
    }

   public void OnRegisterClicked(string nickname,string userName,string password)
    {
        StartCoroutine(Register(nickname, userName, password));
    }
}
