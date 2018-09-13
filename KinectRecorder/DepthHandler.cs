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
using System.IO;

namespace KinectRecorder
{
    class DepthHandler
    {
        //Depth variables
        private double MapDepthToByte = 8000 / 256;
        public byte[] depthPixels = null; // wee have to use byte

        /// Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        static DepthHandler instance = new DepthHandler();

        private Queue<byte[]> depthBinaryBuffer = new Queue<byte[]>();
        public byte[] depthPixelBuffer;

        private String depthVideoPath;
        private BinaryReader depthReader;

        private BinaryWriter binaryWriter;

        private int savedBinaryCount = 0;
        private int readBinaryCount = 0;
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
            depthPixels = new byte[Width * Height * 2];
            
            // to save to a video helper buffer
            depthPixelBuffer = new byte[Width * Height * 2];

            depthPreviewPixels = new byte[Width * Height * 2];
        }

        public void SetShowState(bool state)
        {
            show = state;
        }
        public void openReader(string path)
        {
            try
            {
                depthReader = new BinaryReader(new FileStream(path, FileMode.Open));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot open file.");
                return;
            }

            readerFrameCount = savedBinaryCount - 1;        }

        public void closeReader()
        {
            depthReader.Close();
            readerFrameCount = 0;
        }

        public void startReading()
        {
            readBinaryCount = 0;
        }

        
        public void Read(ref WriteableBitmap depthPreview)
        {

            openReader(depthVideoPath+"/"+readBinaryCount);

            this.depthPreviewPixels = depthReader.ReadBytes(Width * Height * 2);


            int stride = 2, j, k;
            for (int i = 0; i < Width * Height; ++i)
            {
                k = stride * i;
                // Get the depth for this pixel
                ushort depth = (ushort)((ushort)depthPreviewPixels[k] + (ushort)(depthPreviewPixels[k + 1] << 8));
                
                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).

                if(depth != 0)
                {
                    depth = (ushort)(((depth - 500) / (float)4000) * (ushort.MaxValue - 1));

                    this.depthPreviewPixels[k] = (byte)(depth);
                    this.depthPreviewPixels[k + 1] = (byte)(depth >> 8);
                }
               
            }



            depthPreview.WritePixels(
                new Int32Rect(0, 0, (int)(depthPreview.Width),(int)(depthPreview.Height)),
                this.depthPreviewPixels,
                depthPreview.PixelWidth*2,
                0);

            readBinaryCount++;
            closeReader();
        }
        public void Write()
        {
            while (true)
            {
                // Console.WriteLine("Depth");
                if (depthBinaryBuffer.Count > 0)
                {
                    openBinaryWriter();
                    //Console.WriteLine(depthBitmapBuffer.Count);
                    this.binaryWriter.Write(depthBinaryBuffer.Dequeue());
                    savedBinaryCount++;
                    closeBinaryWriter();

                }
                else if (!depthRecording)
                {
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
            depthVideoPath = path + "/depth";
            try
            {
                Directory.CreateDirectory(depthVideoPath);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("The directory creating process failed {0}", e.ToString());
            }

            frameCount = 0;
            savedBinaryCount = 0;
        }

        public void openBinaryWriter()
        {
            try
            {
                binaryWriter = new BinaryWriter(new FileStream(depthVideoPath+"/"+savedBinaryCount, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create directory.");
                return;
            }
        }

        public void closeBinaryWriter()
        {
            binaryWriter.Close();
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
                        this.depthBinaryBuffer.Enqueue((byte[])(depthPixelBuffer.Clone()));
                        this.frameCount++;
                        
                        if (fps < 16.0)
                        {
                            garbageCount++;
                            Console.WriteLine("fps drop yaşandı");
                            this.depthBinaryBuffer.Enqueue((byte[])(depthPixelBuffer.Clone()));
                            this.frameCount++;
                        }
                        /*if(garbageCount > 500)
                        {
                            System.GC.Collect();
                            garbageCount = 0;
                        }*/
                        
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
            int stride = 2, strideWrite = 2, j, k;
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];
                j = stride * i;
                k = strideWrite * i;
                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                if (depth >= minDepth && depth <= maxDepth)
                {
                    ushort depthShow = (ushort)(((depth-500) / (float)4000) * (ushort.MaxValue-1));
                    this.depthPixels[j] = (byte)(depthShow);
                    this.depthPixels[j + 1] = (byte)(depthShow >> 8);

                    this.depthPixelBuffer[k] = (byte)(depth);
                    this.depthPixelBuffer[k + 1] = (byte)(depth >> 8); 


                }
                else if (depth < minDepth)
                {
                    this.depthPixels[j] = (byte)0;
                    this.depthPixels[j + 1] = (byte)0;

                    this.depthPixelBuffer[k] = (byte)0; //(frameData[i] / 1000);
                    this.depthPixelBuffer[k+1] = (byte)0; //((frameData[i] % 1000) / 100);
                }
                else
                {
                    this.depthPixels[j] = (byte)255;
                    this.depthPixels[j + 1] = (byte)255;

                    this.depthPixelBuffer[k] = (byte)255; //(frameData[i] / 1000);
                    this.depthPixelBuffer[k+1] = (byte)255; //((frameData[i] % 1000) / 100);
                }
            }
        }

        

    }
}
