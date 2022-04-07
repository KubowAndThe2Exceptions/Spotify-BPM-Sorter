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
            int negTemp = ran.Next(-25, -35);
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
            if (chance <= 20)
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
            //create list<int> from range that allows 5 point deviation.
            //ex: 100 becomes 195-105
            //Send list<int> to songs
            //receive dbtrack
            //return dbtrack
        }
        private void AddRareSong(int tempo, Tempo songs)
        {
            //if tempo is low
                //send rare list<int>
                //return dbtrack
            //else
                //send high rare list<int>
                //return dbtrack

        }
        public List<DbTrack> NewGenPlaylist()
        {
            int currentTempo = 1;
            bool up = true;
            for (var i = 0; i < 20; i--)
            {
                if (currentTempo < 120)
                {
                    up = true;
                    if (currentTempo <= 100 && IsRare())
                    {
                        //Grab rare song
                        
                        //Add whatever is necessary to get
                            //to medium range.
                    }
                    else 
                    {
                        //Grab normal song
                        currentTempo += AddTempo();
                    }
                }
                else if (120 <= currentTempo && currentTempo <= 160)
                {
                    //grab song

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
                    if (180 <= currentTempo && IsRare())
                    {
                        //Grab rare song

                        //Remove whatever is necessary to get
                            //to medium range.
                    }
                    else
                    {
                        //Grab normal song
                        currentTempo += RemoveTempo();
                    }
                }
            }



















            //int pickedTrack = 0;
            //int cleanIntervals = 0;
            //for (int i = 20; i > 0; i--)
            //{
            //    switch (songNum)
            //    {

            //        case 4:
            //            pickedTrack = ran.Next(0, (HighTempoList.Tracklist.Count - 1));
            //            GeneratedList.Add(HighTempoList.Tracklist[pickedTrack]);
            //            HighTempoList.Tracklist.RemoveAt(pickedTrack);
            //            songNum = 2;
            //            break;

            //        case 3:
            //            pickedTrack = ran.Next(0, (MidTempoList.Tracklist.Count - 1));
            //            GeneratedList.Add(MidTempoList.Tracklist[pickedTrack]);
            //            MidTempoList.Tracklist.RemoveAt(pickedTrack);
            //            songNum = 4;
            //            break;

            //        case 2:
            //            pickedTrack = ran.Next(0, (MidTempoList.Tracklist.Count - 1));
            //            GeneratedList.Add(MidTempoList.Tracklist[pickedTrack]);
            //            MidTempoList.Tracklist.RemoveAt(pickedTrack);
            //            songNum = 1;
            //            break;

            //        case 1:
            //            pickedTrack = ran.Next(0, (LowTempoList.Tracklist.Count - 1));
            //            GeneratedList.Add(LowTempoList.Tracklist[pickedTrack]);
            //            LowTempoList.Tracklist.RemoveAt(pickedTrack);
            //            songNum = 3;
            //            break;
            //    }
            //}
        }

    }
}