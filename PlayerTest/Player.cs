using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Linq;


namespace PlayerTest
{
    class Player
    {
        Folder currentFolder = new Folder();
        public Track currentTrack { get; set; }
        private float Volume { get; set; } = 0f;
        public bool Muted { get; set; } = false;
        public bool IsPlaying { get; set; } = false;
        public WaveOutEvent outputDevice;
        public AudioFileReader audioFile;
        public Player()
        {
            outputDevice = new WaveOutEvent();
            outputDevice.Volume = 0.5f;
            //outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
        }
        public void Load(string trackpath)
        {
            //Honestly, every track change should be made by Load(newTrack), but... In Xamarin release maybe
            //So, expect to get null pointers after deleting tracks from folder lol
            Stop();
            currentTrack = new Track(trackpath);
            currentFolder.ClearAllTracks();
            currentFolder.Path = Path.GetDirectoryName(trackpath);
            
            //Пройтись по всем трекам в папке
            //Записать метадаты
            //Добавить их в лист фолдера
            string[] musicFiles = Directory.GetFiles(currentFolder.Path, "*.mp3");
            foreach (string musicFile in musicFiles)
            {
                Track track = new Track(musicFile);
                currentFolder.AddTrack(track);
            }
            InitAudio();
            if (IsPlaying)
            {
                Play();
            }
        }
        public Folder GetFolder()
        {
            return this.currentFolder;
        }
        private void InitAudio()
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                //No event cause it breaks my shitty logic and creates null pointers
                //outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
            }
            if (audioFile == null)
            {
                audioFile = new AudioFileReader(currentTrack.Path);
                outputDevice.Init(audioFile);
            }
        }
        public void Play()
        {
            InitAudio();
            outputDevice.Play();
            IsPlaying = true;
        }

        //private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        private void OutputDevice_PlaybackStopped()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }
        }

        public void Stop()
        {
            OutputDevice_PlaybackStopped();
        }
        public void Pause()
        {
            outputDevice.Pause();
            IsPlaying = false;
        }
        public void NextTrack()
        {
            Stop();
            //произошел диспоуз
            var nextTrack = currentFolder.Tracks.SkipWhile(t => t.Path!=currentTrack.Path).Skip(1).FirstOrDefault();
            if (nextTrack == null)
            {
                currentTrack = currentFolder.Tracks.Last();
            }
            else
            {
                currentTrack = nextTrack;
            }
            InitAudio();
            if (IsPlaying)
            {
                Play();
            }
            
        }
        public void PreviousTrack()
        {
            Stop();
            currentFolder.Tracks.Reverse();
            var previousTrack = currentFolder.Tracks.SkipWhile(t => t.Path != currentTrack.Path).Skip(1).FirstOrDefault();
            currentFolder.Tracks.Reverse();
            if (previousTrack == null)
            {
                currentTrack = currentFolder.Tracks.First();
            }
            else
            {
                currentTrack = previousTrack;
            }
            InitAudio();
            if (IsPlaying)
            {
                Play();
            }
        }
        public void SetVolume(double d)
        {
            double vol = d;
            if (outputDevice == null)
            {
                InitAudio();
            }
            if (d == -1)
            {
                Muted = !Muted;
                if (Muted)
                {
                    this.Volume = outputDevice.Volume;
                    outputDevice.Volume = 0;
                }
                else
                {
                    outputDevice.Volume = this.Volume;
                }
            }
            else
            {

                if (vol == 0)
                {
                    Muted = true;
                }
                else
                {
                    Muted = false;
                }
                outputDevice.Volume = (float)(vol / 100f);
                this.Volume = outputDevice.Volume;
                
            }
       
            
        }
    }
}
