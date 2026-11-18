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
}