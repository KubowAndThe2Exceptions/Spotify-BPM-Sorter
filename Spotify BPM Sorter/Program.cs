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
        public static DbCon Db = new DbCon();
        public static List<DbTrack> TrackList = new List<DbTrack>();
        public static List<DbTrack> TempoProblemList = new List<DbTrack>();
        public static PlayListMaker PlaylistMaker;
        

        static async Task Main(string[] args)
        {
            //Env variables crossed over
            DotNetEnv.Env.Load(@"C:\Users\Amazingg\source\repos\Spotify BPM Sorter\Spotify BPM Sorter\.env");
            ClientId = DotNetEnv.Env.GetString("CLIENT_ID");
            Secret = DotNetEnv.Env.GetString("CLIENT_SECRET");


            await Start();

            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y)
            {
                await PlaylistMaker.GenerateSpotifyPlaylistAsync();
                Console.ReadLine();
            }
            Console.ReadKey();            
        }

        private static async Task Start()
        {
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
        }


        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();
            Console.WriteLine("Authorized. . .");
            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                ClientId, Secret, response.Code, new Uri("http://localhost:5000/callback")
              )
            );
            
            var spotify = new SpotifyClient(tokenResponse.AccessToken);
            
            PlaylistMaker = await PlayListMaker.CreateAsync(spotify);

            Console.WriteLine("PlaylistMaker Initialized");
            await PlaylistMaker.FillSpotifyTemposAsync();
            await Task.Run(() => Console.WriteLine("Generate?"));
            
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }
    }
}
