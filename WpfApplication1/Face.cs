using System;
using System.Drawing;

namespace FaceTracker
{
    public class Face
    {
        public Rectangle FacePosition { get; set; }
        public Rectangle LeftEyePosition { get; set; }
        public Rectangle RigthEyePosition { get; set; }

        public double FaceAngle => GetRectangeAngle(LeftEyePosition, RigthEyePosition);

        private static double GetRectangeAngle(Rectangle rec1, Rectangle rec2)
        {
            var dx = (rec1.X + rec1.Width / 2) -
                     (rec2.X + rec2.Width / 2);
            var dy = (rec1.Y + rec1.Height / 2) -
                     (rec2.Y + rec2.Height / 2);

            return Math.Atan2(dx, dy) * (180.0 / Math.PI) + 90;
        }
    }
}
