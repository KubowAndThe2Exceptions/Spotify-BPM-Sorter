using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using dotenv.net;
using dotenv.net.Utilities;
using System.IO;

namespace Spotify_BPM_Sorter_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static EmbedIOAuthServer _server;
        private string _clientId;
        public static PlayListMaker PlaylistMaker;
        public MainWindow()
        {
            InitializeComponent();
            
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            string filePath = projectDirectory + @"\Spotify BPM Sorter\.env";
            
            //gets Env variables
            DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: false, envFilePaths: new[] { filePath }));
            _clientId = EnvReader.GetStringValue("CLIENT_ID");
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes(120);

            _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            await _server.Start();

            _server.AuthorizationCodeReceived += async (s, response) =>
            {
                await _server.Stop();
                PKCETokenResponse token = await new OAuthClient().RequestToken(
                  new PKCETokenRequest(_clientId, response.Code, _server.BaseUri, verifier)
                );

                var spotify = new SpotifyClient(token.AccessToken);
                PlaylistMaker = await PlayListMaker.CreateAsync(spotify);
            };

            var loginRequest = new LoginRequest(new Uri("http://localhost:5000/callback"), _clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.AppRemoteControl,
                    Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistReadPrivate, Scopes.Streaming, Scopes.UgcImageUpload, Scopes.UserFollowModify,
                    Scopes.UserFollowRead, Scopes.UserLibraryModify, Scopes.UserLibraryRead, Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackPosition, Scopes.UserReadPlaybackState,
                    Scopes.UserReadPrivate, Scopes.UserReadRecentlyPlayed, Scopes.UserTopRead }
            };
            
            var uri = loginRequest.ToUri();
            BrowserUtil.Open(uri);
        }

        private void btnEditAllSongs_Click(object sender, RoutedEventArgs e)
        {
            EditAllWindow editAllWindow = new EditAllWindow();
            editAllWindow.Show();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
