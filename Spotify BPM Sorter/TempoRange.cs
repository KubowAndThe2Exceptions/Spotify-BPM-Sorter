using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class TempoRange
    {
        public static List<Tempo> AllTempos = new List<Tempo>();
        public Tempo LowerTempo;
        public Tempo HigherTempo;

        public TempoRange(Tempo lowerTempo, Tempo higherTempo)
        {
            LowerTempo = lowerTempo;
            HigherTempo = higherTempo;
            AllTempos.Add(lowerTempo);
            AllTempos.Add(higherTempo);
            //add sorting in future
        }

        public int Total()
        {
            var total = LowerTempo.Tracklist.Count + HigherTempo.Tracklist.Count;
            return total;
        }
    }
}
