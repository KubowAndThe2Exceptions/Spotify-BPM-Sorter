using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Spotify_BPM_Sorter
{
    class Generator
    {
        public static TempoRange HighTempo;
        public static TempoRange MidTempo;
        public static TempoRange LowTempo;
        public static List<DbTrack> FinalList = new List<DbTrack>();
        public static Random ran = new Random();

        public Generator(TempoRange highTempo, TempoRange midTempo, TempoRange lowTempo)
        {
            HighTempo = highTempo;
            MidTempo = midTempo;
            LowTempo = lowTempo;
        }

        private int RemoveTempo()
        {
            //Picks random number between -35 and -25
            int negTemp = ran.Next(-35, -25);
            return negTemp;
        }
        private int AddTempo()
        {
            //Picks a random number between 25 and 35
            int posTemp = ran.Next(25, 35);
            return posTemp;
        }
        private bool IsRare()
        {
            //Picks 1-100, 50% chance to return true
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
        private void RemoveTrack(DbTrack track, TempoRange range)
        {
            int index = range.Tracklist.FindIndex(x => x == track);
            range.Tracklist.RemoveAt(index);
        }
        private void AddSong(int tempo, TempoRange range)
        {
            //create list<int> from range that allows 5 point deviation.
            //ex: 100 becomes 195-105
            int rangeBottom = tempo - 5;
            int rangeCount = 10;
            List<int> intlist = new List<int>();
            intlist.AddRange(Enumerable.Range(rangeBottom, rangeCount).ToList());
            
            //Send list<int> to Tempo, add returned track
            DbTrack track = range.GetSong(intlist);
            RemoveTrack(track, range);
            FinalList.Add(track);
        }
        private void AddRareSong(int tempo, TempoRange range)
        {
            //If tempo currently high, pick a high song from the rare tempos, vice versa
            DbTrack track;
            List<int> intlist = new List<int>();
            if (tempo < 120)
            {
                intlist.AddRange(Enumerable.Range(60, 40).ToList());
                track = range.GetSong(intlist);
                RemoveTrack(track, range);
                FinalList.Add(track);
            }
            else
            {
                intlist.AddRange(Enumerable.Range(185, 55).ToList());
                track = range.GetSong(intlist);
                RemoveTrack(track, range);
                FinalList.Add(track);
            }
        }
        public List<DbTrack> NewGenPlaylist()
        {
            //oscilates up and down in tempo, checks for rare song placement
            //in high or low tempo ranges, then returns the final list
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