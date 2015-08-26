using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;

namespace ZVI
{
    class MotionDetector
    {
        private MainWindow mainWindow;

        const int MIN_AREA_SIZE = 20;

        const int MERGE_DISTANCE = 5;

        //public List<Rectangle> Areas { get; private set; }

        private Bitmap lastFrame;

        private Bitmap lastButOneFrame;

        private Bitmap background;

        private byte[] pixels;

        private int frameWidth;

        private int areaX1;

        private int areaX2;

        private int areaY1;

        private int areaY2;


        public MotionDetector(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        internal FrameWrapper compute(Method method, Bitmap frame)
        {
            Bitmap differenceFrame = getDifference(method, frame);

            lastButOneFrame = lastFrame;
            lastFrame = frame;

            // if we cannot compute diffference yet, we return empty list of areas
            if (differenceFrame == null)
            {
                return new FrameWrapper(frame, new List<Rectangle>());
            }

            Bitmap grayscaledFrame = transformToGrayscale(differenceFrame);
            Bitmap thresholdedFrame = getThresholdedFrame(grayscaledFrame);

            if (mainWindow.UseOpeningFilter)
            {
                //Console.WriteLine("openning openingFilter used");
                Opening openingFilter = new Opening();
                thresholdedFrame = openingFilter.Apply(thresholdedFrame);
            }

            List<Rectangle> areas = getAreas(thresholdedFrame);
            List<Rectangle> movingObjects = mergeAreas(areas);

            Bitmap outputFrame = null;

            switch (mainWindow.OutputFrameType)
            {
                case Output.Source:
                    if (method == Method.ThreeFrame)
                    {
                        outputFrame = lastFrame;
                    }
                    else
                    {
                        outputFrame = frame;
                    }
                    break;
                case Output.Differenced:
                    outputFrame = differenceFrame;
                    break;
                case Output.Greyscaled:
                    outputFrame = grayscaledFrame;
                    break;
                case Output.Thresholded:
                    outputFrame = thresholdedFrame;
                    break;
            }

            return new FrameWrapper(outputFrame, movingObjects);
        }

        private List<Rectangle> getAreas(Bitmap trasholdedFrame)
        {
            List<Rectangle> areas = new List<Rectangle>();
            
            frameWidth = trasholdedFrame.Width;

            //bool[,] checkedPixesls = new bool[frameWidth, thresholdedFrame.Height];
            bool[] checkedPixesls = new bool[frameWidth * trasholdedFrame.Height];

            // getting data from image
            Rectangle rect = new Rectangle(0, 0, frameWidth, trasholdedFrame.Height);
            BitmapData bitmapData = trasholdedFrame.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, trasholdedFrame.PixelFormat);

            // Get the address of the first line.
            IntPtr pointer = bitmapData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes  = Math.Abs(bitmapData.Stride) * trasholdedFrame.Height;
            pixels = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(pointer, pixels, 0, bytes);

            for(int i = 0; i < pixels.Length; i++){
                if(checkedPixesls[i]){
                    continue;
                }

                checkedPixesls[i] = true;

                if(pixels[i] == 0){
                    continue;
                }

                Queue<int> areaPixels = new Queue<int>();
                areaPixels.Enqueue(i);

                int y = i / frameWidth;
                int x = i - (y * frameWidth);
                areaX1 = x;
                areaX2 = x;
                areaY1 = y;
                areaY2 = y;

                while(areaPixels.Count != 0){
                    int pixelIndex = areaPixels.Dequeue();

                    // try to expand area rectangle
                    y = pixelIndex / frameWidth;
                    x = pixelIndex - (y * frameWidth);

                    if (x < areaX1)
                    {
                        areaX1 = x;
                    }
                    else if (x > areaX2)
                    {
                        areaX2 = x;
                    }
                    if (y < areaY1)
                    {
                        areaY1 = y;
                    }
                    else if (y > areaY2)
                    {
                        areaY2 = y;
                    }

                    int[] neigbourPixelIndexes = getNeigbourPixelIndexes(pixelIndex);

                    for (int j = 0; j < neigbourPixelIndexes.Length; j++)
                    {
                        
                        int index = neigbourPixelIndexes[j];
                        if (index == -1 || checkedPixesls[index])
                        {
                            continue;
                        }
                        checkedPixesls[index] = true;

                        if(pixels[index] == 0){
                            continue;
                        }
                        
                        areaPixels.Enqueue(index);
                    }
                }

                // evading of small areas
                if ((areaX2 - areaX1) * (areaY2 - areaY1) > MIN_AREA_SIZE)
                {
                    areas.Add(new Rectangle(areaX1, areaY1, areaX2 - areaX1, areaY2 - areaY1));
                }
            }

            // Set every third value to 255. A 24bpp bitmap will look red.  
            //for (int counter = 2; counter < pixels.Length; counter += 3)
            //    pixels[counter] = 255;

            // Unlock the bits.
            trasholdedFrame.UnlockBits(bitmapData);

            return areas;
        }

        private int[] getNeigbourPixelIndexes(int pixelIndex)
        {
            int[] neigbourPixelIndexes = new int[8];

            neigbourPixelIndexes[0] = (pixelIndex - 1) % frameWidth > 0 && pixelIndex - frameWidth - 1 > 0 ? pixelIndex - frameWidth - 1 : -1;
            neigbourPixelIndexes[1] = pixelIndex - frameWidth > 0 ? pixelIndex - frameWidth : -1;
            neigbourPixelIndexes[2] = (pixelIndex + 1) % frameWidth > 0 && pixelIndex - frameWidth > 0 ? pixelIndex - frameWidth + 1 : -1;

            neigbourPixelIndexes[3] = (pixelIndex - 1) % frameWidth > 0 ? pixelIndex - 1 : -1;
            neigbourPixelIndexes[4] = (pixelIndex + 1) % frameWidth > 0 ? pixelIndex + 1 : -1;

            neigbourPixelIndexes[5] = (pixelIndex - 1) % frameWidth > 0 && pixelIndex + frameWidth < pixels.Length ? pixelIndex + frameWidth - 1 : -1;
            neigbourPixelIndexes[6] = pixelIndex + frameWidth < pixels.Length ? pixelIndex + frameWidth : -1;
            neigbourPixelIndexes[7] = (pixelIndex + 1) % frameWidth > 0 && pixelIndex + frameWidth + 1 < pixels.Length ? pixelIndex + frameWidth + 1 : -1;

            return neigbourPixelIndexes;
        }

        private byte getPixel(int x, int y)
        {
            // Get start index of the specified pixel
            int i = ((y * frameWidth) + x);

            byte pixel = pixels[i];

            return pixel;
        }

        private List<Rectangle> mergeAreas(List<Rectangle> areas)
        {
            List<Rectangle> areasMerged = new List<Rectangle>();

            //for (int i = areas.Count - 1; i >= 0; i--)
            //{
            //    Rectangle rectangle = areas[i];
            //    areas.RemoveAt(i);

            //    for (int j = areas.Count - 1; j >= 0; j--)
            //    {
            //        Rectangle rectangle2 = areas[j];
            //        if (rectanglesOverlap(rectangle, rectangle2) || getRectanglesDistance(rectangle, rectangle2) < MERGE_DISTANCE)
            //        {
            //            rectangle = mergeRectangles(rectangle, rectangle2);
            //            areas.RemoveAt(j);
            //            i--;
            //        }
            //    }
            //    areasMerged.Add(rectangle);
            //}


            int areasMergedLastLength = 0;
            while (true)
            {
                for (int i = areas.Count - 1; i >= 0; i--)
                {
                    Rectangle rectangle = areas[i];
                    areas.RemoveAt(i);

                    for (int j = areas.Count - 1; j >= 0; j--)
                    {
                        Rectangle rectangle2 = areas[j];
                        if (rectanglesOverlap(rectangle, rectangle2) || getRectanglesDistance(rectangle, rectangle2) < MERGE_DISTANCE)
                        {
                            rectangle = mergeRectangles(rectangle, rectangle2);
                            areas.RemoveAt(j);
                            i--;
                        }
                    }
                    areasMerged.Add(rectangle);
                }
                if (areasMerged.Count == areasMergedLastLength)
                {
                    break;
                }

                areas = areasMerged;
                areasMergedLastLength = areasMerged.Count;
                areasMerged = new List<Rectangle>();
            }

            return areasMerged;
        }

        private bool rectanglesOverlap(Rectangle rectangle, Rectangle rectangle2)
        {
            bool overlap = rectangle.IntersectsWith(rectangle2);
            //if (overlap)
            //{
            //    Console.WriteLine("rectangles overlap");
            //}
            return overlap;
        }

        private Rectangle mergeRectangles(Rectangle rectangle, Rectangle rectangle2)
        {
            // only for rectangles whitch not overlap each other
            //int x, y, width, height;
            //if (rectangle.X < rectangle2.X)
            //{
            //    x = rectangle.X;
            //    width = rectangle2.X + rectangle2.Width - x;
            //}
            //else
            //{
            //    x = rectangle2.X;
            //    width = rectangle.X + rectangle.Width - x;
            //}

            //if (rectangle.Y < rectangle2.Y)
            //{
            //    y = rectangle.Y;
            //    height = rectangle2.Y + rectangle2.Height - y;
            //}
            //else
            //{
            //    y = rectangle2.Y;
            //    height = rectangle.Y + rectangle.Height - y;
            //}

            int x1 = Math.Min(rectangle.X, rectangle2.X);
            int y1 = Math.Min(rectangle.Y, rectangle2.Y);
            int x2 = Math.Max(rectangle.Right, rectangle2.Right);
            int y2 = Math.Max(rectangle.Bottom, rectangle2.Bottom);

            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        private int getRectanglesDistance(Rectangle rectangle, Rectangle rectangle2)
        {
            int aDistance = Math.Abs(rectangle.Y - rectangle2.Y + rectangle2.Height);
            int bDistance = Math.Abs(rectangle.X + rectangle.Width - rectangle2.X);
            int cDistance = Math.Abs(rectangle.Y + rectangle.Height - rectangle2.Y);
            int dDistance = Math.Abs(rectangle.X - rectangle2.Width);

            return Math.Max(Math.Min(aDistance, cDistance), Math.Min(bDistance, dDistance));
        }

        private Bitmap getThresholdedFrame(Bitmap grayscaledFrame)
        {
            Threshold filter = new Threshold(18);
            Bitmap thresholdedFrame = filter.Apply(grayscaledFrame);
            return thresholdedFrame;
        }

        private Bitmap transformToGrayscale(Bitmap differenceFrame)
        {
            Grayscale filter = new Grayscale( 0.2125, 0.7154, 0.0721 );
            Bitmap grayscaledFrame = filter.Apply(differenceFrame);
            return grayscaledFrame;
        }

        private Bitmap getDifference(Method method, Bitmap frame)
        {
            switch (method)
            {
                case Method.TwoFrame:
                    return getTwoFrameDifference(frame);
                case Method.ThreeFrame:
                    return getThreeFrameDifference(frame);
                case Method.ChangingBackground:
                    return getChangingBackgroundDifference(frame);
                default:
                    throw new NotImplementedException("unknown difference method");
            }
        }

        private Bitmap getChangingBackgroundDifference(Bitmap frame)
        {
            if (background == null || lastFrame == null)
            {
                background = frame;
                return null;
            }

            if (background != lastFrame)
            {
                Morph filter = new Morph(background);
                filter.SourcePercent = 0.5;
                background = filter.Apply(lastFrame);
            }

            Difference differenceFilter = new Difference(background);
            Bitmap differenceBitmap = differenceFilter.Apply(frame);

            return differenceBitmap;
        }

        private Bitmap getThreeFrameDifference(Bitmap frame)
        {
            if (lastFrame == null || lastButOneFrame == null)
            {
                return null;
            }

            Difference filterPrevious = new Difference(lastButOneFrame);
            Bitmap differenceBitmapPrevious = filterPrevious.Apply(lastFrame);

            Difference filterNext = new Difference(lastFrame);
            Bitmap differenceBitmapNext = filterNext.Apply(frame);

            Intersect intersectionFilter = new Intersect(differenceBitmapPrevious);
            Bitmap threeFrameDifferenceBitmap = intersectionFilter.Apply(differenceBitmapNext);

            return threeFrameDifferenceBitmap;
        }

        private Bitmap getTwoFrameDifference(Bitmap frame)
        {
            if (lastFrame == null)
            {
                return null;
            }

            Difference filter = new Difference(lastFrame);
            Bitmap differenceBitmap = filter.Apply(frame);

            return differenceBitmap;
        }

        internal FrameWrapper test(Bitmap frame)
        {
            //Bitmap differenceFrame =  getTwoFrameDifference(frame);
            //Bitmap differenceFrame = getThreeFrameDifference(frame);
            Bitmap differenceFrame = getChangingBackgroundDifference(frame);

            lastButOneFrame = lastFrame;
            lastFrame = frame;

            if (differenceFrame == null)
            {
                return new FrameWrapper(frame, null);
            }

            Bitmap grayscaledFrame = transformToGrayscale(differenceFrame);
            Bitmap trasholdedFrame = getThresholdedFrame(grayscaledFrame);

            List<Rectangle> areas = getAreas(trasholdedFrame);

            List<Rectangle> movingObjects = mergeAreas(areas);

            return new FrameWrapper(trasholdedFrame, movingObjects);
        }
    }
}
