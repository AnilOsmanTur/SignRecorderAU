using Accord.Video.FFMPEG;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        private Queue<byte[]> infraredBinaryBuffer = new Queue<byte[]>();
        //public byte[] infraredPixelBuffer;

        private String infraredVideoPath;
        private VideoFileWriter infraredWriter = new VideoFileWriter();
        //private VideoFileReader infraredReader = new VideoFileReader();
        private int bitRate = 1200000;


        private BinaryReader infraredReader;
        private BinaryWriter binaryWriter;
        private int readBinaryCount = 0;
        private int savedBinaryCount = 0;

        public UInt32 frameCount = 0;
        public long readerFrameCount = 0;
        public int Width, Height;

        private bool infraredRecording = false;
        public byte[] infraredPixels = null;
        public byte[] infraredPixelBuffer = null;
        public bool show;

        public byte[] infraredPreviewPixels = null;

        static InfraredHandler instance = new InfraredHandler();

        public static InfraredHandler Instance
        {
            get { return instance; }
        }
        public void InfraredHandlerSet(FrameDescription fd)
        {

            infraredFrameDescription = fd;
            Width = fd.Width;
            Height = fd.Height;

            // to show on screen
            infraredPixels = new byte[Width * Height * 2];
            infraredPixelBuffer = new byte[Width * Height * 2];

            // to save to a video helper buffer
            //infraredPixelBuffer = new byte[Width * Height * 3];

            infraredPreviewPixels = new byte[Width * Height * 2];

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

        public void openReader(string path)
        {
            try
            {
                infraredReader = new BinaryReader(new FileStream(path, FileMode.Open));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot open file.");
                return;
            }

            readerFrameCount = savedBinaryCount - 1;
        }

        public void closeReader()
        {
            infraredReader.Close();
            readerFrameCount = 0;
        }

        public void startReading()
        {
            readBinaryCount = 0;
        }

        public void Read(ref WriteableBitmap infraredPreview)
        {
            openReader(infraredVideoPath + "/" + readBinaryCount);

            this.infraredPreviewPixels = infraredReader.ReadBytes(Width * Height * 2);

            int pixelIndex = 0;
            for (int i = 0; i < (Width * Height); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                //backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
                ushort ir = (ushort)((ushort)infraredPreviewPixels[pixelIndex] + (ushort)(infraredPreviewPixels[pixelIndex + 1] << 8));
                float intensityRatio = (float)ir / InfraredSourceValueMaximum;
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneSD;
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                ushort intensity = (ushort)(intensityRatio * ushort.MaxValue);
                this.infraredPreviewPixels[pixelIndex++] = (byte)(intensity); // Red
                this.infraredPreviewPixels[pixelIndex++] = (byte)(intensity >> 8); // Green 
            }


            infraredPreview.WritePixels(
                new Int32Rect(0, 0, (int)(infraredPreview.Width), (int)(infraredPreview.Height)),
                this.infraredPreviewPixels,
                infraredPreview.PixelWidth * 2,
                0);

            readBinaryCount++;
            closeReader();
        }
    
        public void Write()
        {
            while (true)
            {
                // Console.WriteLine("Depth");
                if (infraredBinaryBuffer.Count > 0)
                {
                    openBinaryWriter();
                    //Console.WriteLine(depthBitmapBuffer.Count);
                    this.binaryWriter.Write(infraredBinaryBuffer.Dequeue());
                    savedBinaryCount++;
                    closeBinaryWriter();

                }
                else if (!infraredRecording)
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
            infraredVideoPath = path;
            infraredVideoPath = path + "/infrared";
            try
            {
                Directory.CreateDirectory(infraredVideoPath);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("The directory creating sprocess failed {0}", e.ToString());
            }

            frameCount = 0;
            savedBinaryCount = 0;
        }

        public void openBinaryWriter()
        {
            try
            {
                binaryWriter = new BinaryWriter(new FileStream(infraredVideoPath + "/" + savedBinaryCount, FileMode.Create));
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
                        this.infraredBinaryBuffer.Enqueue((byte[])(infraredPixelBuffer.Clone()));
                        this.frameCount++;
                        if (fps < 16.0)
                        {
                            Console.WriteLine("fps drop yaşandı");
                            this.infraredBinaryBuffer.Enqueue((byte[])(infraredPixelBuffer.Clone()));
                            this.frameCount++;
                        }
                       
                    }
                }
            }
        }
        /*private Bitmap IRFrameToBitmap(InfraredFrame frame)
        {
            System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;

            ushort[] infraredData = new ushort[frame.FrameDescription.LengthInPixels];
            byte[] pixelData = new byte[frame.FrameDescription.LengthInPixels * 2];

            frame.CopyFrameDataToArray(infraredData);

            for (int infraredIndex = 0; infraredIndex < infraredData.Length; infraredIndex++)
            {
                ushort ir = infraredData[infraredIndex];
                //byte intensity = (byte)(ir >> 8);

                pixelData[infraredIndex * 3] = (byte)(ir);// intensity; // Red
                pixelData[infraredIndex * 3 + 1] = (byte)(ir >> 8); // Green   
            }

            return UtilityClass.ByteArrayToBitmap(pixelData, this.Width, this.Height, format);
        }*/
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

                ushort intensity = (ushort)(intensityRatio * ushort.MaxValue);
                infraredPixels[pixelIndex++] = (byte) (intensity); // Red
                infraredPixels[pixelIndex++] = (byte) (intensity >> 8); // Green 

                infraredPixelBuffer[pixelIndex - 2] = (byte) (ir);
                infraredPixelBuffer[pixelIndex - 1] = (byte) (ir >> 8);
            }


        }
    }
}
