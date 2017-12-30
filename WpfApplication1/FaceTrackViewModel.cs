using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
using PixelFormat = System.Windows.Media.PixelFormat;

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

        private BitmapSource _postProcessedFrame;
        public BitmapSource PostProcessedFrame
        {
            get { return _postProcessedFrame; }
            set
            {
                _postProcessedFrame = value;
                OnPropertyChanged();
            }
        }

        private Capture _capture;

        public FaceTrackViewModel()
        {
            _capture = new Capture();

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            ScaleFactor = 1.0;

            _capture.Start();
            
            System.Windows.Forms.Application.Idle += timer_Tick;
        }


        public void timer_Tick(object a, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var frame = _capture.QueryFrame().ToImage<Bgr, byte>(); 
            var tmp = frame.Resize(ScaleFactor,Emgu.CV.CvEnum.Inter.Area);

            var grayFrame = MakeGrayscale3(tmp.Bitmap);

            Rectangle[] faces = {};
            if (FaceDetectionEnabled)
                 faces = _cascadeFaceClassifier.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty);

            foreach (var face in faces)
            {
                frame.Draw(new Rectangle((int)(face.X / ScaleFactor ), 
                                         (int)(face.Y / ScaleFactor), 
                                         (int)(face.Width / ScaleFactor), 
                                         (int)(face.Height /ScaleFactor)),
                                         new Bgr(Color.BurlyWood), 3);
            }

            Rectangle[] eyes = {};
            if (EyeDetectionEnabled)
                eyes = _cascadeEyeClassifier.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty);

            foreach (var eye in eyes)
            {
                frame.Draw(new Rectangle((int)( eye.X / ScaleFactor),
                                        (int)(eye.Y / ScaleFactor),
                                        (int)(eye.Width / ScaleFactor),
                                        (int)(eye.Height / ScaleFactor)), 
                                        new Bgr(Color.Blue), 3);
            }
            
            ImageFrame = Convert(frame.ToBitmap());
            
            PostProcessedFrame = Convert(grayFrame.ToBitmap());

            stopwatch.Stop();

            FrameGenerationTime = stopwatch.ElapsedMilliseconds;
        }

        public static Image<Bgr, byte> MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return new Image<Bgr, byte>(newBitmap);
        }

        public static BitmapSource Convert(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}