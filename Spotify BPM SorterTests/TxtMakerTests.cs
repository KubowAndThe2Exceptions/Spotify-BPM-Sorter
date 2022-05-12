using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spotify_BPM_Sorter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_BPM_Sorter.Tests
{
    [TestClass()]
    public class TxtMakerTests
    {
        [TestMethod()]
        public void CreateGeneratedPlaylistTxtTest()
        {
            //Arrange
            string location = @"C:\Users\Amazingg\Desktop\Generated Playlists\New Songs\";
            int fileCount = Directory.GetFiles(location, "*", SearchOption.TopDirectoryOnly).Length;
            string id = "someid";
            DbTrack track = new DbTrack(id);
            List<DbTrack> list = new List<DbTrack> { track };
            
            //Act
            TxtMaker.CreateGeneratedPlaylistTxt(list);
            int fileCountAfterCall = Directory.GetFiles(location, "*", SearchOption.TopDirectoryOnly).Length;
            //get snapshot of new total as int
            //if int greater,
                //check if has content
                    //if contains track, pass test
                    //else, fail
                //delete new file using some derivative of Directory
            //else fail test, dont delete any files

            //Assert
            Assert.Fail();
        }

        [TestMethod()]
        public void CreateNewSongsTxtTest()
        {
            Assert.Fail();
        }
    }
}