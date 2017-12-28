using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        private CascadeClassifier _cascadeFaceClassifier;
        private CascadeClassifier _cascadeEyeClassifier;

        private Capture _capture;

        public FaceTrackViewModel()
        {
            _capture = new Capture();

            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            System.Windows.Forms.Application.Idle += timer_Tick;
        }


        public void timer_Tick(object a, EventArgs e)
        {
            var tmp = _capture.QueryFrame().ToImage<Bgr, byte>();

            var _grayframe = tmp.Convert<Gray, byte>();

            var faces = _cascadeFaceClassifier.DetectMultiScale(_grayframe, 1.1, 10, System.Drawing.Size.Empty);


            foreach (var face in faces)
            {
                tmp.Draw(face, new Bgr(Color.BurlyWood), 3);
            }


            var eyes = _cascadeEyeClassifier.DetectMultiScale(_grayframe, 1.1, 10, System.Drawing.Size.Empty);

            foreach (var eye in eyes)
            {
                tmp.Draw(eye, new Bgr(Color.Blue), 3);
            }
            var tmpBmp = tmp.ToBitmap();
            ImageFrame = Convert(tmpBmp);
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