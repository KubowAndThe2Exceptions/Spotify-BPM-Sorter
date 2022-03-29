using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Collections.Generic;

namespace Spotify_BPM_Sorter
{
    class Program
    {
        private static EmbedIOAuthServer _server;
        public static string ClientId = string.Empty;
        public static string Secret = string.Empty;
        public static string TargetPlaylist = string.Empty;
        public static string HHPlaylist = string.Empty;
        public static string HLPlaylist = string.Empty;
        public static string MHPlaylist = string.Empty;
        public static string MLPlaylist = string.Empty;
        public static string LHPlaylist = string.Empty;
        public static string LLPlaylist = string.Empty;
        public static DbClass Db = new DbClass();
        public static List<DbTrack> TrackList = new List<DbTrack>();
        public static List<DbTrack> TempoProblemList = new List<DbTrack>();
        public static TempoRange LowTempos = new TempoRange();
        public static TempoRange MidTempos = new TempoRange();
        public static TempoRange HighTempos = new TempoRange();
        

        static async Task Main(string[] args)
        {
            //Env variables crossed over
            DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            ClientId = DotNetEnv.Env.GetString("CLIENT_ID");
            Secret = DotNetEnv.Env.GetString("CLIENT_SECRET");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");
            HHPlaylist = DotNetEnv.Env.GetString("HH_PLAYLIST");
            HLPlaylist = DotNetEnv.Env.GetString("HL_PLAYLIST");
            MHPlaylist = DotNetEnv.Env.GetString("MH_PLAYLIST");
            MLPlaylist = DotNetEnv.Env.GetString("ML_PLAYLIST");
            LHPlaylist = DotNetEnv.Env.GetString("LH_PLAYLIST");
            LLPlaylist = DotNetEnv.Env.GetString("LL_PLAYLIST");


            //Creates new embedded server
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            //Calls Either task depending on response
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Code)
            {
                //populate with proper scopes
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.AppRemoteControl,
                    Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistReadPrivate, Scopes.Streaming, Scopes.UgcImageUpload, Scopes.UserFollowModify,
                    Scopes.UserFollowRead, Scopes.UserLibraryModify, Scopes.UserLibraryRead, Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackPosition, Scopes.UserReadPlaybackState,
                    Scopes.UserReadPrivate, Scopes.UserReadRecentlyPlayed, Scopes.UserTopRead }
            };

            BrowserUtil.Open(request.ToUri());
            Console.ReadKey();
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                ClientId, Secret, response.Code, new Uri("http://localhost:5000/callback")
              )
            );
            
            var spotify = new SpotifyClient(tokenResponse.AccessToken);
            
            //new playlistrequest only asking for the total num of songs
            var playlistGIR = new PlaylistGetItemsRequest();
            playlistGIR.Fields.Add("total");

            //calls playlist and extracts total num of songs
            var playlistTotalSongData = await spotify.Playlists.GetItems(TargetPlaylist, playlistGIR);
            
            //Extracts Name and Id of each song in increments of 50 and stores data in DbTrack objects
            int totalSongs = (int)playlistTotalSongData.Total;
            int calledSongs = 0;
            int offset = 0;
            while (calledSongs <= totalSongs)
            {
                var list = new List<DbTrack>();
                var playlist = await spotify.Playlists.GetItems(TargetPlaylist, new PlaylistGetItemsRequest { Offset = offset });
                foreach (var item in playlist.Items)
                {
                    if (item.Track is FullTrack fullTrack)
                    {
                        DbTrack track = new DbTrack(fullTrack.Name, fullTrack.Id);
                        list.Add(track);
                    }
                }
                //Adds tracks to TrackList
                TrackList.AddRange(list);

                var count = (int)playlist.Items.Count;
                calledSongs += count;
                offset = calledSongs - 1;
            }

            totalSongs = TrackList.Count;
            calledSongs = 0;

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
                var trackFeatures = await spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                foreach (var track in trackFeatures.AudioFeatures)
                {
                    int index = TrackList.FindIndex(t => t.TrackId == track.Id);
                    TrackList[index].Tempo = track.Tempo;
                }
                calledSongs += amountToCall;
            }
            
            TempoProblemList = TrackList.FindAll(t => t.Tempo == 0);
            foreach (var problem in TempoProblemList)
            {
                int index = TrackList.FindIndex(t => t == problem);
                TrackList.RemoveAt(index);
            }

            TrackList.Sort((x, y) => x.Tempo.CompareTo(y.Tempo));

            Console.WriteLine("low tempo: {0}", TrackList[0].Tempo);
            Console.WriteLine("high tempo: {0}", TrackList[TrackList.Count - 1].Tempo);
            Console.WriteLine("problem tempos: {0}", TempoProblemList.Count);

            foreach (var track in TrackList)
            {
                if (track.Tempo < 107)
                {
                    if (track.Tempo < 84)
                    {
                        LowTempos.LowerTempo.Add(track);
                    }
                    else
                    {
                        LowTempos.HigherTempo.Add(track);
                    }
                } 
                else if (track.Tempo < 153)
                {
                    if (track.Tempo < 130)
                    {
                        MidTempos.LowerTempo.Add(track);
                    }
                    else
                    {
                        MidTempos.HigherTempo.Add(track);
                    }
                }
                else
                {
                    if (track.Tempo < 176)
                    {
                        HighTempos.LowerTempo.Add(track);
                    }
                    else
                    {
                        HighTempos.HigherTempo.Add(track);
                    }
                }
            }
            Console.WriteLine("Low Tempo: {0}, Lower-range: {1}, Higher-range {2}", LowTempos.Total(), LowTempos.LowerTempo.Count, LowTempos.HigherTempo.Count);
            Console.WriteLine("Mid Tempo: {0} Lower-range: {1}, Higher-range {2}", MidTempos.Total(), MidTempos.LowerTempo.Count, MidTempos.HigherTempo.Count);
            Console.WriteLine("High Tempo: {0} Lower-range: {1}, Higher-range {2}", HighTempos.Total(), HighTempos.LowerTempo.Count, HighTempos.HigherTempo.Count);

            List<DbTrack> generatedList = new List<DbTrack>();
            Random ran = new Random();
            int pickedTrack = 0;
            int songNum = 3;
            int cleanIntervals = 0;
                
            for (int i = 20; i > 0; i--)
            {
                switch (songNum)
                {
                    case 6:
                        pickedTrack = ran.Next(0, (HighTempos.HigherTempo.Count - 1));
                        generatedList.Add(HighTempos.HigherTempo[pickedTrack]);
                        songNum = 3;
                        cleanIntervals = 0;
                        break;

                    case 5:
                        pickedTrack = ran.Next(0, (HighTempos.LowerTempo.Count - 1));
                        generatedList.Add(HighTempos.LowerTempo[pickedTrack]);
                        songNum = 3;
                        cleanIntervals++;
                        break;

                    case 4:
                        pickedTrack = ran.Next(0, (MidTempos.HigherTempo.Count - 1));
                        generatedList.Add(MidTempos.HigherTempo[pickedTrack]);
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
                        pickedTrack = ran.Next(0, (MidTempos.LowerTempo.Count - 1));
                        generatedList.Add(MidTempos.LowerTempo[pickedTrack]);
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
                        pickedTrack = ran.Next(0, (LowTempos.HigherTempo.Count - 1));
                        generatedList.Add(LowTempos.HigherTempo[pickedTrack]);
                        songNum = 4;
                        cleanIntervals++;
                        break;

                    case 1:
                        pickedTrack = ran.Next(0, (LowTempos.LowerTempo.Count - 1));
                        generatedList.Add(LowTempos.LowerTempo[pickedTrack]);
                        songNum = 4;
                        cleanIntervals = 0;
                        break;
                }
            }
            foreach (var track in generatedList)
            {
                Console.WriteLine("Track name: {0}, Track tempo: {1}", track.Name, track.Tempo);
            }

            //-EDIT FOR SORTING SONGS INTO PLAYLISTS-

            for (int i = 0; i < 6; i++)
            {
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
                    var trackFeatures = await spotify.Tracks.GetSeveralAudioFeatures(new TracksAudioFeaturesRequest(extractedStrings));
                    foreach (var track in trackFeatures.AudioFeatures)
                    {
                        int index = TrackList.FindIndex(t => t.TrackId == track.Id);
                        TrackList[index].Tempo = track.Tempo;
                    }
                    calledSongs += amountToCall;
                }
            }
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }
    }
}
