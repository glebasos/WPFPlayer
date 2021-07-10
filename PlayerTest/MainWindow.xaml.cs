using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PlayerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Player player = new Player();
        //Folder folder = new Folder();
        //Track track = new Track();
        bool playbackState = false;
        bool isTSThumbDragged = false;
        bool isMouseOverTS = false;
        //this.Thread = new System.Threading.Thread(new System.Threading.ThreadStart(OnPlaying));
        //this.Thread.Start();
        
        public MainWindow()
        {
            InitializeComponent();
            
            UpdateTimeSeekAsync();
            //Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(OnPlaying));
            //thread.Start();
        }
        public async void UpdateTimeSeekAsync()
        {
            var progress = new Progress<double>(d => UpdateTimeSeekControls(d));
            await Task.Run(() =>InfUpdate(progress));
        }

        private void UpdateTimeSeekControls(double ms)
        {
            if (!isTSThumbDragged)
            {
                //sliderTimeSeek.Value = (ms / player.audioFile.TotalTime.TotalMilliseconds) * 100;
                sliderTimeSeek.Value = (player.audioFile.CurrentTime.TotalMilliseconds / player.audioFile.TotalTime.TotalMilliseconds) * 100;
            }
            lblTimeCurrent.Content = player.audioFile.CurrentTime.ToString(@"mm\:ss");
        
        }
        public void InfUpdate(IProgress<double> progress)
        {
            double ms = 0;
            double lastms = 0;
            while (true)
            {
                if (player.IsPlaying)
                {
                    try
                    {
                        ms = player.outputDevice.GetPosition() * 1000.0 / player.outputDevice.OutputWaveFormat.BitsPerSample / player.outputDevice.OutputWaveFormat.Channels * 8.0 / player.outputDevice.OutputWaveFormat.SampleRate;
                        lastms = ms;
                    }
                    catch (Exception)
                    {

                        ms = lastms;
                    }
                    
                    progress.Report(ms);
                    //sliderTimeSeek.Value = (ms / player.audioFile.TotalTime.TotalMilliseconds) * 100;
                    //MessageBox.Show("Testlol");
                    //Console.WriteLine("Milliseconds Played: " + ms);
                }
                Task.Delay(1000).Wait();
            }
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "MP3 Files |*.mp3" };
            // Был ли совершен щелчок на кнопке ОК?
            if (true == openDlg.ShowDialog())
            {
                // Загрузить содержимое выбранного файла.
                string trackPath = openDlg.FileName;
                // Отобразить строку в TextBox.
                LoadTrack(trackPath);
            }

        }

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void LoadTrack(string trackpath)
        {
            player.Load(trackpath);
            UpdateTrackList(player.GetFolder());
            SetAlbumCover();
            UpdateUI();
        }
        private void UpdateUI()
        {
            SetBottomLeftInfo();
            lblTimeTotal.Content = player.audioFile.TotalTime.ToString(@"mm\:ss");
        }
        private void SetBottomLeftInfo()
        {

            lblBotLeftTrackName.Content = player.currentTrack.TrackName;
            lblBotLeftArtist.Content = player.currentTrack.ArtistName;
        }
        private void SetAlbumCover()
        {
            //string pathToCoverArt = player.GetFolder().Path + @"\folder.jpg";
            try
            {
                TagLib.IPicture pic = player.currentTrack.Pictures[0];
                MemoryStream ms = new MemoryStream(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                // ImageSource for System.Windows.Controls.Image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                imgAlbumCover.Source = bitmap;
                imgLeftBotoomCoverArt.Source = bitmap;
                imgAlbumCover.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\folder.jpg", UriKind.Absolute));
                imgLeftBotoomCoverArt.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\folder.jpg", UriKind.Absolute));
            }
            catch (Exception)
            {

                try
                {
                    imgAlbumCover.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\folder.jpg", UriKind.Absolute));
                    imgLeftBotoomCoverArt.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\folder.jpg", UriKind.Absolute));
                }
                catch (Exception)
                {
                    try
                    {
                        imgAlbumCover.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\cover.jpg", UriKind.Absolute));
                        imgLeftBotoomCoverArt.Source = new BitmapImage(new Uri(player.GetFolder().Path + @"\cover.jpg", UriKind.Absolute));
                        // Create a System.Windows.Controls.Image control
                        //System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                        //img.Source = bitmap;
                    }
                    catch (Exception)
                    {
                        imgAlbumCover.Source = null;
                        imgLeftBotoomCoverArt.Source = null;
                    }
                }
            }
        }
        private void UpdateTrackList(Folder fold)
        {
            stpTrackList.Children.Clear();
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (Brush)converter.ConvertFromString("White");
            int idx = 0;
            foreach (var tr in fold.Tracks)
            {
                var l = new Label();
                l.PreviewMouseLeftButtonUp += LabelMouseLeftButtonUp;
                l.MouseEnter += LabelMouseEnterColor;
                l.MouseLeave += LabelMouseLeaveColor;
                l.Content = tr.TrackName.ToString();
                l.Foreground = brush;
                stpTrackList.Children.Add(l);
                idx++;
            }
        }

        private void LabelMouseLeaveColor(object sender, MouseEventArgs e)
        {
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (Brush)converter.ConvertFromString("Black");
            Label senderLabel = sender as Label;
            senderLabel.Background = brush;
        }

        private void LabelMouseEnterColor(object sender, MouseEventArgs e)
        {
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (Brush)converter.ConvertFromString("#FF181818");
            Label senderLabel = sender as Label;
            senderLabel.Background = brush;
        }

        private void LabelMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Выбор песни из лейблов слева
            //if (playbackState == true)
            //{
            //    StopMusic();
            //}
            //else
            //{
            //    StopMusic();
            //    ChangePlaybackState();
            //}
            player.Stop();
            Label senderLabel = sender as Label;
            player.currentTrack = player.GetFolder().Tracks.Where(t => t.TrackName == senderLabel.Content.ToString()).FirstOrDefault();
            player.Load(player.currentTrack.Path);
            UpdateUI();
            //if (playbackState == true)
            //    player.Play();
            //ChangePlaybackState();
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player.currentTrack != null){
                //ChangePlaybackState();
                if (playbackState == false)
                {
                    StartMusic();
                    //UpdateTimeSeekAsync();
                }
                else
                {
                    PauseMusic();
                }
            }
            
            
        }
        private void btnPrevious_Click(object sender, MouseEventArgs e)
        {
            if (player.currentTrack != null)
            {
                player.PreviousTrack();
                //ChangePlaybackState();
                UpdateUI();
            }
            

        }

        private void btnNext_Click(object sender, MouseEventArgs e)
        {
            if (player.currentTrack != null)
            {
                player.NextTrack();
                //ChangePlaybackState();
                UpdateUI();
            }
        }
        private void StopMusic()
        {
            player.Stop();
            ChangePlaybackState();
            
        }
        private void StartMusic()
        {
            player.Play();
            ChangePlaybackState();

        }
        private void PauseMusic()
        {
            player.Pause();
            ChangePlaybackState();

        }
        private void ChangePlaybackState()
        {
            playbackState = !playbackState;
            if (playbackState == false)
            {
                btnPlayPause.Content = "▸";
            }
            else
            {
                btnPlayPause.Content = "||";
            }
        }

        private void btnMute_Click(object sender, RoutedEventArgs e)
        {
            if (sliderVolume.Value == 0 && player.Muted)
            {
                sliderVolume.Value = 10;
                player.SetVolume(sliderVolume.Value);
                //player.Muted = false;
                btnMute.Content = "🔉";
            }
            else
            {
                player.SetVolume(-1);
                if (player.Muted)
                {
                    btnMute.Content = "🔇";
                }
                else
                {
                    btnMute.Content = "🔉";
                }
            }
            
        }

        private void btnPreviousNext_MouseEnter(object sender, MouseEventArgs e)
        {
            var converter = new System.Windows.Media.BrushConverter();
            var brushBack = (Brush)converter.ConvertFromString("#FF181818");
            var brushFront = (Brush)converter.ConvertFromString("White");
            Label senderButton = sender as Label;
            senderButton.Background = brushBack;
            senderButton.Foreground = brushFront;
        }

        private void btnPreviousNext_MouseLeave(object sender, MouseEventArgs e)
        {
            var converter = new System.Windows.Media.BrushConverter();
            //var brushBack = (Brush)converter.ConvertFromString("#FF181818");
            var brushFront = (Brush)converter.ConvertFromString("Gray");
            Label senderButton = sender as Label;
            //senderButton.Background = brushBack;
            senderButton.Foreground = brushFront;
        }

        private void sliderVolume_Dragged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            
            if (slider.Value == 0)
            {
                btnMute.Content = "🔇";
            }
            else
            {
                btnMute.Content = "🔉";
            }
            player.SetVolume(slider.Value);
        }

        private void sliderTimeSeek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isTSThumbDragged = true;
        }

        private void sliderTimeSeek_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            //player.audioFile.  (slider.Value * player.audioFile.TotalTime)/100;
            
            //sliderTimeSeek.Value = ((player.outputDevice.GetPosition() * 1000.0 / player.outputDevice.OutputWaveFormat.BitsPerSample / player.outputDevice.OutputWaveFormat.Channels * 8.0 / player.outputDevice.OutputWaveFormat.SampleRate) / player.audioFile.TotalTime.TotalMilliseconds) * 100;
            isTSThumbDragged = false;
            
        }

        private void sliderTimeSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (isTSThumbDragged || isMouseOverTS)
            {
                player.audioFile.Seek((long)(player.audioFile.WaveFormat.AverageBytesPerSecond * slider.Value * player.audioFile.TotalTime.TotalSeconds / 100), SeekOrigin.Begin);

            }
            if (player.audioFile.CurrentTime == player.audioFile.TotalTime)
            {
                player.NextTrack();
                //ChangePlaybackState();
                UpdateUI();
            }
        }

        private void sliderTimeSeek_MouseEnter(object sender, MouseEventArgs e)
        {
            isMouseOverTS = true;
        }

        private void sliderTimeSeek_MouseLeave(object sender, MouseEventArgs e)
        {
            isMouseOverTS = false;
        }

        //private void btnMute_Click(object sender, MouseButtonEventArgs e)
        //{

        //}
    }
}
