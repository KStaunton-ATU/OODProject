using System.Diagnostics;
using System.IO;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using WinForms = System.Windows.Forms;

namespace OODProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //global variables 
        private bool isDraggingSlider = false;
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MediaItem> playlist = new List<MediaItem>();
        private int currentIndex = -1;//no file selected at start
        private bool loopEnabled = false;

        public MainWindow()
        {
            InitializeComponent();

            //Timer updates seek bar every 500 milliseconds           
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;//Setting up event via code

            //For MediaElement 
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;
            
            SeekBar.ValueChanged += SeekBar_ValueChanged;
            SeekBar.PreviewMouseDown += SeekBar_PreviewMouseDown;
            SeekBar.PreviewMouseUp += SeekBar_PreviewMouseUp;
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
            SeekBar.Value = 0;

            //Playback ended
            //Play next media file in playlist            
            if (currentIndex < playlist.Count - 1)
            {
                PlayNextItem();               
            }
            else
            {
                //loop the playlist/single file
                if (loopEnabled && playlist.Count > 0)
                {
                    currentIndex = 0;
                    PlaylistListBox.SelectedIndex = 0;   // triggers playback
                }
                else
                {
                    Player.Stop();
                }
            }
        }

        private void PlayNextItem()
        {
            int nextVid = currentIndex + 1;

            if (nextVid < playlist.Count)
            {
                currentIndex = nextVid;
                SeekBar.Value = 0;
                PlaylistListBox.SelectedIndex = nextVid;                
            }                
        }

        private void PlaySelectedMedia()
        {
            //single source for playing a file
            //easier to track potential issues
            //will invoke mediaopened event
            if (currentIndex >= 0 && currentIndex < playlist.Count)
            {
                //set the file path and play it
                MediaItem mi = playlist[currentIndex];
                Player.Source = new Uri(mi.FilePath);
                Player.Play();
            }
        }


        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //error handling - unable to play to file
            System.Windows.MessageBox.Show("Unable to play this file.", "Playback Error", MessageBoxButton.OK);
        }

        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            //get current timestamp and rewind 5000 milliseconds
            Player.Position -= TimeSpan.FromSeconds(5);
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            Player.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
        }

        private void btnFastForward_Click(object sender, RoutedEventArgs e)
        {
            Player.Position += TimeSpan.FromSeconds(5);
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Update textbox with timestamp
            //Constantly executing
            //slider is always in sync with video
            //slider position determines point of video playback
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
            //Restart video at new slider position
            //Re-enable slider sync. Textbox resumes updating
            isDraggingSlider = false;
            Player.Position = TimeSpan.FromSeconds(SeekBar.Value);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //keep the seekbar updated in sync with the video
            if (!isDraggingSlider && Player.Source != null && Player.NaturalDuration.HasTimeSpan)
            {
                SeekBar.Value = Player.Position.TotalSeconds;
            }
        }

        private void PlaylistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistListBox.SelectedIndex >= 0)
            {
                currentIndex = PlaylistListBox.SelectedIndex;   
                PlaySelectedMedia();

                //SelectionChanged is firing twice, skipping every second video
                //2 selections, First is removing the current video, Second is assinging the new video
                Debug.WriteLine("SelectionChanged fired");
            }

        }

        private void btn_openFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Media Files|*.mp4;*.mp3;*.wav;*.wmv;*.avi;*.mkv";

            if (dialog.ShowDialog() == true)
            {
                //Simplify as MediaItem
                var item = new MediaItem
                {
                    Title = System.IO.Path.GetFileName(dialog.FileName),
                    FilePath = dialog.FileName
                };

                //Add to playlist
                playlist.Clear();
                playlist.Add(item);
                RefreshSource();

                //trigger selection changed event and start playback
                currentIndex = playlist.Count - 1;
                PlaylistListBox.SelectedIndex = currentIndex;
            }
        }

        private void btn_OpenPlaylist_Click(object sender, RoutedEventArgs e)
        {
            //using System.Windows.Forms but theres a naming conflict with WPF
            //so use WinForms alias
            var dialog = new WinForms.FolderBrowserDialog();

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string selectedFolder = dialog.SelectedPath;

                //clear playlist and search for media files
                playlist.Clear();

                string[] extensions = { ".mp4", ".mp3", ".wav", ".wmv", ".avi", ".mkv" };

                //search for media files in the selected folder
                var files = Directory.GetFiles(selectedFolder)
                                     .Where(f => extensions.Contains(System.IO.Path.GetExtension(f).ToLower()));

                //now turn them into MediaItems
                MediaItem mi;
                foreach (var file in files)
                {
                    mi = new MediaItem
                    {
                        Title = System.IO.Path.GetFileName(file),
                        FilePath = file
                    };
                    playlist.Add(mi);
                }
                RefreshSource();
                currentIndex = 0;
                PlaylistListBox.SelectedIndex = currentIndex;
            }

        }

        private void RefreshSource()
        {
            PlaylistListBox.ItemsSource = null;
            PlaylistListBox.ItemsSource = playlist;
        }

        private void btnLoop_Click(object sender, RoutedEventArgs e)
        {
            if(loopEnabled)
            {
                loopEnabled = false;
            }
            else
            {
                loopEnabled = true;
            }
        }
    }

    public class MediaItem
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
    }
}