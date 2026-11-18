using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ChatClient : MonoBehaviour
{
    private ClientWebSocket socket;

    private CancellationTokenSource cancellation = new CancellationTokenSource();

    public event Action<ChatReceiveMessage> MessageReceived;

    //该字典用于存储和好友的聊天记录 好友id 好友消息
    public Dictionary<string,List<string>> friend_Messages_History = new Dictionary<string, List<string>>();

    public async void Connect(string accessToken)
    {
      if(string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("缺少登录令牌 请先登录");
            return;
        }
      if(socket != null)
        {
            Debug.Log("已经连接聊天服务器");
            return;
        }
        try
        {
            socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
            Debug.Log("正在连接服务器......");
            await socket.ConnectAsync(new System.Uri("ws://127.0.0.1:5080/ws/chat"), cancellation.Token);
            Debug.Log("连接成功");
            ReceiveMessage();
        }
        catch(OperationCanceledException)
        {

        }
        catch(Exception e)
        {
            Debug.Log("连接失败" + e.Message);
            socket?.Dispose();
            socket = null;
        }
    

       
    }

    #region 给服务器发消息 客户端收消息 处理消息的 一些方法
    //给服务器发送消息
    public async Task<bool> SendMessageToServer(string targetPlayerId, string content)
    {
        #region 一些不能正常发送的情况

        if (socket == null || socket.State != WebSocketState.Open)
        {
            Debug.Log("聊天连接尚未建立");
            return false;
        }
        if (string.IsNullOrEmpty(content))
        {
            Debug.Log("消息不能为空");
            return false;
        }
        if (string.IsNullOrWhiteSpace(targetPlayerId))
        {
            Debug.Log("请选择聊天对象");
            return false;
        }
        #endregion

        ChatSendRequest request = new ChatSendRequest { targetPlayerId = targetPlayerId, content = content };
        string json = JsonUtility.ToJson(request);//转为json
        byte[] data = Encoding.UTF8.GetBytes(json);//转为字节数组
        try
        {
            //发送消息 
            await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellation.Token);

            Debug.Log("消息已发送" + json);
            AddChatHistory(targetPlayerId, "我："+content);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("发送失败" + e.Message);
            return false;
        }
    }

    //不停等待服务器消息 并进行接收处理
    private async Task ReceiveMessage()
    {
        byte[] buffer = new byte[4096];
        try
        {
            //网络连接不关闭 就一直等待接收消息
            while (socket != null && socket.State == WebSocketState.Open)
            {
                using (MemoryStream messageStream = new MemoryStream())
                {

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellation.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Debug.Log("服务器关闭了聊天连接");
                            return;
                        }
                        messageStream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage); //该循环用于不停接收同一条消息 直到接收完毕

                    string json = Encoding.UTF8.GetString(messageStream.ToArray());

                    HandleIncomingMessage(json);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            Debug.LogError("接收消息失败" + e.Message);
        }
    }
    //对收到的消息进行解析 
    private void HandleIncomingMessage(string json)
    {
        ChatReceiveMessage message;
        try
        {
            //对接收到的消息进行反序列化
            message = JsonUtility.FromJson<ChatReceiveMessage>(json);
        }
        catch (ArgumentException e)
        {
            Debug.Log("聊天消息解析失败" + e.Message);
            return;
        }
        if (message == null || string.IsNullOrEmpty(message.fromNickname) || string.IsNullOrWhiteSpace(message.content))
        {
            Debug.LogWarning("聊天消息缺少必要字段" + json);
            return;
        }
        Debug.Log(message.fromNickname + ":" + message.content);
        AddChatHistory(message.fromPlayerId, message.fromNickname + ":" + message.content);
        //这里是 收到消息后显示在面板上的订阅
        MessageReceived?.Invoke(message);
    }
    #endregion

    /// <summary>
    /// 把消息存入字典当中
    /// </summary>
    /// <param name="playerId">好友id</param>
    /// <param name="line">一条消息</param>
    private void AddChatHistory(string playerId,string line)
    {
        if(!friend_Messages_History.TryGetValue(playerId,out List<string> lines))
        {
            lines = new List<string>();
            friend_Messages_History.Add(playerId, lines);
        }
        lines.Add(line);
    }
    public string GetChatHistory(string playerId)
    {
        if(string.IsNullOrEmpty(playerId))
        {
            return "";
        }
        if(friend_Messages_History.TryGetValue(playerId,out List<string> lines))
        {
            return string.Join("\n", lines);
        }
        return "";
    }

    [Serializable] 
    private class ChatSendRequest //信息发送类 
    {
        public string targetPlayerId;//目标id
        public string content;//具体内容
    }

    [Serializable]
    public class ChatReceiveMessage //客户端收到的服务器转发的其它客户端消息的数据结构
    {
        public string fromPlayerId;
        public string fromNickname;
        public string content;
        public string createdAtUtc;
    }


    private void OnDestroy()
    {
        cancellation.Cancel();
        if(socket!= null)
        {
            socket.Abort();
            socket.Dispose();
            socket = null;
        }

        cancellation.Dispose();
    }
}
