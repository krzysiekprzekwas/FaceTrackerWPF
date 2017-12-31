using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OpenTK.Graphics.OpenGL;
using Color = System.Drawing.Color;
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

        private Emgu.CV.Image<Gray, byte> EqualizeHistogram(Emgu.CV.Image<Gray,byte> image)
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

            Rectangle[] faces = {};
            if (FaceDetectionEnabled)
                faces = _cascadeFaceClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);


            foreach (var face in faces)
            {
                frame.Draw(new Rectangle((int)(face.X / ScaleFactor),
                        (int)(face.Y / ScaleFactor),
                        (int)(face.Width / ScaleFactor),
                        (int)(face.Height / ScaleFactor)),
                    new Bgr(Color.BurlyWood), 3);
                

                _previousFacePosition = new Rectangle((int) (face.X / ScaleFactor - ROIOffset),
                    (int) (face.Y / ScaleFactor - ROIOffset),
                    (int) (face.Width / ScaleFactor + ROIOffset * 2),
                    (int) (face.Height / ScaleFactor + ROIOffset * 2));

                frame.Draw( _previousFacePosition, new Bgr(Color.Chartreuse), 3);
            }

            Rectangle[] eyes = {};
            if (EyeDetectionEnabled)
                eyes = _cascadeEyeClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

            foreach (var eye in eyes)
            {
                frame.Draw(new Rectangle((int) (eye.X / ScaleFactor),
                        (int) (eye.Y / ScaleFactor),
                        (int) (eye.Width / ScaleFactor),
                        (int) (eye.Height / ScaleFactor)),
                    new Bgr(Color.Blue), 3);
            }

            var editableGrayFrame = MarkAngles(grayFrame);

            ImageFrame = Convert(frame.ToBitmap());

            PostProcessedFrame = Convert(editableGrayFrame.ToBitmap());
        }

        private static Image<Bgr, byte> MarkAngles(Image<Gray, byte> grayFrame)
        {
            var editableGrayFrame = grayFrame.Convert<Bgr, byte>();

            var blackPen = new System.Drawing.Pen(Color.Black, 1);
            using (var graphics = Graphics.FromImage(editableGrayFrame.Bitmap))
            {
                graphics.DrawLine(blackPen, 0, 0, grayFrame.Width / 2, grayFrame.Height);
                graphics.DrawLine(blackPen, grayFrame.Width / 4, 0, grayFrame.Width / 2, grayFrame.Height);
                graphics.DrawLine(blackPen, grayFrame.Width / 2, 0, grayFrame.Width / 2, grayFrame.Height);
                graphics.DrawLine(blackPen, 3 * grayFrame.Width / 4, 0, grayFrame.Width / 2, grayFrame.Height);
                graphics.DrawLine(blackPen, grayFrame.Width, 0, grayFrame.Width / 2, grayFrame.Height);
            }
            return editableGrayFrame;
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