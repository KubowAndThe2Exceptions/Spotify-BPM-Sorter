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

        public void CompareTrack()
        {

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
        public void FindTrack(DbTrack Track)
        {

        }
        public void Exists(DbTrack track)
        {

        }
        //public bool GetTempo(DbTrack track)
        //{

        //}
        public void GetAllTracks()
        {

        }

        //public DbTrack GetTrack() {}
        //public void InsertTrack(DbTrack track) {}
        //public void InsertTrackList(List<DbTrack> tracklist) {}
    }
}
