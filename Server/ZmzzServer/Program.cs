using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//拼接处 文件夹所在路径
string dataDirectory = Path.Combine(app.Environment.ContentRootPath, "Data");
//创建文件夹
Directory.CreateDirectory(dataDirectory);
//拼接 文件路径
string databasePath = Path.Combine(dataDirectory, "accounts.db");
//实例化出一个数据库文件 传入的参数通过构造函数 就行创建账号表 
//负责将账号写入数据库 从数据库里面读取账号
var accountStore = new AccountStore(databasePath);
//生成密码哈希 之后登录时验证密码
var passwordHasher = new PasswordHasher<UserAccount>();

var gameSaveStore = new GameSaveStore(databasePath);
var friendStore = new FriendStore(databasePath);

app.UseWebSockets();//负责启用WebSocket支持

var sessions = new ConcurrentDictionary<string,SessionInfo>();
//好友申请表 存储 令牌 - 请求信息
var friendRequests = new ConcurrentDictionary<string, FriendRequest>();
//好友关系表
var friendRelations = new ConcurrentDictionary<string, FriendRelation>();

//聊天连接表
var chatConnections = new ConcurrentDictionary<string, WebSocket>();
//json解析的配置
var chatJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
//发送锁 多人同时向同一个人发送消息 要排队 开始时允许的数量 最大数量
var chatSendGate = new SemaphoreSlim(1, 1);


//从请求中获取信息
SessionInfo? GetSession(HttpRequest request)
{
	string authorization = request.Headers["Authorization"].ToString();
	if(!authorization.StartsWith("Bearer "))
	{
		return null;
	}
	string token = authorization.Substring("Bearer ".Length).Trim();
	if(!sessions.TryGetValue(token, out var session)) return null;
	return session;
}

//获得好友键 绑定唯一的好友关系
string GetFriendPairKey(string firstPlayerId, string secondPlayerId)
{
	//规则是小在前
	if(string.CompareOrdinal(firstPlayerId,secondPlayerId)<=0)
		return $"{firstPlayerId}|{secondPlayerId}";
	return $"{secondPlayerId}|{firstPlayerId}";
}

app.MapGet("/health", () => new {
	status = "ok",
	serverTimeUtc = DateTime.UtcNow
});

app.MapPost("/auth/register", (RegisterRequest request) =>
{
	string userName = (request.UserName ?? "").Trim().ToLowerInvariant();
	string password = request.Password ?? "";
	string nickname = (request.Nickname ?? "").Trim();

	if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(password))
	{
		return Results.Json(
			new { error = "账号 密码和昵称不能为空" },
			statusCode: StatusCodes.Status400BadRequest);
	}

	string playerId = "player-" + Guid.NewGuid().ToString("N");
	UserAccount account = new UserAccount(userName, "", nickname, playerId);
	//根据用户提交的密码 生成密码哈希
	string passwordHash = passwordHasher.HashPassword(account, password);
	//将密码哈希加入账号对象中
	account = account with { PasswordHash = passwordHash };

	try
	{
		accountStore.CreateAccount(account);
	}
	catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
	{
		return Results.Json(
			new { error = "账号名已存在" },
		statusCode: StatusCodes.Status409Conflict);
	}

	return Results.Json(new
	{
		message = "注册成功",
		playerId = account.PlayerId,
		nickname = account.Nickname
	},
	statusCode: StatusCodes.Status201Created
	);
});


//玩家登录后 存进会话表中
app.MapPost("/auth/login",(LoginRequest request)=>
{
	string userName = (request.UserName ?? "").Trim().ToLowerInvariant();
	string password = request.Password ?? "";
	//if(request.Password!="Demo123!"||(userName != "saber"&&userName!="rin"&&userName!="rider"))
	//{
 //       return Results.Json(
 //   		new {error = "用户名或密码错误"},
 //   		statusCode: StatusCodes.Status401Unauthorized);
 //   }
	//string playerId = "player-"+userName;
	//string nickName = userName;
	if(string.IsNullOrEmpty(userName)||string.IsNullOrEmpty(password))
	{
		return Results.Json(new { error = "账号密码不能为空" },
			statusCode: StatusCodes.Status400BadRequest);
	}

	UserAccount? account = accountStore.FindByUserName(userName);
	if(account == null)
	{
		return Results.Json(
			new { error = "账号或密码错误" },
			statusCode: StatusCodes.Status401Unauthorized);
	}
	var result = passwordHasher.VerifyHashedPassword(account,account.PasswordHash,password);
	if(result == PasswordVerificationResult.Failed)
	{
		return Results.Json(
			new { error = "账号或密码错误" },
			statusCode: StatusCodes.Status401Unauthorized);
	}
	string accessToken = Guid.NewGuid().ToString("N");
	sessions[accessToken] = new SessionInfo(account.PlayerId,account.Nickname);
	return Results.Json(new
	{
		accessToken,
		playerId = account.PlayerId,
		nickName = account.Nickname
	});

    //if(request.UserName != "saber"||
    //	request.Password != "Demo123!")
    //{
    //	return Results.Json(
    //		new {error = "用户名或密码错误"},
    //		statusCode: StatusCodes.Status401Unauthorized);
    //}

    //string accessToken = Guid.NewGuid().ToString("N");
    //sessions[accessToken] = new SessionInfo("player-saber","saber");
    //return Results.Json(new
    //{
    //	accessToken,
    //	playerId = "player-saber",
    //	nickname = "saber"
    //});
});

//登录验证
app.MapGet("/auth/me",(HttpRequest request)=>
{
	string authorization = request.Headers["Authorization"].ToString();
	if(!authorization.StartsWith("Bearer "))
	{
		return Results.Json(
			new {error = "缺少登录令牌"},
			statusCode:StatusCodes.Status401Unauthorized);
	}

	string token = authorization.Substring("Bearer ".Length).Trim();
	if(!sessions.TryGetValue(token,out var session))
	{
		return Results.Json(new {
			error = "登录令牌无效"
		},statusCode:StatusCodes.Status401Unauthorized);
	}
	return Results.Json(new {
		session.PlayerId,
		session.Nickname
	});
});
//读档
app.MapGet("/saves/me", (HttpRequest request) =>
{
	SessionInfo? session = GetSession(request);
	if(session == null)
	{
		return Results.Json(
			new { error = "请先登录" },
			statusCode: StatusCodes.Status401Unauthorized
			);
	}
	string? saveJson = gameSaveStore.Load(session.PlayerId);

	return Results.Json(new
	{
		hasSave = saveJson != null,
		saveJson = saveJson ?? ""
	});

});

app.MapPost("/saves/me", (HttpRequest request, SaveGameRequest input) =>
{
	SessionInfo? session =GetSession(request);
	if (session == null)
	{
		return Results.Json(
			new { error = "请先登录" },
			statusCode: StatusCodes.Status401Unauthorized);
	}
	if(string.IsNullOrWhiteSpace(input.SaveJson))
	{
		return Results.Json(
			new { error = "存档内容不能为空" },
			statusCode: StatusCodes.Status400BadRequest);
	}

	gameSaveStore.Save(session.PlayerId, input.SaveJson);
	return Results.Json(new
	{
		message = "保存成功"
	});

});



#region 好友申请相关
//通过好友码 对玩家进行搜索
app.MapGet("/players/search", (HttpRequest request, string friendCode) =>
{
    SessionInfo? session = GetSession(request);
    if (session == null)
    {
        return Results.Json(new
        {
            error = "请先登录"
        }, statusCode: StatusCodes.Status401Unauthorized);
    }
    if (string.IsNullOrWhiteSpace(friendCode))
    {
        return Results.Json(
            new { error = "好友码不能为空" },
            statusCode: StatusCodes.Status400BadRequest);
    }
    if (friendCode != "ZMZZ-RIN-002")
    {
        return Results.Json(
            new { error = "未找到玩家" },
            statusCode: StatusCodes.Status404NotFound);
    }
    return Results.Json(
        new
        {
            playerID = "player-rin",
            friendCode = "ZMZZ-RIN-002",
            nickname = "Rin",
            level = 12
        });
});

app.MapPost("/friends/requests", (HttpRequest httpRequest, FriendRequestCreate input) =>
{
    SessionInfo? session = GetSession(httpRequest);
    if (session == null)
    {
        return Results.Json(
            new { error = "请先登录" },
            statusCode: StatusCodes.Status401Unauthorized);
    }
    string targetPlayerId = (input.TargetPlayerId ?? "").Trim();
    if (string.IsNullOrWhiteSpace(targetPlayerId))
    {
        return Results.Json(
            new { error = "目标玩家不能为空" },
            statusCode: StatusCodes.Status400BadRequest);
    }
    if (targetPlayerId == session.PlayerId)
    {
        return Results.Json(
            new { error = "不能添加自己为好友" },
            statusCode: StatusCodes.Status400BadRequest);
    }
    if (targetPlayerId != "player-saber" && targetPlayerId != "player-rin")
    {
        return Results.Json(
            new { error = "目标玩家不存在" },
            statusCode: StatusCodes.Status404NotFound);
    }
    string relationKey = GetFriendPairKey(session.PlayerId, targetPlayerId);
    if (friendRelations.ContainsKey(relationKey))
    {
        return Results.Json(
            new { error = "双方已经是好友" },
            statusCode: StatusCodes.Status409Conflict);
    }
    bool hasPendingRequest = friendRequests.Values.Any(item =>
       item.Status == "pending" &&
       (
           item.FromPlayerId == session.PlayerId &&
           item.ToPlayerId == targetPlayerId
       ) ||
       (
           item.FromPlayerId == targetPlayerId &&
           item.ToPlayerId == session.PlayerId
       )
   );

    if (hasPendingRequest)
    {
        return Results.Json(
            new { error = "已经存在待处理的好友申请" },
            statusCode: StatusCodes.Status409Conflict);
    }

    string requestId = Guid.NewGuid().ToString("N");
    DateTime createdAt = DateTime.UtcNow;

    FriendRequest friendRequest = new FriendRequest(
        requestId,
        session.PlayerId,
        session.Nickname,
        targetPlayerId,
        "pending",
        createdAt
    );

    friendRequests[requestId] = friendRequest;

    return Results.Json(new
    {
        requestId,
        fromPlayerId = session.PlayerId,
        toPlayerId = targetPlayerId,
        status = "pending",
        createdAtUtc = createdAt
    }, statusCode: StatusCodes.Status201Created);
});
//查看好友申请
app.MapGet("/friends/requests", (HttpRequest httpRequest) =>
{
    SessionInfo? session = GetSession(httpRequest);

    if (session == null)
    {
        return Results.Json(
            new { error = "请先登录" },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var incomingRequests = friendRequests.Values
        .Where(item =>
            item.ToPlayerId == session.PlayerId &&
            item.Status == "pending")
        .OrderByDescending(item => item.CreatedAtUtc)
        .Select(item => new
        {
            item.RequestId,
            item.FromPlayerId,
            item.FromNickname,
            item.Status,
            item.CreatedAtUtc
        })
        .ToArray();

    return Results.Json(incomingRequests);
});

//对好友申请进行同意操作
app.MapPost("/friends/requests/{requestId}/accept", (HttpRequest httpRequest, string requestId) =>
{
    //获取请求中的会话信息
    SessionInfo? session = GetSession(httpRequest);
    //信息为空 返回
    if (session == null)
    {
        return Results.Json(
            new { error = "请先登录" },
            statusCode: StatusCodes.Status401Unauthorized
            );
    }
    //查询好友申请表中有无该申请信息 有的话 取出来
    if (!friendRequests.TryGetValue(requestId, out FriendRequest? friendRequest))
    {
        return Results.Json(
            new { error = "好友申请不存在" },
            statusCode: StatusCodes.Status404NotFound);
    }
    //
    if (friendRequest.ToPlayerId != session.PlayerId)
    {
        return Results.Json(
            new
            {
                error = "无权处理该好友申请"
            }, statusCode: StatusCodes.Status403Forbidden);
    }
    //该申请的状态是否已处理
    if (friendRequest.Status != "pending")
    {
        return Results.Json(
            new { error = "该申请已经处理" },
            statusCode: StatusCodes.Status409Conflict);
    }
    //好友关系键生成
    string pairKey = GetFriendPairKey(friendRequest.FromPlayerId, friendRequest.ToPlayerId);
    //好友关系id
    string relationId = Guid.NewGuid().ToString("N");
    DateTime createdAt = DateTime.UtcNow;
    //好友关系信息
    FriendRelation relation = new FriendRelation(
        relationId,
        friendRequest.FromPlayerId,
        friendRequest.ToPlayerId,
        createdAt);
    //判断好友关系表里有没有该关系 无 就加入 防止重复好友关系
    if (!friendRelations.TryAdd(pairKey, relation))
    {
        return Results.Json(
            new { error = "双方已经是好友" },
            statusCode: StatusCodes.Status409Conflict);
    }
    //修改好友申请中的状态信息
    friendRequests[requestId] = friendRequest with { Status = "accepted" };
    return Results.Json(new
    {
        relationId,
        playerAId = friendRequest.FromPlayerId,
        playerBId = friendRequest.ToPlayerId,
        status = "accepted",
        createdAtUtc = createdAt
    });
});

app.MapPost("/friends/requests/{requestId}/reject", (HttpRequest httpRequest, string requestId) =>
{
    //获取请求中的会话信息
    SessionInfo? session = GetSession(httpRequest);
    //信息为空 返回
    if (session == null)
    {
        return Results.Json(
            new { error = "请先登录" },
            statusCode: StatusCodes.Status401Unauthorized
            );
    }
    //查询好友申请表中有无该申请信息 有的话 取出来
    if (!friendRequests.TryGetValue(requestId, out FriendRequest? friendRequest))
    {
        return Results.Json(
            new { error = "好友申请不存在" },
            statusCode: StatusCodes.Status404NotFound);
    }
    //
    if (friendRequest.ToPlayerId != session.PlayerId)
    {
        return Results.Json(
            new
            {
                error = "无权处理该好友申请"
            }, statusCode: StatusCodes.Status403Forbidden);
    }
    //该申请的状态是否已处理
    if (friendRequest.Status != "pending")
    {
        return Results.Json(
            new { error = "该申请已经处理" },
            statusCode: StatusCodes.Status409Conflict);
    }

    DateTime createdAt = DateTime.UtcNow;
    friendRequests[requestId] = friendRequest with { Status = "rejected" };
    return Results.Json(new
    {

        requestId = friendRequest.RequestId,
        status = "rejected",
        createdAtUtc = createdAt
    });

});

//查看好友列表
app.MapGet("/friends", (HttpRequest httpRequest) =>
{
    SessionInfo? session = GetSession(httpRequest);
    if (session == null)
    {
        return Results.Json(
            new { error = "请先登录" },
            statusCode: StatusCodes.Status401Unauthorized
            );
    }
    var friends = friendRelations.Values.Where(item => item.PlayerAId == session.PlayerId ||
    item.PlayerBId == session.PlayerId).Select(item => new
    {
        item.RelationId,
        item.PlayerAId,
        item.PlayerBId,
        item.CreatedAtUtc
    }).ToArray();
    return Results.Json(friends);
});

#endregion

//聊天连接相关
app.Map("/ws/chat", async (HttpContext context) =>
{
	SessionInfo? session = GetSession(context.Request);
	//检查登录令牌
	if(session == null)
	{
		context.Response.StatusCode = 401;
		return;
	}
	//检查是否为WebSocket连接请求
	if(!context.WebSockets.IsWebSocketRequest)
	{
		context.Response.StatusCode = 400;
		return;
	}
	//接收客户端的webSocket连接 告诉客户端：服务器同意建立WebSocket连接 随后返回为websocket
	using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
	if(!chatConnections.TryAdd(session.PlayerId,socket))
	{
		await socket.CloseOutputAsync(
			WebSocketCloseStatus.PolicyViolation,
			"该玩家已经连接聊天服务器",
			context.RequestAborted
			);
		return;
	}
	Console.WriteLine($"{session.Nickname} 已上线,在线人数：{chatConnections.Count}");

	byte[] buffer = new byte[4096];
	try
	{
		//持续等待消息
		while(socket.State == WebSocketState.Open)
		{
			using MemoryStream messageStream = new MemoryStream();
			WebSocketReceiveResult result;
			do
			{
				result = await socket.ReceiveAsync(
					new ArraySegment<byte>(buffer),
					context.RequestAborted);
				//客户端请求关闭连接
				if (result.MessageType == WebSocketMessageType.Close)
				{
					await socket.CloseOutputAsync(
						WebSocketCloseStatus.NormalClosure,
						"连接已关闭",
						context.RequestAborted);
					return;
				}

				if (result.MessageType != WebSocketMessageType.Text)
				{
					await socket.CloseOutputAsync(
						WebSocketCloseStatus.InvalidMessageType,
						"只支持文字消息",
						context.RequestAborted);
					return;
				}

				messageStream.Write(buffer, 0, result.Count);
			}
			while (!result.EndOfMessage);


			byte[] messageBytes = messageStream.ToArray();
			string json = Encoding.UTF8.GetString(messageBytes);
			ChatSendRequest? input;
			try
			{
				input = JsonSerializer.Deserialize<ChatSendRequest>(json, chatJsonOptions);
			}
			catch(JsonException)
			{
				Console.WriteLine("聊天JSON格式错误" + json);
				continue;
			}
			if(input == null||string.IsNullOrEmpty(input.Content)||string.IsNullOrWhiteSpace(input.TargetPlayerId))
			{
				Console.WriteLine("接收人或消息内容不能为空");
				continue;
			}
			Console.WriteLine($"{session.Nickname}发给{input.TargetPlayerId}:{input.Content}");

			////目前测试 原样返回
			//await socket.SendAsync(
			//	new ArraySegment<byte>(buffer, 0, result.Count),
			//	WebSocketMessageType.Text,
			//	result.EndOfMessage,
			//	context.RequestAborted);

			string pairKey = GetFriendPairKey(session.PlayerId, input.TargetPlayerId);
			if(!friendRelations.ContainsKey(pairKey))
			{
				Console.WriteLine("转发失败 双方还不是好友");
				continue;
			}
			//根据接收人的ID 查找在线连接
			if(!chatConnections.TryGetValue(input.TargetPlayerId,out WebSocket?targetSocket)||targetSocket.State!=WebSocketState.Open)
			{
				Console.WriteLine("转发失败 对方不在线");
				continue;
			}
			//服务器补充发送人信息 使得客户端知道是谁发来的
			var outgoingMessage = new
			{
				fromPlayerId = session.PlayerId,
				fromNickname = session.Nickname,
				content = input.Content,
				createdAtUtc = DateTime.UtcNow
			};
			string outgoingJson = JsonSerializer.Serialize(outgoingMessage, chatJsonOptions);
			byte[] outgoingBytes = Encoding.UTF8.GetBytes(outgoingJson);
			//等待发送名额
			await chatSendGate.WaitAsync(context.RequestAborted);

			try
			{
				if (targetSocket.State != WebSocketState.Open)
				{
					Console.WriteLine("转发失败 对方已断线");
					continue;
				}
				await targetSocket.SendAsync(
					new ArraySegment<byte>(outgoingBytes),
					WebSocketMessageType.Text,
					true,
					context.RequestAborted);
				Console.WriteLine("消息已经发送给" + input.TargetPlayerId);
			}
			catch (WebSocketException ex)
			{
				Console.WriteLine("转发失败" + ex.Message);
			}
			catch (ObjectDisposedException)
			{
				Console.WriteLine("转发失败 对方连接已经释放");
			}
			finally
			{
				//释放发送锁
				chatSendGate.Release();
			}
		}
	}
	catch(OperationCanceledException)
	{
		Console.WriteLine($"{session.Nickname}的连接已取消");
	}
	catch(WebSocketException ex)
	{
		Console.WriteLine($"聊天连接中断：{ex.Message}");
	}
	finally
	{
		chatConnections.TryRemove(session.PlayerId, out _);
		Console.WriteLine($"{session.Nickname}已断开聊天连接,在线人数{chatConnections.Count}");
	}
});

app.Run("http://127.0.0.1:5080");

record LoginRequest(string UserName,string Password);
record SessionInfo(string PlayerId,string Nickname);
record FriendRequestCreate(string TargetPlayerId);
//好友申请的相关数据结构
record FriendRequest(
	string RequestId, // 申请的id 令牌
	string FromPlayerId, // 申请者id
	string FromNickname, // 申请者名字
	string ToPlayerId, //被申请者id
	string Status,  //申请状态
	DateTime CreatedAtUtc
	);
//好友关系的相关数据结构
record FriendRelation(
	string RelationId,
	string PlayerAId,
	string PlayerBId,
	DateTime CreatedAtUtc
	);
//聊天消息的相关数据结构
record ChatSendRequest(
	string TargetPlayerId,
	string Content);

//注册请求
record RegisterRequest
(
	string UserName,
	string Password,
	string Nickname
);
//服务器所记录的玩家账号信息
record UserAccount
(
	string UserName,
	string PasswordHash,
	string Nickname,
	string PlayerId
);


record SaveGameRequest(string SaveJson);