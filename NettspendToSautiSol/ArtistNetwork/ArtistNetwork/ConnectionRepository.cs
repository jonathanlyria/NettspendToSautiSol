using Microsoft.Data.Sqlite;
namespace NettspendToSautiSol;

public class ConnectionRepository : IConnectionRepository
{
    private readonly string _connectionString;
    public ConnectionRepository( string connectionString)
    {
        _connectionString = connectionString;
    }
    public bool IsConnectionInDb(string spotifyId1, string spotifyId2)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string selectQuery = @"
                SELECT * FROM Connections
                WHERE (SpotifyId1 = @spotifyId1 AND SpotifyId2 = @spotifyId2)
                OR (SpotifyId1 = @spotifyId2 AND SpotifyId2 = @spotifyId1);";

            using (SqliteCommand cmd = new SqliteCommand(selectQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId1", spotifyId1);
                cmd.Parameters.AddWithValue("@spotifyId2", spotifyId2);
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return true;
                    }
                    return false;
                }

            }
        }
    }
    public void AddConnectionToDb(string spotifyId1, string spotifyId2, double weight)
    {
        using (SqliteConnection connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            string insertQuery = @"
                INSERT INTO Connections (SpotifyId1, SpotifyId2, Weight)
                VALUES (@spotifyId1, @spotifyId2, @strength);";
            using (SqliteCommand cmd = new SqliteCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@spotifyId1", spotifyId1);
                cmd.Parameters.AddWithValue("@spotifyId2", spotifyId2);
                cmd.Parameters.AddWithValue("@strength", weight);
                cmd.ExecuteNonQuery();
            }
        }

    }
}