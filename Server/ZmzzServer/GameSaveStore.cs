using Microsoft.Data.Sqlite;

class GameSaveStore
{
    private readonly string connectionString;

    public GameSaveStore(string dataPath)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dataPath
        }.ToString();
    
    using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """"
     CREATE TABLE IF NOT EXISTS GameSaves
     (
        PlayerId TEXT NOT NULL PRIMARY KEY,
        SaveJson TEXT NOT NULL,
        UpdateAtUTC TEXT NOT NULL
     );
"""";


        command.ExecuteNonQuery();
    }


    public void Save(string playerId,string saveJson)
    {
        string updateAtUtc = DateTime.UtcNow.ToString("O");

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """"
     INSERT INTO GameSaves
        (PlayerId,SaveJson,UpdateAtUTC)
        VALUES
        ($playerId,$saveJson,$updateAtUtc)
        ON CONFLICT(PlayerId) DO UPDATE SET
        SaveJson = excluded.SaveJson,
        UpdateAtUTC = excluded.UpdateAtUTC;
"""";
        command.Parameters.AddWithValue("$playerId", playerId);
        command.Parameters.AddWithValue("$saveJson", saveJson);
        command.Parameters.AddWithValue("$updateAtUtc", updateAtUtc);

        command.ExecuteNonQuery();
    }

    public string? Load(string playerId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = """"
     SELECT SaveJson
     FROM GameSaves
     WHERE PlayerId = $playerId;
"""";
        command.Parameters.AddWithValue("$playerId", playerId);

        using var reader = command.ExecuteReader();
        if(!reader.Read())
        {
            return null;
        }
        return reader.GetString(0);
    }
}
     