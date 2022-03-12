using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Spotify_BPM_Sorter
{
    class DbClass
    {
        public string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\SpotifyDB.mdf"";Integrated Security=True";

        public DbClass()
        {

        }

        public void TestConnection()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                Console.WriteLine("Connection is open");
            }
        }
    }
}
