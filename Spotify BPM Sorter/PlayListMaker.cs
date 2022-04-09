﻿using System;
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
        private string CurrentUserId { get; set; }
        public Tempo LowTempoList { get; set; }
        public Tempo MidTempoList { get; set; }
        public Tempo HighTempoList { get; set; }
        private static Generator Gen { get; set; }
        public List<DbTrack> TrackList { get; set; } = new List<DbTrack>();
        public List<DbTrack> GeneratedList { get; set; } = new List<DbTrack>();
        public List<DbTrack> TempoProblems { get; set; } = new List<DbTrack>();
        public SpotifyClient Spotify { get; set; }
        
        public DbCon DataBaseContext = new DbCon();

        public static async Task<PlayListMaker> CreateAsync(SpotifyClient spotify)
        {
            var playListMaker = new PlayListMaker(spotify);
            await playListMaker.RequestUserIdAsync();
            await playListMaker.FillTrackListAsync();
            return playListMaker;
        }
        
        private PlayListMaker(SpotifyClient spotify)
        {
            DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            string HHPlaylist = DotNetEnv.Env.GetString("HH_PLAYLIST");
            string MHPlaylist = DotNetEnv.Env.GetString("MH_PLAYLIST");
            string MLPlaylist = DotNetEnv.Env.GetString("ML_PLAYLIST");
            string LLPlaylist = DotNetEnv.Env.GetString("LL_PLAYLIST");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

            HighTempoList = new Tempo(HHPlaylist);
            MidTempoList = new Tempo(MHPlaylist);
            LowTempoList = new Tempo(LLPlaylist);

            Spotify = spotify;

        }

        private async Task<int> GetPlaylistTotalAsync(string playlistId)
        {
            //new playlistrequest only asking for the total num of songs
            var playlistGIR = new PlaylistGetItemsRequest();
            playlistGIR.Fields.Add("total");

            //calls playlist and extracts total num of songs
            var playlistTotalSongData = await Spotify.Playlists.GetItems(playlistId, playlistGIR);

            return (int)playlistTotalSongData.Total;
        }

        private async Task RequestUserIdAsync()
        {
            var userInfo = await Spotify.UserProfile.Current();
            CurrentUserId = userInfo.Id;
        }
        private async Task FillTrackListAsync()
        {
            int totalSongs = GetPlaylistTotalAsync(TargetPlaylist).Result;
            int calledSongs = 0;
            int offset = 0;
            while (calledSongs <= totalSongs)
            {
                var list = new List<DbTrack>();
                var playlist = await Spotify.Playlists.GetItems(TargetPlaylist, new PlaylistGetItemsRequest { Offset = offset });
                foreach (var item in playlist.Items)
                {
                    if (item.Track is FullTrack fullTrack)
                    {
                        DbTrack track = new DbTrack(fullTrack.Name, fullTrack.Id, fullTrack.Uri, 
                            fullTrack.DurationMs, fullTrack.Artists, fullTrack.Album.Name);
                        list.Add(track);
                    }
                }
                
                //Adds tracks to TrackList
                TrackList.AddRange(list);

                var count = (int)playlist.Items.Count;
                calledSongs += count;
                offset = calledSongs - 1;
            }
            Console.WriteLine("Track List Filled. . .");
            await AddTrackAnalysisInfoAsync();
            DetectTempoProblems();
            SortTempos();

        }
        private async Task AddTrackAnalysisInfoAsync()
        {
            var totalSongs = TrackList.Count;
            int calledSongs = 0;

            while (calledSongs < totalSongs)
            {
                //Gets number of songs left to call, only allows <= 100 ids through
                var difference = totalSongs - calledSongs;
                var amountToCall = 0;
                if (difference > 100)
                {
                    amountToCall = 100;
                }
                else
                {
                    amountToCall = difference;
                }

                //Adds <= 100 track ids into a list of strings
                List<string> extractedStrings = new List<string>();
                List<DbTrack> listToExtract = TrackList.GetRange(calledSongs, amountToCall);
                foreach (var track in listToExtract)
                {
                    extractedStrings.Add(track.TrackId);
                }

                //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                var trackFeatures = await Spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                foreach (var track in trackFeatures.AudioFeatures)
                {
                    int index;
                    if (DataBaseContext.Exists(track.Id))
                    {
                        var tempo = DataBaseContext.GetTempo(track.Id);
                        if (tempo == 0 && track.Tempo > 0)
                        {
                            tempo = track.Tempo;
                            DataBaseContext.SetTempo(track.Tempo, track.Id);
                        }
                        index = TrackList.FindIndex(t => t.TrackId == track.Id);
                        TrackList[index].Tempo = tempo;
                        DataBaseContext.FixArtists(TrackList[index].Artists, track.Id);
                    }
                    else
                    {
                        index = TrackList.FindIndex(t => t.TrackId == track.Id);
                        TrackList[index].Tempo = track.Tempo;
                        Console.WriteLine("New Track Added!");
                        TrackList[index].Display();
                        DataBaseContext.StoreTrack(TrackList[index]);
                    }
                }
                calledSongs += amountToCall;
            }
            Console.WriteLine("Track Analysis Finished, Tempos Acquired.");
        }

        private void DetectTempoProblems()
        {
            TempoProblems = TrackList.FindAll(t => t.Tempo == 0);
            foreach (var problem in TempoProblems)
            {
                int index = TrackList.FindIndex(t => t == problem);
                TrackList.RemoveAt(index);
            }

            TrackList.Sort((x, y) => x.Tempo.CompareTo(y.Tempo));
            Console.WriteLine("Tempo Problems Detected");
            Console.WriteLine("Lowest Tempo: {0}", TrackList[0].Tempo);
            Console.WriteLine("Highest Tempo: {0}", TrackList.Last().Tempo);
        }
        
        private void SortTempos()
        {
            foreach (var track in TrackList)
            {
                if (track.Tempo < 120)
                {
                    LowTempoList.Tracklist.Add(track);
                }
                else if (track.Tempo <= 160)
                {
                    if (track.Tempo < 140)
                    {
                        MidTempoList.Tracklist.Add(track);
                    }
                    else
                    {
                        MidTempoList.Tracklist.Add(track);
                    }
                }
                else
                {
                    HighTempoList.Tracklist.Add(track);
                }
            }
            Console.WriteLine("Tempos Internally Sorted");
            Console.WriteLine("Number of L: {0}", LowTempoList.Tracklist.Count);
            Console.WriteLine("Number of M {0}", MidTempoList.Tracklist.Count);
            Console.WriteLine("Number of H: {0}", HighTempoList.Tracklist.Count);
        }

        public async Task GenerateSpotifyPlaylistAsync()
        {
            Gen = new Generator(HighTempoList, MidTempoList, LowTempoList);
            GeneratedList = await Task.Run(() => Gen.NewGenPlaylist());
            await DisplayGenListAsync();
        }
        private async Task DisplayGenListAsync()
        {
            foreach (var track in GeneratedList)
            {
                track.Display();
            }
            Console.WriteLine("Does this look okay? (y/n)");
            var answer = Console.ReadKey();
            if (answer.Key == ConsoleKey.Y)
            {
                var genPlaylist = await Spotify.Playlists.Create(CurrentUserId, new PlaylistCreateRequest(DateTime.Today.Date.ToString("d") + " GPlaylist"));
                await AddGeneratedSongs(genPlaylist.Id);
                Console.WriteLine("Finished Generating");
            }
            else
            {
                Console.WriteLine("My bad! Do you want me to try again? (y/n)");
                answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Y)
                {
                    await GenerateSpotifyPlaylistAsync();
                }
            }
        }
        private async Task AddGeneratedSongs(string playlistId)
        {
            int totalSongs = GeneratedList.Count();
            int calledSongs = 0;

            while (calledSongs < totalSongs)
            {
                //Gets number of songs left to call, only allows <= 100 ids through
                var difference = totalSongs - calledSongs;
                var amountToCall = 0;
                if (difference > 100)
                {
                    amountToCall = 100;
                }
                else
                {
                    amountToCall = difference;
                }

                //Adds <= 100 track ids into a list of strings
                List<string> extractedStrings = new List<string>();
                List<DbTrack> listToExtract = GeneratedList.GetRange(calledSongs, amountToCall);
                foreach (var track in listToExtract)
                {
                    extractedStrings.Add(track.Uri);
                }

                //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                await Spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(extractedStrings));

                calledSongs += amountToCall;
            }
        }
        //private async Task<List<string>> ExtractSpotifySongIdsAsync(string playlistId)
        //{
        //    int totalSongs = GetPlaylistTotalAsync(playlistId).Result;
        //    int calledSongs = 0;
        //    int offset = 0;
        //    List<string> listToReturn = new List<string>();
        //    while (calledSongs <= totalSongs)
        //    {
        //        var list = new List<string>();
        //        var playlist = await Spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Offset = offset });
        //        foreach (var item in playlist.Items)
        //        {
        //            if (item.Track is FullTrack fullTrack)
        //            {
        //                list.Add(fullTrack.Uri);
        //            }
        //        }

        //        //Adds tracks to TrackList
        //        listToReturn.AddRange(list);

        //        var count = (int)playlist.Items.Count;
        //        calledSongs += count;
        //        offset = calledSongs - 1;
        //    }
        //    return listToReturn;
        //
        //}
        private async Task RemoveAllSongsAsync()
        {
            for (int i = 0; i < (Tempo.AllTempos.Count - 1); i++)
            {
                int totalSongs = Tempo.AllTempos[i].Tracklist.Count();
                string playlistId = Tempo.AllTempos[i].Id;
                int calledSongs = 0;

                while (calledSongs < totalSongs)
                {
                    //Gets number of songs left to call, only allows <= 100 ids through
                    var difference = totalSongs - calledSongs;
                    var amountToCall = 0;
                    if (difference > 100)
                    {
                        amountToCall = 100;
                    }
                    else
                    {
                        amountToCall = difference;
                    }
                    // var extractedList = ExtractSpotifySongIdsAsync(TempoRange.AllTempos[i].Id);
                    var playlist = await Spotify.Playlists.Get(playlistId);
                    var snapshot = playlist.SnapshotId;
                    int spotifyPlaylistTotal = (int)playlist.Tracks.Total;
                    List<int> positions = new List<int>();
                    positions.AddRange(Enumerable.Range(0, spotifyPlaylistTotal).ToList());

                    if (positions.Count != 0)
                    {
                        //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                        try
                        {
                            await Spotify.Playlists.RemoveItems(Tempo.AllTempos[i].Id, new PlaylistRemoveItemsRequest() { Positions = positions, SnapshotId = snapshot });
                        }
                        catch (APIException e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.Response?.StatusCode);
                        }
                    }
                    calledSongs += amountToCall;
                }
            }
            Console.WriteLine("All Spotify Tempo Playlists Wiped");
        }
            
        public async Task FillSpotifyTemposAsync()
        {
            await RemoveAllSongsAsync();
            for (int i = 0; i < (Tempo.AllTempos.Count - 1); i++)
            {
                int totalSongs = Tempo.AllTempos[i].Tracklist.Count();
                int calledSongs = 0;

                while (calledSongs < totalSongs)
                {
                    //Gets number of songs left to call, only allows <= 100 ids through
                    var difference = totalSongs - calledSongs;
                    var amountToCall = 0;
                    if (difference > 100)
                    {
                        amountToCall = 100;
                    }
                    else
                    {
                        amountToCall = difference;
                    }

                    //Adds <= 100 track ids into a list of strings
                    List<string> extractedStrings = new List<string>();
                    List<DbTrack> listToExtract = Tempo.AllTempos[i].Tracklist.GetRange(calledSongs, amountToCall);
                    foreach (var track in listToExtract)
                    {
                        extractedStrings.Add(track.Uri);
                    }

                    //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                    await Spotify.Playlists.AddItems(Tempo.AllTempos[i].Id, new PlaylistAddItemsRequest(extractedStrings));
                    
                    calledSongs += amountToCall;
                }
            }
            Console.WriteLine("Spotify Tempo Playlists Filled");
        }
        
        public async Task GenPrompt()
        {
            Console.WriteLine("Generate?");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y)
            {
                await GenerateSpotifyPlaylistAsync();
                Console.ReadLine();
            }
        }

    }
}
