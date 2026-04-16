using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        private bool shuffleEnabled = false;
        private bool isPlaying = false;
        private string favoritesPath = @"../../../favorites.json";
        private string historyPath = @"../../../history.json";

        public MainWindow()
        {
            InitializeComponent();

            //Timer updates seek bar every 100 milliseconds           
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;//Setting up event via code

            //For MediaElement 
            Player.MediaOpened += Player_MediaOpened;
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaFailed += Player_MediaFailed;
            
            SeekBar.ValueChanged += SeekBar_ValueChanged;
            SeekBar.PreviewMouseDown += SeekBar_PreviewMouseDown;
            SeekBar.PreviewMouseUp += SeekBar_PreviewMouseUp;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //to listen for keyboard input
            this.Focus();
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

            //shuffle if button is checked
            if (shuffleEnabled)
            {
                PlayRandomItem();
                return;
            }


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
                isPlaying = true;
                UpdateHistory(mi);
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //error handling - unable to play to file
            System.Windows.MessageBox.Show("Unable to play this file.", "Playback Error", MessageBoxButton.OK);
        }

        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            Rewind();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            Player.Play();
            isPlaying = true;
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
            isPlaying = false;
        }

        private void btnFastForward_Click(object sender, RoutedEventArgs e)
        {
            FastForward();
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

            //if the user clicks the Thumb specifically
            //then drag the slider. Normal behaviour
            DependencyObject sneed = e.OriginalSource as DependencyObject;
            var thumb = FindAncestor<System.Windows.Controls.Primitives.Thumb>(sneed);
            if (thumb != null)
                return;


            //if they click any part of the track, override behaviour and jump to that point
            //same logic as volume slider
            Slider slider = sender as Slider;

            //get clicked "target" for the slider
            System.Windows.Point clickPoint = e.GetPosition(slider);

            //only care about the X coordinate. The horizontal.
            //get its position as a fraction of the overall seekbar and set to that value
            double ratio = clickPoint.X / slider.ActualWidth;
            double newValue = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
            slider.Value = newValue;

            //set player position and playback
            Player.Position = TimeSpan.FromSeconds(newValue);
            Player.Play();

            //override default behaviours
            e.Handled = true;

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
                MediaItem item = new MediaItem
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
            this.Focus();//return focus to the app window

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

        private void btnShuffle_Checked(object sender, RoutedEventArgs e)
        {
            shuffleEnabled = true;
        }

        private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            shuffleEnabled = false;
        }

        private void PlayRandomItem()
        {
            if (playlist.Count <= 1)
                return;

            Random rand = new Random();
            int nextIndex;

            do
            {
                nextIndex = rand.Next(0, playlist.Count);
            }
            while (nextIndex == currentIndex); //do not repeat the same media

            currentIndex = nextIndex;
            PlaylistListBox.SelectedIndex = nextIndex; //begin playback
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //volume control
            Player.Volume = VolumeSlider.Value;
        }

        private void VolumeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //if the user clicks the Thumb specifically
            //then drag the slider. Normal behaviour
            DependencyObject sneed = e.OriginalSource as DependencyObject;
            var thumb = FindAncestor<System.Windows.Controls.Primitives.Thumb>(sneed);
            if (thumb != null)
                return;

            //if they click any part of the track, override behaviour and jump to that point
            //set the volume with a single click
            Slider slider = sender as Slider;

            //get clicked "target" for the slider
            System.Windows.Point clickPoint = e.GetPosition(slider);

            //only care about the X coordinate. The horizontal.
            //get its position as a fraction of the overall volume and set to that value
            double ratio = clickPoint.X / slider.ActualWidth;
            double newValue = slider.Minimum + (ratio * (slider.Maximum - slider.Minimum));
            slider.Value = newValue;

            //override default behaviours
            e.Handled = true;

        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            //clicking the thumb of the slider is not straightforward due to other elements (grids, borders)
            //need to check the 'visual tree' of elements and get the parent source, the thumb
            while (current != null)
            {
                //if current is the Thumb, return it
                //else keep getting the parent until we find it
                if (current is T)
                    return (T)current;

                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //why does previewkeydown work regardless of focus?
            //but keydown doesn't
            if (e.Key == Key.Space)
            {
                //press spacebar to play/pause
                TogglePause();
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                //press 'esc' key to terminate
                System.Windows.Application.Current.Shutdown();
                e.Handled = true;
            }

            if (e.Key == Key.Right)
            {
                FastForward();
                e.Handled = true;
                return;
            }

            // Left Arrow → rewind 5 seconds
            if (e.Key == Key.Left)
            {
                Rewind();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Down)
            {
                PlayNextMedia();
                e.Handled = true;
                return;
            }

            // Up Arrow → previous video
            if (e.Key == Key.Up)
            {
                PlayPreviousMedia();
                e.Handled = true;
                return;
            }

        }

        private void PlayPreviousMedia()
        {
            //if we're at the start of the playlist, do nothing
            if (currentIndex<=0)
                return;

            //otherwise, decrement the index and playback
            currentIndex--;
            PlaylistListBox.SelectedIndex = currentIndex;
            PlaySelectedMedia();
        }

        private void PlayNextMedia()
        {
            //if we're at the end of the playlist, do nothing
            if (currentIndex >= playlist.Count)
                return;

            //otherwise, increment the index and playback
            currentIndex++;
            PlaylistListBox.SelectedIndex = currentIndex;
            PlaySelectedMedia();
        }

        private void FastForward()
        {
            //get current timestamp and forward 3000 milliseconds
            Player.Position += TimeSpan.FromSeconds(3);
            isPlaying = true;
        }

        private void Rewind()
        {
            //get current timestamp and rewind 3000 milliseconds
            Player.Position -= TimeSpan.FromSeconds(3);
            isPlaying = true;
        }

        private void TogglePause()
        {
            if (isPlaying)
            {
                Player.Pause();
                isPlaying = false;
            }
            else
            {
                Player.Play();
                isPlaying = true;
            }
        }

        private void btnSaveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex < 0 || currentIndex >= playlist.Count)
            {
                System.Windows.MessageBox.Show("No media is currently loaded");
                return;
            }

            //Write MediaItem info to a JSON file
            MediaItem im = playlist[currentIndex];
            List<MediaItem>? favorites = new List<MediaItem>();

            //load file if it exists
            if (File.Exists(favoritesPath))
            {
                string json = File.ReadAllText(favoritesPath);
                favorites = JsonSerializer.Deserialize<List<MediaItem>>(json);
            }

            //check for duplicates
            if (!favorites.Any(f => f.Title == im.Title))
            {
                //It's a new addition to favorites
                favorites.Add(im);
            }
            else
            {
                System.Windows.MessageBox.Show("File is already favorited", "Duplicate", MessageBoxButton.OK);
                return;
            }

            //write to JSON file
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true//makes it readable with line breaks and indentation
            };

            string output = JsonSerializer.Serialize(favorites, options);
            File.WriteAllText(favoritesPath, output);
            System.Windows.MessageBox.Show("Added to favorites", "Success", MessageBoxButton.OK);
        }

        private void btnLoadFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(favoritesPath))
            {
                System.Windows.MessageBox.Show("File does not exist","Error",MessageBoxButton.OK);
                return;
            }

            //Load favorites
            string json = File.ReadAllText(favoritesPath);
            List<MediaItem> favorites = JsonSerializer.Deserialize<List<MediaItem>>(json);

            if (favorites.Count == 0)
            {
                System.Windows.MessageBox.Show("No favorites to load","Error",MessageBoxButton.OK);
                return;
            }

            playlist.Clear();
            playlist.AddRange(favorites);//safer than using '='?
            RefreshSource();

            //trigger playback
            currentIndex = 0;
            PlaylistListBox.SelectedIndex = 0;

        }
        private void UpdateHistory(MediaItem mi)
        {
            //Log viewing history to a JSON file
            List<HistoryItem>? historyList = new List<HistoryItem>();

            //Load file if it exists
            if (File.Exists(historyPath))
            {
                string json = File.ReadAllText(historyPath);
                historyList = JsonSerializer.Deserialize<List<HistoryItem>>(json);
            }
            
            //add current mediaItem plus time
            historyList.Add( new HistoryItem()
            {
                Title = mi.Title,
                FilePath = mi.FilePath,
                TimeStamp = DateTime.Now
            });           

            //write to JSON file
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true//makes it readable with line breaks and indentation
            };

            string output = JsonSerializer.Serialize(historyList, options);
            File.WriteAllText(historyPath, output);
            Debug.WriteLine("history.json updated");
        }
    }

    public class MediaItem
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
    }
    public class HistoryItem
    {
        public string? Title { get; set; }
        public string? FilePath { get; set; }
        public DateTime? TimeStamp { get; set; }

        public HistoryItem() { }
    }
}