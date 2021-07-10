using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PlayerTest
{
    class Folder
    {
        public string Path { get; set; }
        public List<Track> Tracks { get; set; } = new List<Track>();
        int CurrentPosition { get; set; }
        public Folder()
        {
            
        }
        public void AddTrack(Track tr)
        {
            Tracks.Add(tr);
        }
        public void ClearAllTracks()
        {
            Tracks.Clear();
        }
    }
}
