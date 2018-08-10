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

namespace KinectRecorder
{
    class InfraredHandler
    {
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        //private const float InfraredSourceScale = 0.75f;

        private const float InfraredOutputValueMinimum = 0.01f;

        private const float InfraredOutputValueMaximum = 1.0f;

        private const float InfraredSceneValueAverage = 0.08f;

        private const float InfraredSceneSD = 3.0f; // STD standart deviation

        private double MapInfraredToByte = (InfraredOutputValueMaximum - InfraredOutputValueMinimum) / 256;

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
        public bool show;

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

        public void SetShowState(bool state)
        {
            show = state;
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
        public void InfraredFrameArrival(InfraredFrame df, double fps, ref bool processed, WriteableBitmap infraredBitmap)
        {
            using (Microsoft.Kinect.KinectBuffer infraredBuffer = df.LockImageBuffer())
            {
                // verify data and write the new infrared frame data to the display bitmap
                if (((this.infraredFrameDescription.Width * this.infraredFrameDescription.Height) == (infraredBuffer.Size / this.infraredFrameDescription.BytesPerPixel)) &&
                    (this.infraredFrameDescription.Width == infraredBitmap.PixelWidth) && (this.infraredFrameDescription.Height == infraredBitmap.PixelHeight))
                {
                    ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);

                    processed = true;
                    if (infraredRecording)
                    {
                        this.iBitmap = IRFrameToBitmap(df);
                        this.infraredBitmapBuffer.Enqueue(this.iBitmap);
                        //System.GC.Collect();
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
            System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

            ushort[] infraredData = new ushort[frame.FrameDescription.LengthInPixels];
            byte[] pixelData = new byte[frame.FrameDescription.LengthInPixels * 3];

            frame.CopyFrameDataToArray(infraredData);

            for (int infraredIndex = 0; infraredIndex < infraredData.Length; infraredIndex++)
            {
                ushort ir = infraredData[infraredIndex];
                //byte intensity = (byte)(ir >> 8);

                pixelData[infraredIndex * 3] = (byte)(ir / 1000);// intensity; // Red
                pixelData[infraredIndex * 3 + 1] = (byte)((ir % 1000) / 100); // Green   
                pixelData[infraredIndex * 3 + 2] = (byte)(ir % 100); // Blue
            }

            return UtilityClass.ByteArrayToBitmap(pixelData, this.Width, this.Height, format);
        }
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // process the infrared data
            int pixelIndex = 0;
            for (int i = 0; i < (int)(infraredFrameDataSize / this.infraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                //backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
                ushort ir = frameData[i];
                float intensityRatio = (float)frameData[i] / InfraredSourceValueMaximum;
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneSD;
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                byte intensity = (byte)(intensityRatio * 255.0f);
                infraredPixels[pixelIndex++] = intensity; // Red
                infraredPixels[pixelIndex++] = intensity; // Green   
                infraredPixels[pixelIndex++] = intensity; // Blue
                infraredPixels[pixelIndex++] = 255; // alpha
            }


        }
    }
}
