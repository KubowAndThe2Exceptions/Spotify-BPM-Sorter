﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class Tempo
    {
        public static List<Tempo> AllTempos = new List<Tempo>();
        public List<DbTrack> Tracklist = new List<DbTrack>();
        public static Random Ran = new Random();
        public string Id { get; set; } = string.Empty;

        public Tempo()
        {
            AllTempos.Add(this);
        }
        public Tempo(string id)
        {
            AllTempos.Add(this);
            Id = id;
        }

        public DbTrack GetSong(List<int> tempoRange)
        {
            DbTrack dbTrack;
            int selected;
            //Find songs that match in this range
            var pickableTracks = this.Tracklist.FindAll(t => tempoRange.First<int>() <= t.Tempo && t.Tempo <= tempoRange.Last<int>());
            if (pickableTracks.Count == 0)
            {
                selected = Ran.Next(0, this.Tracklist.Count);
                dbTrack = this.Tracklist[selected];
                return dbTrack;
            }
            //select random track
            selected = Ran.Next(0, (pickableTracks.Count - 1));
            dbTrack = pickableTracks[selected];
            //remove that track
            pickableTracks.Clear();
            return dbTrack;
        }
    }
}
