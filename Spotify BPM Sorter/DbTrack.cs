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
        public float Tempo { get; set; }

        public DbTrack(string name, string trackId, float tempo)
        {
            Name = name;
            TrackId = trackId;
            Tempo = tempo;
        }
        public DbTrack(string name, string trackId)
        {
            Name = name;
            TrackId = trackId;
        }
        public DbTrack(string trackId)
        {
            TrackId = trackId;
        }
    }
}
