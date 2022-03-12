using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Spotify_BPM_Sorter
{
    class SpotifyClient
    {
        HttpClient HttpClient { get; }

        public SpotifyClient(HttpClient client)
        {
            HttpClient = client;
            HttpClient.BaseAddress = new Uri("");
        }
    }
}
