using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;
using System.Drawing;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace KinectRecorderAccord
{
    class ColorHandler
    {
        private Bitmap cBitmap;

        // writer buffer
        private Queue<System.Drawing.Bitmap> colorBitmapBuffer = new Queue<System.Drawing.Bitmap>();
        byte[] colorPixelBuffer;

        // writer class
        private String colorVideoPath;
        private VideoFileWriter colorWriter;
        private int bitRate = 1200000;

        public UInt32 frameCount = 0;

        // FrameDescriptor
        private FrameDescription colorFrameDescription = null;

        public int Width, Height;

        private bool colorRecording = false;

        //private readonly object _lock;

        public ColorHandler(FrameDescription fd)//, object l)
        {
            //_lock = l;

            colorFrameDescription = fd;
            Width = fd.Width;
            Height = fd.Height;
            
            colorPixelBuffer = new byte[Width * Height * 4];


        }

        public void Write()
        {
            while (true)
            {

                //Console.WriteLine("color");
                if (colorBitmapBuffer.Count > 0)
                {
                    //Console.WriteLine(colorBitmapBuffer.Count);
                    this.colorWriter.WriteVideoFrame(colorBitmapBuffer.Dequeue());
                }
                else if (!colorRecording)
                {
                    colorWriter.Close();
                    Console.WriteLine("color writer closed.");
                    break;
                }
            }
        }

        public void SetVideoPath(string path, int br)
        {
            colorVideoPath = path;
            bitRate = br;
            openVideoWriter();
        }

        public void openVideoWriter()
        {
            Accord.Math.Rational rationalFrameRate = new Accord.Math.Rational(30);
            colorWriter = new VideoFileWriter();
            colorWriter.Open(colorVideoPath, Width, Height, rationalFrameRate, VideoCodec.MPEG4, bitRate);
            frameCount = 0;
        }

        public void setRecordingState(bool state)
        {
            colorRecording = state;
        }

        public void ColorFrameArrival(ColorFrame colorFrame, ref WriteableBitmap colorBitmap, double fps)
        {

            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
            {
                colorBitmap.Lock();

                int width = colorFrame.FrameDescription.Width;
                int height = colorFrame.FrameDescription.Height;
                // verify data and write the new color frame data to the display bitmap
                if ((width == colorBitmap.PixelWidth) && (height == colorBitmap.PixelHeight))
                {

                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        colorBitmap.BackBuffer,
                        (uint)(width * height * 4),
                        ColorImageFormat.Bgra);

                    colorBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                    colorBitmap.Unlock();

                    colorFrame.CopyConvertedFrameDataToArray(colorPixelBuffer, ColorImageFormat.Bgra);

                    if (colorRecording)
                    {
                        cBitmap = UtilityClass.ByteArrayToBitmap(colorPixelBuffer, width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        colorBitmapBuffer.Enqueue(cBitmap);
                        System.GC.Collect();
                        frameCount++;
                        if (fps < 16.0)
                        {
                            Console.WriteLine("fps droped");
                            colorBitmapBuffer.Enqueue(cBitmap);
                            frameCount++;
                        }
                    }
                }
            }
                       
                      
        }


    }
}
