using Microsoft.Data.Sqlite;
using System.Globalization;

class FriendStore
{
    private readonly string connectionString;
    //构造函数传入的是 存储路径
    public FriendStore(string dataBasePath)
    {
        //指定要打开的数据库文件
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dataBasePath
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """"
CREATE TABLE IF NOT EXISTS FriendRequests
(
RequestId TEXT NOT NULL PRIMARY KEY,
FromPlayerId TEXT NOT NULL,
FromNickname TEXT NOT NULL,
ToPlayerId TEXT NOT NULL,
Status TEXT NOT NULL,
CreatedAtUtc TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS FriendRelations
(
RelationId TEXT NOT NULL PRIMARY KEY,
PairKey TEXT NOT NULL UNIQUE,
PlayerAId TEXT NOT NULL,
PlayerBId TEXT NOT NULL,
CreatedAtUtc TEXT NOT NULL
);
"""";
        command.ExecuteNonQuery();
    }

    //给数据库加入 一条好友申请数据
    public void CreateRequest(FriendRequest request)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """"
INSERT INTO FriendRequests
(
RequestId,
FromPlayerId,
FromNickname,
ToPlayerId,
Status,
CreatedAtUtc
)
VALUES
(
$requestId,
$fromPlayerId,
$fromNickname,
$toPlayerId,
$status,
$createdAtUtc
);
"""";
        command.Parameters.AddWithValue("$requestId", request.RequestId);
        command.Parameters.AddWithValue("$fromPlayerId", request.FromPlayerId);
        command.Parameters.AddWithValue("$fromNickname", request.FromNickname);
        command.Parameters.AddWithValue("$toPlayerId", request.ToPlayerId);
        command.Parameters.AddWithValue("$status", request.Status);
        command.Parameters.AddWithValue("$createdAtUtc", request.CreatedAtUtc.ToString("O"));

        command.ExecuteNonQuery();
    }

    //从数据库读取该玩家 的 好友申请的数据
    public List<FriendRequest> GetIncomingRequests(string playerId)
    {
        List<FriendRequest> requests = new List<FriendRequest>();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """"
SELECT
RequestId,
FromPlayerId,
FromNickname,
ToPlayerId,
Status,
CreatedAtUtc
FROM FriendRequests
WHERE ToPlayerId = $playerId
AND Status = 'pending'
ORDER BY CreatedAtUtc DESC;
"""";
        command.Parameters.AddWithValue("$playerId", playerId);
        using var reader = command.ExecuteReader();
        while(reader.Read())
        {
            DateTime createdAtUtc = DateTime.ParseExact(
                reader.GetString(5),
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            FriendRequest request = new FriendRequest(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3), 
                reader.GetString(4),
                createdAtUtc
                );
            requests.Add( request );
        }
        return requests;
    }

    //通过申请id 从数据库读取好友申请数据
    public FriendRequest? FindRequestById(string requestId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """"
SELECT
RequestId,
FromPlayerId,
FromNickname,
ToPlayerId,
Status,
CreatedAtUtc
FROM FriendRequests
WHERE RequestId = $requestId;
"""";
        command.Parameters.AddWithValue("$requestId", requestId);
        using var reader = command.ExecuteReader();
        if(!reader.Read())
        {
            return null;
        }
        DateTime createdAtUtc = DateTime.ParseExact(
                reader.GetString(5),
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        return new FriendRequest(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            createdAtUtc
            );
    }

    //读取好友关系
    public List<FriendRelation> GetFriends(string playerId)
    {
        List<FriendRelation> friends = new List<FriendRelation>();

        using var connection = new SqliteConnection(connectionString);

        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """"
SELECT
RelationId,
playerAId,
playerBId,
CreatedAtUtc
FROM FriendRelations
WHERE PlayerAId = $playerId
OR PlayerBId = $playerId
ORDER BY CreatedAtUtc DESC;
"""";
        command.Parameters.AddWithValue("$playerId", playerId);
        using var reader = command.ExecuteReader();
        while(reader.Read())
        {
            DateTime createdAtUtc = DateTime.ParseExact(
                reader.GetString(3),
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
                );
            FriendRelation relation = new FriendRelation(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), createdAtUtc);
            friends.Add(relation);
        }
        return friends;
    }

    //判断两者是不是好友
    public bool AreFriends(string playerAId, string playerBId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """"
SELECT 1
FROM FriendRelations
WHERE 
(PlayerAId = $playerAId AND PlayerBId = $playerBId)
OR
(PlayerAId = $playerBId AND PlayerBId = $playerAId)
LIMIT 1;
"""";
        command.Parameters.AddWithValue("$playerAId", playerAId);
        command.Parameters.AddWithValue("$playerBId", playerBId);

        using var reader = command.ExecuteReader();
        return reader.Read();
    }

    //修改申请状态 保存好友关系
    public FriendRelation? TryAcceptRequest(string requestId,string playerId)
    {
        FriendRequest? request = FindRequestById(requestId);
        if(request == null || request.ToPlayerId!=playerId||request.Status!="pending")
        {
            return null;
        }
        string pairKey;
        if(string.CompareOrdinal(request.FromPlayerId,request.ToPlayerId) <= 0)
        {
            pairKey = $"{request.FromPlayerId}|{request.ToPlayerId}";
        }
        else
        {
            pairKey = $"{request.ToPlayerId}|{request.FromPlayerId}";
        }
        FriendRelation relation = new FriendRelation(
            Guid.NewGuid().ToString("N"),
            request.FromPlayerId,
            request.ToPlayerId,
            DateTime.UtcNow);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        //开启事务
        using var transaction = connection.BeginTransaction();
        //第一条 改状态
        using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText = """"
UPDATE FriendRequests
SET Status = 'accepted'
WHERE RequestId = $requestId
AND FromPlayerId = $fromPlayerId
AND ToPlayerId = $playerId
AND Status = 'pending';
"""";
        updateCommand.Parameters.AddWithValue("$requestId", requestId);
        updateCommand.Parameters.AddWithValue("$fromPlayerId", request.FromPlayerId);
        updateCommand.Parameters.AddWithValue("$playerId", playerId);

        if(updateCommand.ExecuteNonQuery()!= 1)
        {
            transaction.Rollback();
            return null;
        }


        //第二条保存好友关系
        using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;

        insertCommand.CommandText = """"
INSERT INTO FriendRelations
(
RelationId,
PairKey,
PlayerAId,
PlayerBId,
CreatedAtUtc
)
VALUES
(
$relationId,
$pairKey,
$playerAId,
$playerBId,
$createdAtUtc
)
ON CONFLICT(PairKey) DO NOTHING;
"""";
        insertCommand.Parameters.AddWithValue("$relationId", relation.RelationId);
        insertCommand.Parameters.AddWithValue("$pairKey", pairKey);
        insertCommand.Parameters.AddWithValue("$playerAId", relation.PlayerAId);
        insertCommand.Parameters.AddWithValue("$playerBId", relation.PlayerBId);
        insertCommand.Parameters.AddWithValue("$createdAtUtc", relation.CreatedAtUtc.ToString("O"));

        if(insertCommand.ExecuteNonQuery()!= 1)
        {
            transaction.Rollback();
            return null;
        }
        transaction.Commit();
        return relation;

    }
}