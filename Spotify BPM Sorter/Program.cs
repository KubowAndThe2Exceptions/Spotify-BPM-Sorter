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
        public static DbClass Db = new DbClass();
        public static List<DbTrack> TrackList = new List<DbTrack>();
        public static List<DbTrack> TempoProblemList = new List<DbTrack>();

        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            ClientId = DotNetEnv.Env.GetString("CLIENT_ID");
            Secret = DotNetEnv.Env.GetString("CLIENT_SECRET");
            TargetPlaylist = DotNetEnv.Env.GetString("TARGET_PLAYLIST");

            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

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
            //NOTE FOR LATER: 
            //First, call playlist, only the total num of tracks
            //while called tracks < total num of tracks:
            //  Call tracks, place name/id in list (new class/task to handle this procedure, and new class for representing)
            //  if called tracks are sub maximum, call with offset, add offset to total called
            var spotify = new SpotifyClient(tokenResponse.AccessToken);
            
            //new playlistrequest only asking for the total num of songs
            var playlistGIR = new PlaylistGetItemsRequest();
            playlistGIR.Fields.Add("total");

            //calls playlist and extracts total num of songs
            var playlistTotalSongData = await spotify.Playlists.GetItems(TargetPlaylist, playlistGIR);
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

                TrackList.AddRange(list);
                //NOTE: clean up these variables
                var count = (int)playlist.Items.Count;
                calledSongs += count;
                offset += count - 1;
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
            foreach (var Problem in TempoProblemList)
            {
                int index = TrackList.FindIndex(t => t == Problem);
                TrackList.RemoveAt(index);
            }

            TrackList.Sort((x, y) => x.Tempo.CompareTo(y.Tempo));

            Console.WriteLine("low tempo: {0}", TrackList[0].Tempo);
            Console.WriteLine("high tempo: {0}", TrackList[TrackList.Count - 1].Tempo);
            Console.WriteLine("problem tempos: {0}", TempoProblemList.Count);
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }
    }
}
