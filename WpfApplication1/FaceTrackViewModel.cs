using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Color = System.Drawing.Color;
using Ellipse = Emgu.CV.Structure.Ellipse;
using Pen = System.Drawing.Pen;
using Rectangle = System.Drawing.Rectangle;

namespace FaceTracker
{
    public class FaceTrackViewModel : INotifyPropertyChanged
    {
        #region Public UI Properties
        public double EyeBasedAngle { get; set; }

        public double ScaleFactor { get; set; }

        public float FrameGenerationTime { get; set; }

        public Bitmap ImageFrame { get; set; }
        
        public bool FaceDetectionEnabled { get; set; }
        
        public bool HistogramEqualizationEnabled { get; set; }
        
        public Bitmap PostProcessedFrame { get; set; }

        public Bitmap AngleBitmap { get; set; }
        public FixedSizeObservableQueue<KeyValuePair<int, int>> ProcessTimeQueue { get; set; }

        private QualityEnum _quality;
        public QualityEnum Quality
        {
            get { return _quality;}

            set
            {
                _quality = value;

                switch (value)
                {
                    case QualityEnum.Minimum:
                        ScaleFactor = 0.34;
                        break;
                    case QualityEnum.Low:
                        ScaleFactor = 0.41;
                        break;
                    case QualityEnum.Medium:
                        ScaleFactor = 0.50;
                        break;
                    case QualityEnum.High:
                        ScaleFactor = 0.72;
                        break;
                    case QualityEnum.Excelent:
                        ScaleFactor = 1.0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnPropertyChanged();
            }
        }
        

        #endregion
        
        private readonly Capture _capture;

        private List<long> _frameGenerationTimeList = new List<long>();

        private Face _previousFacePosition;
        private Face _currentFacePosition;

        private int _frameCount;

        private FaceDetector fd;

        public FaceTrackViewModel()
        {
            _capture = new Capture {FlipHorizontal = true};

            Quality = QualityEnum.Medium;
            
            var image = new Bitmap(640, 480);
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.Transparent);

                AngleBitmap = MarkAngles(image, 22.5);
            }

            PostProcessedFrame = new Bitmap(640, 480);
            ImageFrame = new Bitmap(640, 480);

            _capture.Start();

            fd = new FaceDetector();

            ProcessTimeQueue = new FixedSizeObservableQueue<KeyValuePair<int, int>>(30);

            System.Windows.Forms.Application.Idle += ProcessFrame;
        }

        private Image<Gray, byte> EqualizeHistogram(Image<Gray, byte> image)
        {
            // Convert a BGR image to HLS range
            var imageHsi = new Image<Hls, byte>(image.Bitmap);

            // Equalize the Intensity Channel
            var equalized = imageHsi[1];
            equalized._EqualizeHist();

            // Convert the image back to BGR range
            return equalized.Convert<Gray, byte>();
        }

        public void ProcessFrame(object sender, EventArgs eventArgs)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var frame = _capture.QueryFrame().ToImage<Bgr, byte>();

            var grayFrame = PreProcessFrame(frame);

            PostProcessedFrame = grayFrame.Bitmap;

            if (FaceDetectionEnabled)
            {
                _previousFacePosition = _currentFacePosition;

                _currentFacePosition = fd.GetFacePosition(grayFrame);

                DrawRectangle(frame, _currentFacePosition.FacePosition, Color.BurlyWood);
                if (_previousFacePosition != null)
                    DrawRectangle(frame, Rectangle.Inflate(_previousFacePosition.FacePosition, 5, 5), Color.Aqua);

                DrawEllipse(frame, _currentFacePosition.LeftEyePosition, Color.Brown);
                DrawEllipse(frame, _currentFacePosition.RigthEyePosition, Color.BurlyWood);

                EyeBasedAngle = _currentFacePosition.FaceAngle;
            }

            ImageFrame = frame.Bitmap;

            stopwatch.Stop();

            FrameGenerationTime = stopwatch.ElapsedMilliseconds;

            _frameGenerationTimeList.Add(stopwatch.ElapsedMilliseconds);

            if (_frameGenerationTimeList.Count > 3)
            {
                ++_frameCount;
                ProcessTimeQueue.Enqueue(new KeyValuePair<int, int>(_frameCount, (int)(1000 / _frameGenerationTimeList.Average())));
                _frameGenerationTimeList.Clear();

            }
        }

        private Image<Gray, byte> PreProcessFrame(Image<Bgr, byte> frame)
        {
            var grayFrame = frame.Resize(ScaleFactor,
                                Emgu.CV.CvEnum.Inter.Area)
                            .Convert<Gray, byte>();

            if (HistogramEqualizationEnabled)
                grayFrame = EqualizeHistogram(grayFrame);
            return grayFrame;
        }

        private void DrawRectangle(Image<Bgr, byte> frame, Rectangle figure, Color color)
        {
            frame.Draw(new Rectangle((int)(figure.X / ScaleFactor),
                    (int)(figure.Y / ScaleFactor),
                    (int)(figure.Width / ScaleFactor),
                    (int)(figure.Height / ScaleFactor)),
                new Bgr(color), 3);
        }

        private void DrawEllipse(Image<Bgr, byte> frame, Rectangle figure, Color color)
        {
            frame.Draw(new Ellipse(new PointF((float) (figure.X / ScaleFactor + figure.Width / ScaleFactor / 2),
                    (float) (figure.Y / ScaleFactor + figure.Height / ScaleFactor / 2)),
                new SizeF((float) (figure.Width / ScaleFactor),
                    (float) (figure.Height / ScaleFactor)), 0.0f), new Bgr(color), 3);
        }

        private static Bitmap MarkAngles(Bitmap image, double degrees)
        {
            var radians = degrees * (Math.PI / 180);
            var blackPen = new System.Drawing.Pen(Color.LightGray, 1);

            using (var graphics = Graphics.FromImage(image))
            {
                for (var currAngle = 0.0; currAngle < Math.PI / 2; currAngle += radians)
                {
                    DrawLineAtAngle(image, currAngle, graphics, blackPen);
                    DrawLineAtAngle(image, -currAngle, graphics, blackPen);
                }
            }

            return image;
        }

        private static void DrawLineAtAngle(Bitmap image, double currAngle, Graphics graphics, Pen blackPen)
        {
            var x2 = (image.Width / 2) + (int) (Math.Cos(currAngle - Math.PI / 2) * (image.Width));
            var y2 = image.Height + (int) (Math.Sin(currAngle - Math.PI / 2) * (image.Width));

            graphics.DrawLine(blackPen, x2,
                y2,
                image.Width / 2,
                image.Height);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}