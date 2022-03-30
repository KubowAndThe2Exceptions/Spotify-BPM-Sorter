using System;
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
        public TempoRange LowRange { get; set; }
        public TempoRange MidRange { get; set; }
        public TempoRange HighRange { get; set; }
        public List<DbTrack> TrackList { get; set; } = new List<DbTrack>();
        public List<DbTrack> GeneratedList { get; set; } = new List<DbTrack>();
        public List<DbTrack> TempoProblems { get; set; } = new List<DbTrack>();
        public SpotifyClient Spotify { get; set; }

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
            string HLPlaylist = DotNetEnv.Env.GetString("HL_PLAYLIST");
            string MHPlaylist = DotNetEnv.Env.GetString("MH_PLAYLIST");
            string MLPlaylist = DotNetEnv.Env.GetString("ML_PLAYLIST");
            string LHPlaylist = DotNetEnv.Env.GetString("LH_PLAYLIST");
            string LLPlaylist = DotNetEnv.Env.GetString("LL_PLAYLIST");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

            HighRange = new TempoRange(new Tempo(HLPlaylist), new Tempo(HHPlaylist));
            MidRange = new TempoRange(new Tempo(MLPlaylist), new Tempo(MHPlaylist));
            LowRange = new TempoRange(new Tempo(LLPlaylist), new Tempo(LHPlaylist));

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
                    int index = TrackList.FindIndex(t => t.TrackId == track.Id);
                    TrackList[index].Tempo = track.Tempo;
                }
                calledSongs += amountToCall;
            }
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
        }
        
        private void SortTempos()
        {
            foreach (var track in TrackList)
            {
                if (track.Tempo < 107)
                {
                    if (track.Tempo < 84)
                    {
                        LowRange.LowerTempo.Tracklist.Add(track);
                    }
                    else
                    {
                        LowRange.HigherTempo.Tracklist.Add(track);
                    }
                }
                else if (track.Tempo < 153)
                {
                    if (track.Tempo < 130)
                    {
                        MidRange.LowerTempo.Tracklist.Add(track);
                    }
                    else
                    {
                        MidRange.HigherTempo.Tracklist.Add(track);
                    }
                }
                else
                {
                    if (track.Tempo < 176)
                    {
                        HighRange.LowerTempo.Tracklist.Add(track);
                    }
                    else
                    {
                        HighRange.HigherTempo.Tracklist.Add(track);
                    }
                }
            }
        }

        public async Task GenerateSpotifyPlaylistAsync()
        {
            Random ran = new Random();
            int pickedTrack = 0;
            int songNum = 3;
            int cleanIntervals = 0;

            for (int i = 20; i > 0; i--)
            {
                switch (songNum)
                {
                    case 6:
                        pickedTrack = ran.Next(0, (HighRange.HigherTempo.Tracklist.Count - 1));
                        GeneratedList.Add(HighRange.HigherTempo.Tracklist[pickedTrack]);
                        HighRange.HigherTempo.Tracklist.RemoveAt(pickedTrack);
                        songNum = 3;
                        cleanIntervals = 0;
                        break;

                    case 5:
                        pickedTrack = ran.Next(0, (HighRange.LowerTempo.Tracklist.Count - 1));
                        GeneratedList.Add(HighRange.LowerTempo.Tracklist[pickedTrack]);
                        HighRange.LowerTempo.Tracklist.RemoveAt(pickedTrack);
                        songNum = 3;
                        cleanIntervals++;
                        break;

                    case 4:
                        pickedTrack = ran.Next(0, (MidRange.HigherTempo.Tracklist.Count - 1));
                        GeneratedList.Add(MidRange.HigherTempo.Tracklist[pickedTrack]);
                        MidRange.HigherTempo.Tracklist.RemoveAt(pickedTrack);
                        if (cleanIntervals == 2)
                        {
                            songNum = 6;
                        }
                        else
                        {
                            songNum = 5;
                        }
                        break;

                    case 3:
                        pickedTrack = ran.Next(0, (MidRange.LowerTempo.Tracklist.Count - 1));
                        GeneratedList.Add(MidRange.LowerTempo.Tracklist[pickedTrack]);
                        MidRange.LowerTempo.Tracklist.RemoveAt(pickedTrack);
                        if (cleanIntervals == 2)
                        {
                            songNum = 1;
                        }
                        else
                        {
                            songNum = 2;
                        }
                        break;

                    case 2:
                        pickedTrack = ran.Next(0, (LowRange.HigherTempo.Tracklist.Count - 1));
                        GeneratedList.Add(LowRange.HigherTempo.Tracklist[pickedTrack]);
                        LowRange.HigherTempo.Tracklist.RemoveAt(pickedTrack);
                        songNum = 4;
                        cleanIntervals++;
                        break;

                    case 1:
                        pickedTrack = ran.Next(0, (LowRange.LowerTempo.Tracklist.Count - 1));
                        GeneratedList.Add(LowRange.LowerTempo.Tracklist[pickedTrack]);
                        LowRange.LowerTempo.Tracklist.RemoveAt(pickedTrack);
                        songNum = 4;
                        cleanIntervals = 0;
                        break;
                }
            }

            var genPlaylist = await Spotify.Playlists.Create(CurrentUserId, new PlaylistCreateRequest(DateTime.Today.Date.ToString("d") + " GPlaylist"));
            await AddGeneratedSongs(genPlaylist.Id);
            Console.WriteLine("Finished Generating");
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
            for (int i = 0; i < 6; i++)
            {
                int totalSongs = TempoRange.AllTempos[i].Tracklist.Count();
                string playlistId = TempoRange.AllTempos[i].Id;
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
                    //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                    try
                    {
                        await Spotify.Playlists.RemoveItems(TempoRange.AllTempos[i].Id, new PlaylistRemoveItemsRequest() { Positions=positions, SnapshotId=snapshot });
                    }catch (APIException e) 
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.Response?.StatusCode);
                    }

                    calledSongs += amountToCall;
                }
            }
        }

        public async Task FillSpotifyTemposAsync()
        {
            await RemoveAllSongsAsync();
            for (int i = 0; i < 6; i++)
            {
                int totalSongs = TempoRange.AllTempos[i].Tracklist.Count();
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
                    List<DbTrack> listToExtract = TempoRange.AllTempos[i].Tracklist.GetRange(calledSongs, amountToCall);
                    foreach (var track in listToExtract)
                    {
                        extractedStrings.Add(track.Uri);
                    }

                    //Calls list of track ids, seeks and replaces each individual track with proper tempo field
                    await Spotify.Playlists.AddItems(TempoRange.AllTempos[i].Id, new PlaylistAddItemsRequest(extractedStrings));
                    
                    calledSongs += amountToCall;
                }
            }
        } 

    }
}
