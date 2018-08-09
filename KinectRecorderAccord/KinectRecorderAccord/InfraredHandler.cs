using Accord.Video.FFMPEG;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectRecorderAccord
{
    class InfraredHandler
    {
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        private const float InfraredSourceScale = 0.75f;

        private const float InfraredOutputValueMinimum = 0.01f;

        private const float InfraredOutputValueMaximum = 1.0f;

        public FrameDescription infraredFrameDescription = null;

        private Bitmap iBitmap;

        private Queue<Bitmap> infraredBitmapBuffer = new Queue<Bitmap>();
        public byte[] infraredPixelBuffer;

        private String infraredVideoPath;
        private VideoFileWriter infraredWriter;
        private int bitRate = 1200000;

        public UInt32 frameCount = 0;

        public int Width, Height;

        private bool infraredRecording = false;
        public byte[] infraredPixels = null; 

        public InfraredHandler(FrameDescription fd)
        {

            infraredFrameDescription = fd;
            Width = fd.Width;
            Height = fd.Height;

            // to show on screen
            infraredPixels = new byte[Width * Height * 4];
            
            // to save to a video helper buffer
            infraredPixelBuffer = new byte[Width * Height * 3];
   
        }
        public void setRecordingState(bool state)
        {
            infraredRecording = state;
            frameCount = 0;
        }
        public uint getBPP() // bytes per pixel
        {
            return this.infraredFrameDescription.BytesPerPixel;
        }
        public void Write()
        {
            while (true)
            {
                // Console.WriteLine("Depth");
                if (infraredBitmapBuffer.Count > 0)
                {
                    //Console.WriteLine(depthBitmapBuffer.Count);
                    this.infraredWriter.WriteVideoFrame(infraredBitmapBuffer.Dequeue());
                }
                else if (!infraredRecording)
                {
                    infraredWriter.Close();
                    Console.WriteLine("depth writer closed.");
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
            infraredVideoPath = path;
            bitRate = br;
            openVideoWriter();
        }
        public void openVideoWriter()
        {
            Accord.Math.Rational rationalFrameRate = new Accord.Math.Rational(30);
            infraredWriter = new VideoFileWriter();
            infraredWriter.Open(infraredVideoPath, Width, Height, rationalFrameRate, VideoCodec.MPEG4, bitRate);
            frameCount = 0;
        }
        public void InfraredFrameArrival(InfraredFrame df, double fps, ref WriteableBitmap infraredBitmap)
        {
            using (Microsoft.Kinect.KinectBuffer infraredBuffer = df.LockImageBuffer())
            {
                // verify data and write the new infrared frame data to the display bitmap
                if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                    (this.infraredFrameDescription.Width == infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == infraredBitmap.PixelHeight))
                {
                    this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size,ref infraredBitmap);


                    if (infraredRecording)
                    {
                        this.iBitmap = IRFrameToBitmap(df);
                        this.infraredBitmapBuffer.Enqueue(this.iBitmap);
                        System.GC.Collect();
                        this.frameCount++;
                        if (fps < 16.0)
                        {
                            Console.WriteLine("fps drop yaşandı");
                            this.infraredBitmapBuffer.Enqueue(this.iBitmap);
                            this.frameCount++;
                        }
                    }
                }
            }
        }
        private Bitmap IRFrameToBitmap(InfraredFrame frame)
        {
            System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;

            ushort[] infraredData = new ushort[frame.FrameDescription.LengthInPixels];
            byte[] pixelData = new byte[frame.FrameDescription.LengthInPixels * 4];

            frame.CopyFrameDataToArray(infraredData);

            for (int infraredIndex = 0; infraredIndex < infraredData.Length; infraredIndex++)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                pixelData[infraredIndex * 4] = intensity; // Blue
                pixelData[infraredIndex * 4 + 1] = intensity; // Green   
                pixelData[infraredIndex * 4 + 2] = intensity; // Red
                pixelData[infraredIndex * 4 + 3] = 255;
            }

            return UtilityClass.ByteArrayToBitmap(pixelData, this.Width, this.Height, format);
        }
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize, ref WriteableBitmap infraredBitmap)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            infraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)infraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            infraredBitmap.AddDirtyRect(new Int32Rect(0, 0, infraredBitmap.PixelWidth, infraredBitmap.PixelHeight));

            // unlock the bitmap
            infraredBitmap.Unlock();
        }
    }
}
