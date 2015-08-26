using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZVI
{
    public class FrameWrapper
    {
        public Bitmap Frame { get;  private set; }

        public List<Rectangle> MovingObjects { get; private set; }

        public FrameWrapper(Bitmap frame, List<Rectangle> movingObjects)
        {
            this.Frame = frame;
            this.MovingObjects = movingObjects;
        }
    }
}
