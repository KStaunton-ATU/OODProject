using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OODProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //might need some kind of stop watch???
        //globalvariables for timer and seekbar
        private bool isDraggingSlider = false;
        private DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            // Timer updates seek bar every 500 milliseconds           
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;//Setting up event via code

            // MediaElement events
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;

            // Seek bar events
            SeekBar.ValueChanged += SeekBar_ValueChanged;
            SeekBar.PreviewMouseDown += SeekBar_PreviewMouseDown;
            SeekBar.PreviewMouseUp += SeekBar_PreviewMouseUp;

            // Playlist event
            PlaylistListBox.SelectionChanged += PlaylistListBox_SelectionChanged;//Setting up event via code
        }


        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            //file is loaded
            //configure the seekbar
            //Start timer
            if (Player.NaturalDuration.HasTimeSpan)
            {
                SeekBar.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
                timer.Start();
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            //Playback ended
            //Play next media file in playlist
            //or loop?
            // Auto-play next item in playlist
            if (PlaylistListBox.SelectedIndex < PlaylistListBox.Items.Count - 1)
            {
                PlaylistListBox.SelectedIndex++;
            }
            else
            {
                Player.Stop();
                SeekBar.Value = 0;
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //error handling
            //unable to play to file
            MessageBox.Show("Unable to play this file.", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);

        }

        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            //some code
            //get current timestamp and rewind 5000 milliseconds
            Player.Position -= TimeSpan.FromSeconds(5);
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            //some code
            Player.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            //some code
            Player.Pause();
        }

        private void btnFastForward_Click(object sender, RoutedEventArgs e)
        {
            //some code
            Player.Position += TimeSpan.FromSeconds(5);
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Update textbox with timestamp
            //Constantly executing
            //Jump to a certain point in the video if user gives input
            if (!isDraggingSlider)
            {
                Player.Position = TimeSpan.FromSeconds(SeekBar.Value);
            }

            Timestamp.Text = TimeSpan.FromSeconds(SeekBar.Value).ToString(@"m\:ss");
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //stop updating textbox
            isDraggingSlider = true;
        }

        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Restart video at new timestamp
            //textbox resumes updating
            isDraggingSlider = false;
            Player.Position = TimeSpan.FromSeconds(SeekBar.Value);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isDraggingSlider && Player.Source != null && Player.NaturalDuration.HasTimeSpan)
            {
                SeekBar.Value = Player.Position.TotalSeconds;
            }
        }

        private void PlaylistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistListBox.SelectedItem is MediaItem item)
            {
                Player.Source = new Uri(item.FilePath);
                Player.Play();
            }
        }

        private void btn_openFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Media Files|*.mp4;*.mp3;*.wav;*.wmv;*.avi;*.mkv";

            if (dialog.ShowDialog() == true)
            {
                // Create a new MediaItem
                var item = new MediaItem
                {
                    Title = System.IO.Path.GetFileName(dialog.FileName),
                    FilePath = dialog.FileName
                };

                // Add to playlist
                PlaylistListBox.Items.Add(item);

                // Auto-select and play it
                PlaylistListBox.SelectedItem = item;
            }
        }
    }

    public class MediaItem
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
    }
}