using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using drawing = System.Drawing;
using System.IO;

namespace ZVI
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Video video;

        public Output OutputFrameType { get; set; }

        public Method Method { get; set; }

        public bool UseOpeningFilter
        {
            get { return (bool) GetValue(UseOpeningFilterProperty); }
            set { SetValue(UseOpeningFilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for 
        //UseOpeningFilter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseOpeningFilterProperty = DependencyProperty.Register("UseOpeningFilter", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        private bool play = false;

        public MainWindow()
        {
            video = new Video(this);
            InitializeComponent();
            SizeToContent = SizeToContent.WidthAndHeight;
            OutputFrameType = Output.Source;
            Method = Method.TwoFrame;
        }

        private void LoadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "All files |*.*";
            if (fileDialog.ShowDialog() == true)
            {
                video.initVideo(fileDialog.FileName);
            }
            //this.Width = video.VideoWidth + 10;
            //this.Width = video.VideoWidth + 10;
            display.Width = video.VideoWidth;
            display.Height = video.VideoHeight;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.PlayVideoTest();
        }

        private async Task PlayVideoTest()
        {
            play = true;
            int frameCounter = 0;
            while (play)
            {
                // delete areas from previous frame
                display.Children.Clear();

                frameCounter++;
                FrameWrapper fw = video.test();
                drawing.Bitmap videoFrame = fw.Frame;
                MemoryStream memoryStream = new MemoryStream();
                videoFrame.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Position = 0;
                BitmapImage frame = new BitmapImage();
                frame.BeginInit();
                frame.StreamSource = memoryStream;
                frame.EndInit();
                ImageBrush ib = new ImageBrush();
                ib.ImageSource = frame;
                
                display.Background = ib;
                Console.WriteLine("frame " + frameCounter);
                Console.WriteLine("pixelFormat: " + videoFrame.PixelFormat);

                if (fw.MovingObjects != null)
                {
                    foreach (drawing.Rectangle dRectangle in fw.MovingObjects)
                    {
                        Rectangle rect = new Rectangle
                        {
                            Stroke = Brushes.LightBlue,
                            StrokeThickness = 2,
                            Width = dRectangle.Width,
                            Height = dRectangle.Height
                        };
                        Canvas.SetLeft(rect, dRectangle.X);
                        Canvas.SetTop(rect, dRectangle.Y);
                        display.Children.Add(rect);
                    }
                }
                await Task.Delay(40);
            }
        }

        private async Task PlayVideo()
        {
            play = true;
            int frameCounter = 0;
            int waitingTime = 1000 / video.VideoFrameRate;
            while (play)
            {
                // delete areas from previous frame
                display.Children.Clear();

                frameCounter++;
                FrameWrapper fw = video.getFrame();

                // render frame
                drawing.Bitmap videoFrame = fw.Frame;
                MemoryStream memoryStream = new MemoryStream();
                videoFrame.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Position = 0;
                BitmapImage frame = new BitmapImage();
                frame.BeginInit();
                frame.StreamSource = memoryStream;
                frame.EndInit();
                ImageBrush ib = new ImageBrush();
                ib.ImageSource = frame;
                display.Width = frame.Width;
                display.Height = frame.Height;
                display.Background = ib;
                //Console.WriteLine("frame " + frameCounter);
                //Console.WriteLine("pixelFormat: " + videoFrame.PixelFormat);

                // render rectangles
                if (fw.MovingObjects != null)
                {
                    foreach (drawing.Rectangle dRectangle in fw.MovingObjects)
                    {
                        Rectangle rect = new Rectangle
                        {
                            Stroke = Brushes.Yellow,
                            StrokeThickness = 2,
                            Width = dRectangle.Width,
                            Height = dRectangle.Height
                        };
                        Canvas.SetLeft(rect, dRectangle.X);
                        Canvas.SetTop(rect, dRectangle.Y);
                        display.Children.Add(rect);
                    }
                }

                // waiting for next frame
                await Task.Delay(waitingTime);
                //await Task.Delay(500);
            }
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            play = false;
        }

        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            if (video.VideoInitialized)
            {
                PlayVideo();
            }
            else
            {
                MessageBox.Show("You have to load video file first", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Warning); 
            }
        }

        
    }

    public class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
}
