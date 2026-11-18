using Microsoft.Data.Sqlite;

class AccountStore
{
    private readonly string connectionString;

    public AccountStore(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        //建立数据库连接对象                           这里是要打开那个文件 
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        //准备命令对象
        using var command = connection.CreateCommand();
        //编写要执行的SQL
        command.CommandText = """"
         CREATE TABLE IF NOT EXISTS Accounts
            (
                PlayerId TEXT PRIMARY KEY,
                UserName TEXT NOT NULL UNIQUE,
                Nickname TEXT NOT NULL,
                PasswordHash TEXT NOT NULL
            );
"""";

        //执行
        command.ExecuteNonQuery();

    }

    //将账号保存至数据库
    public void CreateAccount(UserAccount account)
    {
        using var connection = new SqliteConnection( connectionString);

        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = """"
               INSERT INTO Accounts
               (PlayerID,UserName,Nickname,PasswordHash)
               VALUES
               ($playerId,$userName,$nickname,$passwordHash);
               """";
        command.Parameters.AddWithValue("$playerId", account.PlayerId);
        command.Parameters.AddWithValue("$userName", account.UserName);
        command.Parameters.AddWithValue("$nickname", account.Nickname);
        command.Parameters.AddWithValue("$passwordHash", account.PasswordHash);
        command.ExecuteNonQuery();

    }
    public UserAccount? FindByUserName(string userName)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = """"
SELECT UserName,PasswordHash,Nickname,PlayerId
FROM Accounts
WHERE UserName = $userName;
"""";
        command.Parameters.AddWithValue("$userName", userName);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }
        return new UserAccount(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3)
            );
    }
}