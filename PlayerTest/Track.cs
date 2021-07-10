using System;
using System.Collections.Generic;
using System.Text;

namespace PlayerTest
{
    class Track
    {
        public string Path { get; set; }
        string Filename { get; set; }
        public string TrackName { get;}
        public string AlbumName { get;}
        public string ArtistName { get;}
        public TimeSpan Duration { get; set; }
        public TagLib.IPicture[] Pictures { get; set; }
        public Track()
        {

        }
        public Track(string path)
        {
            this.Path = path;
            var tfile = TagLib.File.Create(this.Path);
            this.TrackName = tfile.Tag.Title;
            this.AlbumName = tfile.Tag.Album;
            this.ArtistName = tfile.Tag.JoinedPerformers;
            this.Duration = tfile.Properties.Duration;
            this.Pictures = tfile.Tag.Pictures;
            //this.Filename
            //this.Extension
            //this.Length
        }
    }
}
