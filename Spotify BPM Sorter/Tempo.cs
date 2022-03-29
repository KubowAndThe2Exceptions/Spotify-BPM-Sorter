using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class Tempo
    {
        public List<DbTrack> Tracklist = new List<DbTrack>();
        public string Id { get; set; } = string.Empty;

        public Tempo()
        {

        }
        public Tempo(string id)
        {
            Id = id;
        }
    }
}
