using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.IO;

namespace Spotify_BPM_Sorter
{
    class PlayListMaker
    {
        public string TargetPlaylist { get; set; }
        private string CurrentUserId { get; set; }
        public TempoRange LowTempoList { get; set; }
        public TempoRange MidTempoList { get; set; }
        public TempoRange HighTempoList { get; set; }
        private static Generator Gen { get; set; }
        public List<DbTrack> TrackList { get; set; } = new List<DbTrack>();
        public List<DbTrack> NewSongs { get; set; } = new List<DbTrack>();
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
            string LLPlaylist = DotNetEnv.Env.GetString("LL_PLAYLIST");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

            HighTempoList = new TempoRange(HHPlaylist);
            MidTempoList = new TempoRange(MHPlaylist);
            LowTempoList = new TempoRange(LLPlaylist);

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

                //Calls api and converts from FullTrack to DbTrack
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
            //Completes DbTrack class
            await AddTrackAnalysisInfoAsync();
            
            //Detects problems with tempo and then sorts to complete tracklist
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

                //Calls list of track ids
                var trackFeatures = await Spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                foreach (var track in trackFeatures.AudioFeatures)
                {
                    //References DB for tempo by default.  If the tempo is not set in the DB
                    //or if the track does not exist in the DB, then it uses spotify's provided tempo
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
                        NewSongs.Add(TrackList[index]);
                        TrackList[index].Display();
                        DataBaseContext.StoreTrack(TrackList[index]);
                    }
                }
                calledSongs += amountToCall;
            }
            TxtMaker.CreateNewSongsTxt(NewSongs);
            Console.WriteLine("Track Analysis Finished, Tempos Acquired.");
        }

        private void DetectTempoProblems()
        {
            //Seeks out unset tempos and removes them from tracklist
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
            //Sorts dbtracks into tempo.tracklists by their tempo value
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
            //Calls generator to generate playlist, then displays it
            Gen = new Generator(HighTempoList, MidTempoList, LowTempoList);
            GeneratedList = await Task.Run(() => Gen.NewGenPlaylist());
            await DisplayGenListAsync();
        }
        private async Task DisplayGenListAsync()
        {
            //Displays generated playlist to user, then prompts to
            //either upload to spotify or try again
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

            //Converts to txt file
            TxtMaker.CreateGeneratedPlaylistTxt(GeneratedList);
            Console.WriteLine("Generated playlist saved as txt file");

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

                //Adds tracks to spotify playlists
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
            for (int i = 0; i < (TempoRange.AllTempos.Count); i++)
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
                    //Obtains list of positions to remove in playlists, then calls to remove them
                    var playlist = await Spotify.Playlists.Get(playlistId);
                    var snapshot = playlist.SnapshotId;
                    int spotifyPlaylistTotal = (int)playlist.Tracks.Total;
                    List<int> positions = new List<int>();
                    positions.AddRange(Enumerable.Range(0, spotifyPlaylistTotal).ToList());

                    //If the playlist isnt already empty, call to remove tracks from playlist
                    if (positions.Count != 0)
                    {
                        try
                        {
                            await Spotify.Playlists.RemoveItems(TempoRange.AllTempos[i].Id, new PlaylistRemoveItemsRequest() { Positions = positions, SnapshotId = snapshot });
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
            for (int i = 0; i < (TempoRange.AllTempos.Count); i++)
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

                    //Adds <= 100 track uris into a list of strings
                    List<string> extractedStrings = new List<string>();
                    List<DbTrack> listToExtract = TempoRange.AllTempos[i].Tracklist.GetRange(calledSongs, amountToCall);
                    foreach (var track in listToExtract)
                    {
                        extractedStrings.Add(track.Uri);
                    }

                    //Adds list of tracks to tempo playlist
                    await Spotify.Playlists.AddItems(TempoRange.AllTempos[i].Id, new PlaylistAddItemsRequest(extractedStrings));
                    
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
