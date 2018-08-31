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
using System.Windows.Media;
using System.Windows;

namespace KinectRecorder
{
    class DepthHandler
    {
        //Depth variables
        private double MapDepthToByte = 8000 / 256;
        public byte[] depthPixels = null; // wee have to use byte

        /// Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        private Bitmap dBitmap;

        static DepthHandler instance = new DepthHandler();

        private Queue<Bitmap> depthBitmapBuffer = new Queue<Bitmap>();
        public byte[] depthPixelBuffer;

        private String depthVideoPath;
        private VideoFileWriter depthWriter = new VideoFileWriter();
        private VideoFileReader depthReader = new VideoFileReader();
        private int bitRate = 1200000;

        public UInt32 frameCount = 0;
        public long readerFrameCount = 0;

        private FrameDescription depthFrameDescription = null;

        public int Width, Height;
        
        private bool depthRecording = false;
        public bool show;

        private int garbageCount = 0;

        public static DepthHandler Instance
        {
            get { return instance; }
        }

        public byte[] depthPreviewPixels;
        public void DepthHandlerSet(FrameDescription fd)
        {
            depthFrameDescription = fd;
            Width = fd.Width;
            Height = fd.Height;

            // to show on screen
            depthPixels = new byte[Width * Height * 4];
            
            // to save to a video helper buffer
            depthPixelBuffer = new byte[Width * Height * 3];

            depthPreviewPixels = new byte[Width * Height*2];
        }

        public void SetShowState(bool state)
        {
            show = state;
        }
        public void openReader()
        {
            depthReader.Open(depthVideoPath);
            readerFrameCount = depthReader.FrameCount;
        }

        public void closeReader()
        {
            depthReader.Close();
            readerFrameCount = 0;
        }

        
        public void Read(ref WriteableBitmap depthPreview)
        {
            Bitmap img = depthReader.ReadVideoFrame();
            
            int depth = 0;
            int k = 0;
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++ )
                {
                    
                    System.Drawing.Color c = img.GetPixel(j, i);
                    depth = c.R * 1000 + c.G * 100 + c.B;

                    //depth = (int)((float) depth / 4000 * ushort.MaxValue);
                    this.depthPreviewPixels[k++] = (byte)(depth >> 8);
                    this.depthPreviewPixels[k++] = (byte)(depth);
                    
                }
                
            }

            depthPreview.WritePixels(
                new Int32Rect(0, 0, img.Width, img.Height),
                this.depthPreviewPixels,
                depthPreview.PixelWidth*2,
                0);
        }
        public void Write()
        {
            while (true)
            {
                // Console.WriteLine("Depth");
                if (depthBitmapBuffer.Count > 0)
                {
                    //Console.WriteLine(depthBitmapBuffer.Count);
                    this.depthWriter.WriteVideoFrame(depthBitmapBuffer.Dequeue());
                }
                else if (!depthRecording)
                {
                    depthWriter.Close();
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
            depthVideoPath = path;
            bitRate = br;
            openVideoWriter();
        }

        public void openVideoWriter()
        {
            Accord.Math.Rational rationalFrameRate = new Accord.Math.Rational(30);
            
            depthWriter.Open(depthVideoPath, Width, Height, rationalFrameRate, VideoCodec.MPEG4, bitRate);
            frameCount = 0;
        }

        public void setRecordingState(bool state)
        {
            depthRecording = state;
            frameCount = 0;
        }


        public uint getBPP() // bytes per pixel
        {
            return this.depthFrameDescription.BytesPerPixel;
        }

        public void DepthFrameArrival(DepthFrame df, ref bool frameProcessed, double fps, WriteableBitmap depthBitmap)
        {
            // the fastest way to process the body index data is to directly access 
            // the underlying buffer
            using (Microsoft.Kinect.KinectBuffer depthBuffer = df.LockImageBuffer())
            {
                // verify data and write the color data to the display bitmap
                if (((df.FrameDescription.Width * df.FrameDescription.Height) == (depthBuffer.Size / getBPP())) &&
                    (df.FrameDescription.Width == depthBitmap.PixelWidth) && (df.FrameDescription.Height == depthBitmap.PixelHeight))
                {
                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                    // we are setting maxDepth to the extreme potential depth threshold
                    //ushort maxDepth = ushort.MaxValue;

                    // If you wish to filter by reliable depth distance, uncomment the following line:
                    ushort maxDepth = df.DepthMaxReliableDistance;
                    ushort minDepth = df.DepthMinReliableDistance;

                    ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, minDepth, maxDepth);

                    frameProcessed = true;

                    // depthFrame.CopyFrameDataToArray(this.depthPixelBuffer); done in processing function
                    if (depthRecording)
                    {
                        garbageCount++;
                        this.dBitmap = UtilityClass.ByteArrayToBitmap(this.depthPixelBuffer, Width, Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        this.depthBitmapBuffer.Enqueue(this.dBitmap);
                        this.frameCount++;
                        if (fps < 16.0)
                        {
                            garbageCount++;
                            Console.WriteLine("fps drop yaşandı");
                            this.depthBitmapBuffer.Enqueue(this.dBitmap);
                            this.frameCount++;
                        }
                        if(garbageCount > 200)
                        {
                            System.GC.Collect();
                            garbageCount = 0;
                        }
                        
                    }

                }

            }

        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            this.MapDepthToByte = (maxDepth - minDepth) / 255.0;
            //System.Console.WriteLine(MapDepthToByte);

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            int stride = 0, strideWrite = 0, j, k;
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];
                j = stride + i;
                k = strideWrite + i;
                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                if (depth >= minDepth && depth <= maxDepth)
                {
                    this.depthPixels[j] = (byte)(depth / MapDepthToByte);
                    this.depthPixels[j + 1] = (byte)(depth / MapDepthToByte);
                    this.depthPixels[j + 2] = (byte)(depth / MapDepthToByte);

                    this.depthPixelBuffer[k] = (byte)(frameData[i] / 1000);
                    this.depthPixelBuffer[k+1] = (byte)((frameData[i] % 1000) / 100);
                    this.depthPixelBuffer[k+2] = (byte)(frameData[i] % 100);

                    //this.depthPixelBuffer[k] = (byte)(depth << 8);
                    //this.depthPixelBuffer[k+1] = (byte)(depth << 8);
                    //this.depthPixelBuffer[k+2] = (byte)(0);

                    //k+3

                }
                else if (depth < minDepth)
                {
                    this.depthPixels[j] = (byte)160;
                    this.depthPixels[j + 1] = (byte)0;
                    this.depthPixels[j + 2] = (byte)0;

                    this.depthPixelBuffer[k] = (byte)0; //(frameData[i] / 1000);
                    this.depthPixelBuffer[k+1] = (byte)0; //((frameData[i] % 1000) / 100);
                    this.depthPixelBuffer[k+2] = (byte)0; //(frameData[i] % 100);
                }
                else
                {
                    this.depthPixels[j] = (byte)0;
                    this.depthPixels[j + 1] = (byte)0;
                    this.depthPixels[j + 2] = (byte)160;

                    this.depthPixelBuffer[k] = (byte)0; //(frameData[i] / 1000);
                    this.depthPixelBuffer[k+1] = (byte)0; //((frameData[i] % 1000) / 100);
                    this.depthPixelBuffer[k+2] = (byte)0; //(frameData[i] % 100);
                }
                stride += 3;
                strideWrite += 2;
            }
        }

        

    }
}
