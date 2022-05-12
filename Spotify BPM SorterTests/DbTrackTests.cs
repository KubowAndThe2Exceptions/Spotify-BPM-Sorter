using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spotify_BPM_Sorter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter.Tests
{
    [TestClass()]
    public class DbTrackTests
    {
        [TestMethod()]
        public void ToString_ReturnsString()
        {
            //Arrange
            bool isSuccess = false;
            float tempo = 120;
            string name = "name";
            string artists = "artists";
            string albumname = "albumname";
            string trackid = "id";
            DbTrack track = new DbTrack(name, trackid);
            track.AlbumName = albumname;
            track.Tempo = tempo;
            track.Artists = artists;

            //Act
            string response = track.ToString();

            //Assert
            if (response is string)
            {
                isSuccess = true;
            }
            else
            {
                isSuccess = false;
            }
            Assert.IsTrue(isSuccess);
        }
        [TestMethod()]
        public void ToString_ContainsCorrectInfo()
        {
            //Arrange
            bool isSuccess = false;
            float tempo = 120;
            string name = "name";
            string artists = "artists";
            string albumname = "albumname";
            string trackid = "id";
            DbTrack track = new DbTrack(name, trackid);
            track.AlbumName = albumname;
            track.Tempo = tempo;
            track.Artists = artists;
            
            //Act
            string response = track.ToString();

            //Assert
            if (response.Contains(track.Name) && response.Contains(track.Artists) &&
                response.Contains(track.AlbumName) && response.Contains(tempo.ToString("G7")))
            {
                isSuccess = true;
            }
            else
            {
                isSuccess = false;
            }
            Assert.IsTrue(isSuccess);
        }

    }
}