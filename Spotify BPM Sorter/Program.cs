using System;
using System.Data.SqlClient;

namespace Spotify_BPM_Sorter
{
    class Program
    {
        static void Main(string[] args)
        {
            DbClass db = new DbClass();
            db.TestConnection();
        }
    }
}
