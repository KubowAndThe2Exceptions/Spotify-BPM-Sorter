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
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            string filePath = projectDirectory + @"\Spotify BPM Sorter\.env";

            //gets Env variables
            DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: false, envFilePaths: new[] { filePath }));
            var clientId = EnvReader.GetStringValue("CLIENT_ID");

            var (verifier, challenge) = PKCEUtil.GenerateCodes(120);

            var loginRequest = new LoginRequest(new Uri("http://localhost:5000/callback"), clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative }
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
