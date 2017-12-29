using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Color = System.Drawing.Color;

namespace FaceTracker
{
    public class FaceTrackViewModel : INotifyPropertyChanged
    {

        private double _scaleFactor;
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                _scaleFactor = value;
                OnPropertyChanged();
            }
        }


        private float _frameGenerationTime;
        public float FrameGenerationTime
        {
            get { return _frameGenerationTime; }
            set
            {
                _frameGenerationTime = value;
                OnPropertyChanged();
            }
        }

        private BitmapSource _imageFrame = new BitmapImage();
        public BitmapSource ImageFrame
        {
            get { return _imageFrame; }
            set
            {
                _imageFrame = value;
                OnPropertyChanged();
            }
        }

        private bool _faceDetectionEnabled;
        public bool FaceDetectionEnabled
        {
            get { return _faceDetectionEnabled; }
            set
            {
                _faceDetectionEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _eyeDetectionEnabled;
        public bool EyeDetectionEnabled
        {
            get { return _eyeDetectionEnabled; }
            set
            {
                _eyeDetectionEnabled = value;
                OnPropertyChanged();
            }
        }

        private CascadeClassifier _cascadeFaceClassifier;
        private CascadeClassifier _cascadeEyeClassifier;
        private Image<Gray, byte> _grayFrame;
        private Capture _capture;

        public FaceTrackViewModel()
        {
            _capture = new Capture();

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            ScaleFactor = 1.0;

            System.Windows.Forms.Application.Idle += timer_Tick;
        }


        public void timer_Tick(object a, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var tmp = _capture.QueryFrame().ToImage<Bgr, byte>().Resize(ScaleFactor,Emgu.CV.CvEnum.Inter.Area);

            _grayFrame = tmp.Convert<Gray, byte>();

            Rectangle[] faces = {};
            if (FaceDetectionEnabled)
                 faces = _cascadeFaceClassifier.DetectMultiScale(_grayFrame, 1.1, 10, System.Drawing.Size.Empty);

            foreach (var face in faces)
            {
                tmp.Draw(face, new Bgr(Color.BurlyWood), 3);
            }

            Rectangle[] eyes = {};
            if (EyeDetectionEnabled)
                eyes = _cascadeEyeClassifier.DetectMultiScale(_grayFrame, 1.1, 10, System.Drawing.Size.Empty);

            foreach (var eye in eyes)
            {
                tmp.Draw(eye, new Bgr(Color.Blue), 3);
            }

            var tmpBmp = tmp.ToBitmap();
            ImageFrame = Convert(tmpBmp);

            stopwatch.Stop();

            FrameGenerationTime = stopwatch.ElapsedMilliseconds;
        }

        public static BitmapSource Convert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}