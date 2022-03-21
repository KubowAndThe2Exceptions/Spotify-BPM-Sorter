using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class DbTrack
    {
        public string Name { get; set; }
        public string TrackId { get; set; }
        public float Loudness { get; set; }

        public DbTrack(string name, string trackId, float loudness)
        {
            Name = name;
            TrackId = trackId;
            Loudness = loudness;
        }
        public DbTrack(string name, string trackId)
        {
            Name = name;
            TrackId = trackId;
        }
    }
}
