using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls.DataVisualization.Charting;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using OpenTK.Graphics.OpenGL;
using PropertyChanged;
using Color = System.Drawing.Color;
using Ellipse = Emgu.CV.Structure.Ellipse;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace FaceTracker
{
    public class FaceTrackViewModel : INotifyPropertyChanged
    {
        public double EyeBasedAngle { get; set; }

        public double ScaleFactor { get; set; }

        public float FrameGenerationTime { get; set; }

        public Bitmap ImageFrame { get; set; }
        
        public bool FaceDetectionEnabled { get; set; }
        
        public bool EyeDetectionEnabled { get; set; }
        
        public bool HistogramEqualizationEnabled { get; set; }
        
        public Bitmap PostProcessedFrame { get; set; }

        public Bitmap AngleBitmap { get; set; }
        public FixedSizeObservableQueue<KeyValuePair<int, int>> ProcessTimeQueue { get; set; }

        private readonly CascadeClassifier _cascadeFaceClassifier;
        private readonly CascadeClassifier _cascadeEyeClassifier;

        private readonly Capture _capture;
        private const int ROIOffset = 30;

        private Rectangle _previousFacePosition;

        private int _frameCount;

        public FaceTrackViewModel()
        {
            _capture = new Capture {FlipHorizontal = true};

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            ScaleFactor = 0.5;
            
            var image = new Bitmap(640, 480);
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.Transparent);

                AngleBitmap = MarkAngles(image, 22.5);
            }

            PostProcessedFrame = new Bitmap(640, 480);
            ImageFrame = new Bitmap(640, 480);

            _capture.Start();


            ProcessTimeQueue = new FixedSizeObservableQueue<KeyValuePair<int, int>>(30);

            System.Windows.Forms.Application.Idle += ProcessFrame;
        }

        public void ProcessFrame(object a, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            PerformFaceDetection();

            stopwatch.Stop();

            FrameGenerationTime = stopwatch.ElapsedMilliseconds;

            ++_frameCount;
            ProcessTimeQueue.Enqueue(new KeyValuePair<int, int>(_frameCount, (int)(1000/FrameGenerationTime)));
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

                DrawRectangle(frame, face, Color.BurlyWood);

                if (face.Width * face.Height > 0)
                {
                    _previousFacePosition = new Rectangle((int)(face.X - ROIOffset),
                        (int)(face.Y  - ROIOffset),
                        (int)(face.Width  + ROIOffset * 2),
                        (int)(face.Height  + ROIOffset * 2));
                }

                DrawRectangle(frame,_previousFacePosition, Color.Chartreuse);
                
            }

            if (EyeDetectionEnabled)
            {
                var eyes = _cascadeEyeClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

                var twoEyes = eyes.OrderBy(x => x.Width * x.Height).Take(2).OrderBy(x => x.X).ToList();
                
                if (twoEyes.Count == 2)
                {
                    DrawEllipse(frame, twoEyes[0], Color.Brown);
                    DrawEllipse(frame, twoEyes[1], Color.BurlyWood);
                    
                    var dx = (twoEyes[0].X + twoEyes[0].Width / 2) - (twoEyes[1].X + twoEyes[1].Width / 2);
                    var dy = (twoEyes[0].Y + twoEyes[0].Height / 2) - (twoEyes[1].Y + twoEyes[1].Height / 2);

                    EyeBasedAngle = Math.Atan2(dx, dy) * (180.0 / Math.PI) + 90;
                }

            }
            
            ImageFrame = frame.Bitmap;

            PostProcessedFrame = grayFrame.Bitmap;
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
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}