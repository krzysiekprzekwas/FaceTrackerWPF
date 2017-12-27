using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WebEye.Controls.Wpf;

namespace FaceTracker
{
    public partial class MainWindow
    {

        WebCameraControl webCameraControl = new WebCameraControl();
        public MainWindow()
        {
            InitializeComponent();
            InitializeComboBox();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (webCameraControl.IsCapturing)
            {
                WebCameraView.Source = Convert(webCameraControl.GetCurrentImage());
            }
        }

        private void InitializeComboBox()
        {
            ComboBox.ItemsSource = webCameraControl.GetVideoCaptureDevices();

            if (ComboBox.Items.Count > 0)
            {
                ComboBox.SelectedItem = ComboBox.Items[0];
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartButton.IsEnabled = e.AddedItems.Count > 0;
        }

        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            var cameraId = (WebCameraId)ComboBox.SelectedItem;
            webCameraControl.StartCapture(cameraId);

            StopButton.IsEnabled = true;
            ImageButton.IsEnabled = true;
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            webCameraControl.StopCapture();
        }

        private void OnImageButtonClick(object sender, RoutedEventArgs e)
        {
            WebCameraView.Source = Convert(webCameraControl.GetCurrentImage());
        }

        public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}
