using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WebEye.Controls.Wpf;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using System.Windows.Forms.Integration;
using System.Windows.Input;


namespace FaceTracker
{
    public partial class MainWindow
    {
        private Capture _capture;

        private CascadeClassifier _cascadeFaceClassifier;
        private CascadeClassifier _cascadeEyeClassifier;


        public MainWindow()
        {
            InitializeComponent();
            _capture = new Capture();

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (FaceDetectionCheckBox.IsChecked.Value)
            {

                using (var imageFrame = _capture.QueryFrame().ToImage<Bgr, byte>())
                {
                    if (imageFrame != null)
                    {
                        var grayframe = imageFrame.Convert<Gray, byte>();
                        var faces = _cascadeFaceClassifier.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here
                        var eyes = _cascadeEyeClassifier.DetectMultiScale(grayframe, 1.1, 10, System.Drawing.Size.Empty); //the actual face detection happens here
                        foreach (var face in faces)
                        {
                            imageFrame.Draw(face, new Bgr(Color.BurlyWood), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                        }
                        foreach (var eye in eyes)
                        {
                            imageFrame.Draw(eye, new Bgr(Color.Blue), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                        }
                    }
                    WebCameraView.Image = imageFrame;                    
                }
                
            }
            else
            {
                WebCameraView.Image = _capture.QueryFrame();
            }
        }

        
    }
}
