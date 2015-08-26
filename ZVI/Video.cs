using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using AForge.Video.FFMPEG;

namespace ZVI
{
    class Video
    {
        public int VideoWidth {get; private set;}
        public int VideoHeight { get; private set; }

        public int VideoFrameRate { get; private set; }
        public bool VideoInitialized { get; private set; }

        

        private MainWindow mainWindow;

        private VideoFileReader videoFileReader;

        private MotionDetector motionDetector;

        public Video(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            
        }


        internal void initVideo(string filePath)
        {
            videoFileReader = new VideoFileReader();
            videoFileReader.Open(filePath);
            VideoWidth = videoFileReader.Width;
            VideoHeight = videoFileReader.Height;
            VideoFrameRate = videoFileReader.FrameRate;
            motionDetector = new MotionDetector(mainWindow);
            VideoInitialized = true;
           
        }

        internal FrameWrapper test()
        {
            //return videoFileReader.ReadVideoFrame();
            return motionDetector.test(videoFileReader.ReadVideoFrame());
        }

        public FrameWrapper getFrame()
        {
            Bitmap frame = videoFileReader.ReadVideoFrame();
            FrameWrapper frameWrapper = motionDetector.compute(mainWindow.Method, frame);
            return frameWrapper;
        }
    }
}
