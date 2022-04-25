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
        public string Artists {get; set; }
        public string AlbumName { get; set; }

        public DbTrack(string name, string trackId, string uri, int durationMs, List<SimpleArtist> artists, string albumName)
        {
            List<string> artistlist = new List<string>();
            foreach (var artist in artists)
            {
                artistlist.Add(artist.Name);
            }

            Name = name;
            TrackId = trackId;
            Uri = uri;
            DurationMs = durationMs;
            AlbumName = albumName;
            if (artistlist.Count > 1)
            {
                Artists = string.Join(", ", artistlist);
            }
            else
            {
                Artists = artistlist[0];
            }
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

        public override string ToString()
        {
            string convertedString = 
                "Name: " + this.Name +
                " | Tempo: " + this.Tempo + " " +
                " | Artist(s): " + this.Artists +
                " | Album: " + this.AlbumName +
                "\r";
            return convertedString;
        }
        public void Display()
        {
            Console.WriteLine("Name: {0} | Tempo: {1} | Artist(s): {2} | Album: {3}", this.Name, this.Tempo, this.Artists, this.AlbumName);
        }
    }
}
