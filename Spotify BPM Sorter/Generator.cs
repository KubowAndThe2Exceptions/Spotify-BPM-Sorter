using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter
{
    class Generator
    {
        public static Tempo HighTempo;
        public static Tempo MidTempo;
        public static Tempo LowTempo;
        public static List<DbTrack> FinalList = new List<DbTrack>();
        public static Random ran = new Random();

        public Generator(Tempo highTempo, Tempo midTempo, Tempo lowTempo)
        {
            HighTempo = highTempo;
            MidTempo = midTempo;
            LowTempo = lowTempo;
        }

        private int RemoveTempo()
        {
            int negTemp = ran.Next(-35, -25);
            return negTemp;
        }
        private int AddTempo()
        {
            int posTemp = ran.Next(25, 35);
            return posTemp;
        }
        private bool IsRare()
        {
            int chance = ran.Next(1, 100);
            if (chance <= 50)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void AddSong(int tempo, Tempo songs)
        {
            int rangeBottom = tempo - 5;
            int rangeTop = 10;
            List<int> intlist = new List<int>();
            //create list<int> from range that allows 5 point deviation.
            //ex: 100 becomes 195-105
            intlist.AddRange(Enumerable.Range(rangeBottom, rangeTop).ToList());
            //Send list<int> to Tempo, add returned track
            DbTrack track = songs.GetSong(intlist);
            int index = songs.Tracklist.FindIndex(x => x == track);
            songs.Tracklist.RemoveAt(index);
            FinalList.Add(track);
        }
        private void AddRareSong(int tempo, Tempo songs)
        {
            DbTrack track;
            List<int> intlist = new List<int>();
            if (tempo < 120)
            {
                intlist.AddRange(Enumerable.Range(60, 40).ToList());
                track = songs.GetSong(intlist);
                FinalList.Add(track);
            }
            else
            {
                intlist.AddRange(Enumerable.Range(185, 55).ToList());
                track = songs.GetSong(intlist);
                FinalList.Add(track);
            }
        }
        public List<DbTrack> NewGenPlaylist()
        {
            int currentTempo = 130;
            bool up = true;
            for (var i = 0; i < 20; i++)
            {
                if (currentTempo < 120)
                {
                    up = true;
                    if (currentTempo <= 105 && IsRare())
                    {
                        AddRareSong(currentTempo, LowTempo);

                        int difference = 120 - currentTempo;
                        currentTempo += difference;
                    }
                    else 
                    {
                        AddSong(currentTempo, LowTempo);
                        currentTempo += AddTempo();
                    }
                }
                else if (120 <= currentTempo && currentTempo <= 160)
                {
                    AddSong(currentTempo, MidTempo);

                    switch (up)
                    {
                        case true:
                            currentTempo += AddTempo();
                            break;
                        case false:
                            currentTempo += RemoveTempo();
                            break;
                    }
                }
                //Would make a single else statement, but I find this more readable
                else if (160 < currentTempo)
                {
                    up = false;
                    if (175 <= currentTempo && IsRare())
                    {
                        AddRareSong(currentTempo, HighTempo);

                        int difference = currentTempo - 160;
                        currentTempo -= difference;
                    }
                    else
                    {
                        AddSong(currentTempo, HighTempo);
                        currentTempo += RemoveTempo();
                    }
                }
            }

            return FinalList;

        }
    }
}