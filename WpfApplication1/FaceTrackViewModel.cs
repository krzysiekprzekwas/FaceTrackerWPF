﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using OpenTK.Graphics.OpenGL;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Rectangle = System.Drawing.Rectangle;

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

        private bool _histogramEqualizationEnabled; 
        public bool HistogramEqualizationEnabled
        {
            get { return _histogramEqualizationEnabled; }
            set
            {
                _histogramEqualizationEnabled = value;
                OnPropertyChanged();
            }
        }

        private readonly CascadeClassifier _cascadeFaceClassifier;
        private readonly CascadeClassifier _cascadeEyeClassifier;

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

        private readonly Capture _capture;
        private const int ROIOffset = 30;

        private Rectangle _previousFacePosition;

        public FaceTrackViewModel()
        {
            _capture = new Capture {FlipHorizontal = true};

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            ScaleFactor = 0.5;

            _capture.Start();

            System.Windows.Forms.Application.Idle += ProcessFrame;
        }

        public void ProcessFrame(object a, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            PerformFaceDetection();

            stopwatch.Stop();

            FrameGenerationTime = stopwatch.ElapsedMilliseconds;
        }

        private Image<Gray, byte> EqualizeHistogram(Emgu.CV.Image<Gray,byte> image)
        {
            // Convert a BGR image to HLS range
            var imageHsi = new Image<Hls, byte>(image.Bitmap);

            // Equalize the Intensity Channel
            var equalized = imageHsi[1];
            equalized._EqualizeHist();

            // Convert the image back to BGR range
            return equalized.Convert<Gray, byte>();
        }

        public void PerformFaceDetection()
        {
            var frame = _capture.QueryFrame().ToImage<Bgr, byte>();


            var grayFrame = frame.Resize(ScaleFactor,
                                        Emgu.CV.CvEnum.Inter.Area)
                                        .Convert<Gray, byte>();


            if (HistogramEqualizationEnabled)
                grayFrame = EqualizeHistogram(grayFrame);

            if (FaceDetectionEnabled)
            {
                var faces = _cascadeFaceClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

                var face = faces.OrderBy(x => x.Width * x.Height).FirstOrDefault();

                DrawFigure(frame, face);

                if (face.Width * face.Height > 0)
                {
                    _previousFacePosition = new Rectangle((int)(face.X / ScaleFactor - ROIOffset),
                        (int)(face.Y / ScaleFactor - ROIOffset),
                        (int)(face.Width / ScaleFactor + ROIOffset * 2),
                        (int)(face.Height / ScaleFactor + ROIOffset * 2));
                }

                frame.Draw(_previousFacePosition, new Bgr(Color.Chartreuse), 3);
            }

            if (EyeDetectionEnabled)
            {
                var eyes = _cascadeEyeClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

                var twoEyes = eyes.OrderBy(x => x.Width * x.Height).Take(2).ToList();

                foreach (var eye in twoEyes)
                {
                    DrawFigure(frame, eye);
                }

            }

            var editableGrayFrame = MarkAngles(grayFrame, 22.5);

            ImageFrame = Convert(frame.ToBitmap());

            PostProcessedFrame = Convert(editableGrayFrame.ToBitmap());
        }

        private void DrawFigure(Image<Bgr, byte> frame, Rectangle figure)
        {
            frame.Draw(new Rectangle((int)(figure.X / ScaleFactor),
                    (int)(figure.Y / ScaleFactor),
                    (int)(figure.Width / ScaleFactor),
                    (int)(figure.Height / ScaleFactor)),
                new Bgr(Color.BurlyWood), 3);
        }

        private static Image<Bgr, byte> MarkAngles(Image<Gray, byte> image, double degrees)
        {
            var editableImage = image.Convert<Bgr, byte>();
            var radians = degrees * (Math.PI / 180);
            var blackPen = new System.Drawing.Pen(Color.LightGray, 1);

            using (var graphics = Graphics.FromImage(editableImage.Bitmap))
            {
                for (var currAngle = 0.0; currAngle < Math.PI / 2; currAngle += radians)
                {

                    DrawLineAtAngle(image, currAngle, graphics, blackPen);
                    DrawLineAtAngle(image, -currAngle, graphics, blackPen);
                }
            }
            return editableImage;
        }

        private static void DrawLineAtAngle(Image<Gray, byte> image, double currAngle, Graphics graphics, Pen blackPen)
        {
            var x2 = (image.Width / 2) + (int) (Math.Cos(currAngle - Math.PI / 2) * (image.Width));
            var y2 = image.Height + (int) (Math.Sin(currAngle - Math.PI / 2) * (image.Width));

            graphics.DrawLine(blackPen, x2,
                y2,
                image.Width / 2,
                image.Height);
        }

        public static Image<Bgr, byte> ConvertToGrayscale(Bitmap original)
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

        public BitmapImage Convert(Bitmap src)
        {
            var ms = new MemoryStream();
            src.Save(ms, ImageFormat.Bmp);
            var image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}