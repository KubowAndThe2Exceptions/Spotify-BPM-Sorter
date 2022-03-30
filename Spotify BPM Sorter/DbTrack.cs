using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace Spotify_BPM_Sorter
{
    class DbTrack
    {
        public string Name { get; set; }
        public string TrackId { get; set; }
        public string Uri { get; set; }
        public float Tempo { get; set; }
        public int DurationMs { get; set; }
        public List<SimpleArtist> Artists {get; set; }
        public string AlbumName { get; set; }

        public DbTrack(string name, string trackId, string uri, int durationMs, List<SimpleArtist> artists, string albumName)
        {
            Name = name;
            TrackId = trackId;
            Uri = uri;
            DurationMs = durationMs;
            Artists = artists;
            AlbumName = albumName;
        }
        public DbTrack(string name, string trackId, float tempo)
        {
            Name = name;
            TrackId = trackId;
            Tempo = tempo;
        }
        public DbTrack(string name, string trackId, string uri)
        {
            Name = name;
            TrackId = trackId;
            Uri = uri;
        }

        public DbTrack(string name, string trackId)
        {
            Name = name;
            TrackId = trackId;
        }
        public DbTrack(string trackId)
        {
            TrackId = trackId;
        }
    }
}
