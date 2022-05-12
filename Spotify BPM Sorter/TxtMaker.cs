using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Spotify_BPM_Sorter
{
    public static class TxtMaker
    {
        public static void CreateGeneratedPlaylistTxt(List<DbTrack> list)
        {
            string date = DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-tt");
            string txtContent = ListToTxt(list, date);
            string txtname = date + ".txt";
            string txtpath = @"C:\Users\Amazingg\Desktop\Generated Playlists\" + txtname;
            File.WriteAllText(txtpath, txtContent);
        }
        public static void CreateNewSongsTxt(List<DbTrack> list)
        {
            string date = DateTime.Now.ToString("MM-dd-yyyy_hh-mm-ss-tt");
            string txtContent = ListToTxt(list, date);
            string txtname = date + ".txt";
            string txtpath = @"C:\Users\Amazingg\Desktop\Generated Playlists\New Songs\" + txtname;
            File.WriteAllText(txtpath, txtContent);
        }
        private static string ListToTxt(List<DbTrack> list, string date)
        {
            string txtContent = date + "\r";
            foreach (var item in list)
            {
                txtContent += item.ToString();
            }
            return txtContent;
        }
    }
}
