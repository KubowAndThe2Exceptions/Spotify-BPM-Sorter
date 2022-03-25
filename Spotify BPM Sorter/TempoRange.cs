using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class TempoRange
    {
        public List<DbTrack> LowerTempo = new List<DbTrack>();
        public List<DbTrack> HigherTempo = new List<DbTrack>();

        public TempoRange()
        {
        }

        public int Total()
        {
            var total = LowerTempo.Count + HigherTempo.Count;
            return total;
        }
    }
}
