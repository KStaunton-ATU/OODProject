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

namespace OODProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //might need some kind of stop watch???
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            //file is loaded
            //configure the seekbar
            //Start timer
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            //Playback ended
            //Play next media file in playlist
            //or loop?
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //error handling
            //unable to play to file
        }

        private void btnRewind_Click(object sender, RoutedEventArgs e)
        {
            //some code
            //get current timestamp and rewind 5000 milliseconds
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            //some code
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            //some code
        }

        private void btnFastForward_Click(object sender, RoutedEventArgs e)
        {
            //some code
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Update textbox with timestamp
            //Constantly executing
            //Jump to a certain point in the video if user gives input
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //stop updating textbox
        }

        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Restart video at new timestamp
            //textbox resumes updating
        }
    }
}