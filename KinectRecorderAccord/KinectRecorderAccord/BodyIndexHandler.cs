using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;
using System.Drawing;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace KinectRecorder
{
    class BodyIndexHandler
    {
        //Bodyindex variables
        private Bitmap bBitmap;

        /// Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;
        /// Collection of colors to be used to display the BodyIndexFrame data.
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };
        /// Intermediate storage for frame data converted to color
        public uint[] bodyIndexPixels = null;

        private FrameDescription bodyIndexFrameDescription = null;

        private Queue<System.Drawing.Bitmap> bodyBitmapBuffer = new Queue<System.Drawing.Bitmap>();
        public byte[] bodyPixelBuffer;

        private String BodyIndexPath;
        private VideoFileWriter bodyWriter;
        private int bitRate = 1200000;

        public UInt32 frameCount = 0;

        public int Width, Height;

        private bool bodyRecording = false;


        public BodyIndexHandler(FrameDescription fd)
        {
            bodyIndexFrameDescription = fd;
            Width = fd.Width;
            Height = fd.Height;

            bodyIndexPixels = new uint[Width * Height];
            
            // to save to a video helper buffer
            bodyPixelBuffer = new byte[Width * Height];
        }

        public void Write()
        {
            while (true)
            {
                //Console.WriteLine("Body");
                if (bodyBitmapBuffer.Count > 0)
                {
                    //Console.WriteLine(bodyBitmapBuffer.Count);
                    this.bodyWriter.WriteVideoFrame(bodyBitmapBuffer.Dequeue());
                }
                else if (!bodyRecording)
                {
                    bodyWriter.Close();
                    Console.WriteLine("body writer closed.");
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public void SetVideoPath(string path, int br)
        {
            BodyIndexPath = path;
            bitRate = br;
            openVideoWriter();
        }

        public void openVideoWriter()
        {
            Accord.Math.Rational rationalFrameRate = new Accord.Math.Rational(30);
            bodyWriter = new VideoFileWriter();
            bodyWriter.Open(BodyIndexPath, Width, Height, rationalFrameRate, VideoCodec.MPEG4, bitRate);
            frameCount = 0;
        }

        public void setRecordingState(bool state)
        {
            bodyRecording = state;
            frameCount = 0;
        }

        public void BodyIndexFrameArrival(BodyIndexFrame bif, ref bool frameProcessed, double fps, WriteableBitmap bodyIndexBitmap)
        {
            // the fastest way to process the body index data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bif.LockImageBuffer())
            {
                int width = bif.FrameDescription.Width;
                int height = bif.FrameDescription.Height;
                // verify data and write the color data to the display bitmap
                if (((width * height) == bodyIndexBuffer.Size) &&
                    (width == bodyIndexBitmap.PixelWidth) && (height == bodyIndexBitmap.PixelHeight))
                {
                    
                    ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                    frameProcessed = true;
                }

                if (bodyRecording)
                {
                    bBitmap = UtilityClass.ByteArrayToBitmap(bodyPixelBuffer, width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    bodyBitmapBuffer.Enqueue(bBitmap);
                    System.GC.Collect();
                    frameCount++;
                    if (fps < 16.0)
                    {
                        Console.WriteLine("fps drop yaşandı");
                        bodyBitmapBuffer.Enqueue(bBitmap);
                        frameCount++;
                    }
                }
            }

        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];

                    this.bodyPixelBuffer[i] = (byte)255;
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this.bodyIndexPixels[i] = 0x00000000;

                    this.bodyPixelBuffer[i] = (byte)0;
                }
            }
        }

    }
}
