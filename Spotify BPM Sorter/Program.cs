using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Spotify_BPM_Sorter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DbClass db = new DbClass();

            var services = new ServiceCollection();
            services.AddHttpClient<SpotifyClient>();
            var serviceProvider = services.BuildServiceProvider();

            var spotify = serviceProvider.GetRequiredService<SpotifyClient>();
        }
    }
}
