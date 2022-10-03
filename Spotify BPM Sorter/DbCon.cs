using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Spotify_BPM_Sorter
{
    class DbCon
    {
        public string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\SpotifyDB.mdf"";Integrated Security=True";

        public DbCon()
        {

        }

        public void TestConnection()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                
            }
        }


        public void StoreTrack(DbTrack track)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string sqlstring = "INSERT INTO SpotifyTrack VALUES (@TrackId, @Name, @Artists, @Tempo, @AlbumName, @Uri, @DurationMs)";
                SqlCommand cmd = new SqlCommand(sqlstring, connection);
                cmd.Parameters.Add("@TrackId", System.Data.SqlDbType.NVarChar).Value = track.TrackId;
                cmd.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar).Value = track.Name;
                cmd.Parameters.Add("@Artists", System.Data.SqlDbType.NVarChar).Value = track.Artists;
                cmd.Parameters.Add("@Tempo", System.Data.SqlDbType.Float).Value = track.Tempo;
                cmd.Parameters.Add("@AlbumName", System.Data.SqlDbType.NVarChar).Value = track.AlbumName;
                cmd.Parameters.Add("@Uri", System.Data.SqlDbType.NVarChar).Value = track.Uri;
                cmd.Parameters.Add("@DurationMs", System.Data.SqlDbType.Int).Value = track.DurationMs;
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                connection.Close();
            }
        }
        
        public void FixArtists(string artists, string id)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string sqlstring = "UPDATE SpotifyTrack SET Artists=@Artists WHERE TrackId=@TrackId;";
                SqlCommand cmd = new SqlCommand(sqlstring, connection);
                cmd.Parameters.Add("@Artists", System.Data.SqlDbType.NVarChar).Value = artists;
                cmd.Parameters.Add("@TrackId", System.Data.SqlDbType.NVarChar).Value = id;

                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }
        public bool Exists(string id)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                bool boolean = false;
                connection.Open();
                //string sqlstring = "SELECT * FROM SpotifyTrack WHERE TrackId='@TrackId';";
                string sqlstring = "SELECT * FROM SpotifyTrack WHERE TrackId=@TrackId;";
                SqlCommand cmd = new SqlCommand(sqlstring, connection);
                cmd.Parameters.Add("@TrackId", System.Data.SqlDbType.NVarChar).Value = id;

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.HasRows)
                    {
                        boolean = true;
                    }
                }
                reader.Close();
                connection.Close();

                return boolean;
            }
        }
        public void SetTempo(float tempo, string id)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string sqlstring = "UPDATE SpotifyTrack SET Tempo=@Tempo WHERE TrackId=@TrackId;";
                SqlCommand cmd = new SqlCommand(sqlstring, connection);
                cmd.Parameters.Add("@Tempo", System.Data.SqlDbType.Float).Value = tempo;
                cmd.Parameters.Add("@TrackId", System.Data.SqlDbType.NVarChar).Value = id;

                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }
        public float GetTempo(string id)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                float tempo = 0;
                connection.Open();
                string sqlstring = "SELECT Tempo FROM SpotifyTrack WHERE TrackId=@TrackId";
                SqlCommand cmd = new SqlCommand(sqlstring, connection);
                cmd.Parameters.Add("@TrackId", System.Data.SqlDbType.NVarChar).Value = id;

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine(reader[0].ToString());
                    tempo = (float)reader.GetDouble(0);
                }
                reader.Close();
                connection.Close();
                
                return tempo;

            }
        }
    }
}
