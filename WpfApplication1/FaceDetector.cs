using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;

namespace FaceTracker
{
    public class FaceDetector
    {
        private readonly CascadeClassifier _cascadeFaceClassifier;
        private readonly CascadeClassifier _cascadeEyeClassifier;

        public bool HistogramEqualizationEnabled { get; set; }

        private readonly Capture _capture;
        private const int ROIOffset = 30;

        public double ScaleFactor { get; set; }

        public FaceDetector()
        {
            _cascadeFaceClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");
            _cascadeEyeClassifier = new CascadeClassifier("haarcascade_eye.xml");

            ScaleFactor = 0.5;
        }

        public Face GetFacePosition(Image<Gray, byte> grayFrame)
        {
            var face = new Face();

            var faces = _cascadeFaceClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

            var facePos = faces.OrderBy(x => x.Width * x.Height).FirstOrDefault();
            
            if (facePos.Width * facePos.Height > 0)
            {
                face.FacePosition = new Rectangle(facePos.X - ROIOffset,
                                                  facePos.Y - ROIOffset,
                                                  facePos.Width + ROIOffset * 2,
                                                  facePos.Height + ROIOffset * 2);
            }
            var eyes = _cascadeEyeClassifier.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty);

            var twoEyes = eyes.OrderBy(x => x.Width * x.Height).Take(2).OrderBy(x => x.X).ToList();

            if (twoEyes.Count == 2)
            {
                face.LeftEyePosition = twoEyes[0];
                face.RigthEyePosition = twoEyes[1];
            }

            return face;
        }

    }
}