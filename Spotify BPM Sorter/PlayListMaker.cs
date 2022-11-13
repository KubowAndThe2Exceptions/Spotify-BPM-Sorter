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
        private string _currentUserId { get; set; }
        public TempoRange LowTempoList { get; set; }
        public TempoRange MidTempoList { get; set; }
        public TempoRange HighTempoList { get; set; }
        public List<DbTrack> TrackList { get; set; } = new List<DbTrack>();
        public List<DbTrack> NewSongs { get; set; } = new List<DbTrack>();
        public List<DbTrack> GeneratedList { get; set; } = new List<DbTrack>();
        public List<DbTrack> TempoErrors { get; set; } = new List<DbTrack>();
        public SpotifyClient Spotify { get; set; }
        
        public DbCon DataBaseContext = new DbCon();

        // I'm using a async method for initialization because the initialization
        // tasks cannot be used without an async parent method.
        public static async Task<PlayListMaker> CreateAsync(SpotifyClient spotify)
        {
            var playListMaker = new PlayListMaker(spotify);
            await playListMaker.RequestUserIdAsync();
            
            // TODO: Should be moved elsewhere, does not belong in creation.
            await playListMaker.FillTrackListAsync();
            return playListMaker;
        }
        
        private PlayListMaker(SpotifyClient spotify)
        {
            //DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            //string HPlaylist = DotNetEnv.Env.GetString("H_PLAYLIST");
            //string MPlaylist = DotNetEnv.Env.GetString("M_PLAYLIST");
           // string LPlaylist = DotNetEnv.Env.GetString("L_PLAYLIST");
           // TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

           // HighTempoList = new TempoRange(HPlaylist);
            //MidTempoList = new TempoRange(MPlaylist);
           // LowTempoList = new TempoRange(LPlaylist);
            //Spotify = spotify;

        }

        private async Task<int> GetPlaylistTotalAsync(string playlistId)
        {
            //new playlistrequest only asking for the total num of songs
            var playlistGIrequest = new PlaylistGetItemsRequest();
            playlistGIrequest.Fields.Add("total");

            //calls playlist and extracts total num of songs
            var totalSongsPlaylist = await Spotify.Playlists.GetItems(playlistId, playlistGIrequest);
            int numSongsInPlaylist = (int)totalSongsPlaylist.Total;

            return numSongsInPlaylist;
        }

        private async Task RequestUserIdAsync()
        {
            var userInfo = await Spotify.UserProfile.Current();
            _currentUserId = userInfo.Id;
        }
        private async Task FillTrackListAsync()
        {
            int totalSongs = GetPlaylistTotalAsync(TargetPlaylist).Result;
            int calledSongs = 0;
            int offset = 0;
            while (calledSongs <= totalSongs)
            {
                var tracks = new List<DbTrack>();

                //Calls api and converts from FullTrack to DbTrack
                var playlist = await Spotify.Playlists.GetItems(TargetPlaylist, new PlaylistGetItemsRequest { Offset = offset });
                foreach (var item in playlist.Items)
                {
                    if (item.Track is FullTrack fullTrack)
                    {
                        DbTrack track = new DbTrack(fullTrack.Name, fullTrack.Id, fullTrack.Uri, 
                            fullTrack.DurationMs, fullTrack.Artists, fullTrack.Album.Name);
                        tracks.Add(track);
                    }
                }
                
                //Adds tracks to TrackList
                TrackList.AddRange(tracks);

                var count = (int)playlist.Items.Count;
                calledSongs += count;
                offset = calledSongs - 1;
            }
            Console.WriteLine("Track List Filled. . .");
            //Completes DbTrack class
            await AddTrackAnalysisInfoAsync();
            
            //Detects problems with tempo and then sorts to complete tracklist
            DetectTempoErrors();
            SortTempos();

        }
        
        //TODO: Method needs comments
        private async Task AddTrackAnalysisInfoAsync()
        {
            int totalSongs = TrackList.Count;
            int calledSongs = 0;
            int remainder = 0;
            int totalCalls = Math.DivRem(totalSongs, 100, out remainder);
            if (remainder > 0)
            {
                totalCalls++;
            }
            int lastCall = totalCalls - 1;
            List<DbTrack> listToExtract = new List<DbTrack>();
            List<string> extractedStrings = new List<string>();
            var AudioFeaturesArray = new TracksAudioFeaturesResponse[totalCalls];
            TracksAudioFeaturesResponse extractedAudioFeatures = new TracksAudioFeaturesResponse();

            while (calledSongs < totalSongs)
            {
                for (var i = 0; i < totalCalls; i++)
                {
                    if (i == lastCall && remainder > 0)
                    {
                        // Can be turned into method
                        listToExtract = TrackList.GetRange(calledSongs, remainder);
                        foreach (var track in listToExtract)
                        {
                            extractedStrings.Add(track.TrackId);
                        }
                        extractedAudioFeatures = await Spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                        AudioFeaturesArray[i] = extractedAudioFeatures;
                        calledSongs += remainder;
                        continue;
                    }
                    // Can be turned into method
                    listToExtract = TrackList.GetRange(calledSongs, 100);
                    foreach (var track in listToExtract)
                    {
                        extractedStrings.Add(track.TrackId);
                    }

                    extractedAudioFeatures = await Spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                    AudioFeaturesArray[i] = extractedAudioFeatures;
                    calledSongs += 100;
                }
            }

            foreach ( var audioFeaturesGroup in AudioFeaturesArray)
            {
                //TODO: Method needs comments
                UpdateTempos(audioFeaturesGroup);
            } 

            // Might still be possible vvvvvv
                // TODO: could be greatly shortened like so:
                // Get total num of songs,
                // Divide by 100 (use remainder as well),
                // Make array of TracksAudioFeaturesResponse thats as long as number of iterations needed,
                // Extract all track ids,
                // call in 100s, if remainder > 0 then last call = calling with remainder,
                // FillAudioFeaturesAsync: Use foreach to call and get audio features
                // StoreTemposAsync: Use separate method to shuffle through array and correct tempos
        }

        private void DetectTempoErrors()
        {
            //Seeks out unset tempos and removes them from tracklist
            TempoErrors = TrackList.FindAll(t => t.Tempo == 0);
            foreach (var error in TempoErrors)
            {
                int index = TrackList.FindIndex(t => t == error);
                TrackList.RemoveAt(index);
            }

            TrackList.Sort((x, y) => x.Tempo.CompareTo(y.Tempo));
            Console.WriteLine("Tempo Errors Detected");
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

        private void UpdateTempos(TracksAudioFeaturesResponse trackFeatures)
        {
            foreach (var spotifyTrack in trackFeatures.AudioFeatures)
            {
                //References DB for tempo by default.  If the tempo is not set in the DB
                //or if the track does not exist in the DB, then it uses spotify's provided tempo
                int index;
                if (DataBaseContext.TrackExists(spotifyTrack.Id))
                {
                    float tempo = DataBaseContext.GetTempo(spotifyTrack.Id);
                    if (tempo == 0 && spotifyTrack.Tempo > 0)
                    {
                        DataBaseContext.SetTempo(spotifyTrack.Tempo, spotifyTrack.Id);
                        index = TrackList.FindIndex(t => t.TrackId == spotifyTrack.Id);
                        TrackList[index].Tempo = spotifyTrack.Tempo;
                        DataBaseContext.FixArtists(TrackList[index].Artists, spotifyTrack.Id);
                    }
                    index = TrackList.FindIndex(t => t.TrackId == spotifyTrack.Id);
                    TrackList[index].Tempo = tempo;
                    DataBaseContext.FixArtists(TrackList[index].Artists, spotifyTrack.Id);
                }
                else
                {
                    index = TrackList.FindIndex(t => t.TrackId == spotifyTrack.Id);
                    TrackList[index].Tempo = spotifyTrack.Tempo;
                    NewSongs.Add(TrackList[index]);
                    TrackList[index].Display();
                    DataBaseContext.StoreTrack(TrackList[index]);
                }
            }
        }
        //private void ExtractTracks(int calledSongs, int remainder, TracksAudioFeaturesResponse audioFeaturesContainer)
        //{
        //    listToExtract = TrackList.GetRange(calledSongs, remainder);
        //    foreach (var track in listToExtract)
        //    {
        //        extractedStrings.Add(track.TrackId);
        //    }
        //}
    }
}
