using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Spotify_BPM_Sorter
{
    class PlayListMaker
    {
        public string TargetPlaylist { get; set; }
        public TempoRange LowRange { get; set; }
        public TempoRange MidRange { get; set; }
        public TempoRange HighRange { get; set; }
        public List<DbTrack> TrackList { get; set; } = new List<DbTrack>();
        public List<DbTrack> TempoProblems { get; set; } = new List<DbTrack>();
        public SpotifyClient Spotify { get; set; }

        public PlayListMaker(SpotifyClient spotify)
        {
            DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            string HHPlaylist = DotNetEnv.Env.GetString("HH_PLAYLIST");
            string HLPlaylist = DotNetEnv.Env.GetString("HL_PLAYLIST");
            string MHPlaylist = DotNetEnv.Env.GetString("MH_PLAYLIST");
            string MLPlaylist = DotNetEnv.Env.GetString("ML_PLAYLIST");
            string LHPlaylist = DotNetEnv.Env.GetString("LH_PLAYLIST");
            string LLPlaylist = DotNetEnv.Env.GetString("LL_PLAYLIST");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

            HighRange = new TempoRange(new Tempo(HLPlaylist), new Tempo(HHPlaylist));
            MidRange = new TempoRange(new Tempo(MLPlaylist), new Tempo(MHPlaylist));
            LowRange = new TempoRange(new Tempo(LLPlaylist), new Tempo(LHPlaylist));

            Spotify = spotify;
        }
    }
}
