using System.Drawing;

namespace FaceTracker
{
    public class Face
    {
        public Rectangle FacePosition { get; set; }
        public Rectangle LeftEyePosition { get; set; }
        public Rectangle RigthEyePosition { get; set; }
    }
}
