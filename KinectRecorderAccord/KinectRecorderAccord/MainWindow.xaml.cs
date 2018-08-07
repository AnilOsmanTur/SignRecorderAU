using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Kinect;
using System.Globalization;
using System.IO;
using System.ComponentModel;
using Accord.Video.FFMPEG;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace KinectRecorderAccord
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private SkeletonHandler skeletonHandler;

        private DepthHandler depthHandler;

        //Bodyindex variables

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
        private uint[] bodyIndexPixels = null;

        //kinect sensor
        private KinectSensor kinectSensor = null;

        // Readers
        private ColorFrameReader colorFrameReader = null;
        private DepthFrameReader depthFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private BodyIndexFrameReader bodyIndexFrameReader = null;

        // FrameDescriptors
        private FrameDescription colorFrameDescription = null;
        private FrameDescription bodyIndexFrameDescription = null;

        // writeable bitmaps
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;
        private WriteableBitmap bodyIndexBitmap = null;
        private DrawingImage skeletalImage = null;

        private Bitmap cBitmap;
        private Bitmap bBitmap;

        // bitmap var
        private Queue<System.Drawing.Bitmap> colorBitmapBuffer = new Queue<System.Drawing.Bitmap>();
        byte[] colorPixelBuffer;
        private Queue<System.Drawing.Bitmap> bodyBitmapBuffer = new Queue<System.Drawing.Bitmap>();
        byte[] bodyPixelBuffer;

        // writer class
        private VideoFileWriter colorWriter;
        private VideoFileWriter bodyWriter;

        private String colorVideoPath;
        private String SkeletalDataPath;
        private String BodyIndexPath;

        private bool isRecording = false;
        private UInt32 recordedColorFrameCount = 0;
        private UInt32 recordedBodyFrameCount = 0;

        double fps;

        private readonly object _lock = new object();

        public MainWindow()
        {

            // kinect init
            this.kinectSensor = KinectSensor.GetDefault();

            InitializeColorStream();
            InitializeDepthStream();
            InitializeSkeletalStream();
            InitializeBodyIndexStream();
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();

            this.DataContext = this; 
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            // this should be done after user input
            this.recordBtn.IsEnabled = true;
        }

        // initialize color stream
        public void InitializeColorStream()
        {
            
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.colorPixelBuffer = new byte[colorFrameDescription.Width * colorFrameDescription.Height* 4];
        }

        public void InitializeDepthStream()
        {
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            depthHandler = new DepthHandler(this.kinectSensor.DepthFrameSource.FrameDescription, _lock);

            this.depthBitmap = new WriteableBitmap(depthHandler.Width, depthHandler.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            
        }

        public void InitializeSkeletalStream()
        {
            skeletonHandler = new SkeletonHandler(this.kinectSensor.DepthFrameSource.FrameDescription.Width,
                                           this.kinectSensor.DepthFrameSource.FrameDescription.Height,
                                           this.kinectSensor.CoordinateMapper);

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.skeletalImage = skeletonHandler.getImageSource();

        }

        public void InitializeBodyIndexStream()
        {
            // open the reader for the depth frames
            this.bodyIndexFrameReader = this.kinectSensor.BodyIndexFrameSource.OpenReader();
            this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;
            this.bodyIndexPixels = new uint[this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height];
            // create the bitmap to display
            this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width, this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.bodyPixelBuffer = new byte[depthHandler.Width * depthHandler.Height];
        }
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSourceColor
        {
            get
            {
                return this.colorBitmap;
                //return null;
            }
        }
        public ImageSource ImageSourceDepth
        {
            get
            {
                return this.depthBitmap;
                //return null;
            }
        }
        public ImageSource ImageSourceSkeletal
        {
            get
            {
                return this.skeletalImage;
                //return null;
            }
        }
        public ImageSource ImageSourceBodyIndex
        {
            get
            {
                return this.bodyIndexBitmap;
                //return null;
            }
        }

        public void colorWrite()
        {
            while (true)
            {
                lock (_lock)
                {
                    Console.WriteLine("color");
                    
                    if (colorBitmapBuffer.Count > 0)
                    {
                        //Console.WriteLine(colorBitmapBuffer.Count);
                        this.colorWriter.WriteVideoFrame(colorBitmapBuffer.Dequeue());
                    }
                    else if (!isRecording)
                    {
                        colorWriter.Close();
                        Console.WriteLine("color writer closed.");
                        break;
                    }
                }
            }
        }



        private void bodyWrite()
        {
            while (true)
            {
                lock (_lock)
                {
                    Console.WriteLine("Body");
                    if (bodyBitmapBuffer.Count > 0)
                    {
                        //Console.WriteLine(bodyBitmapBuffer.Count);
                        this.bodyWriter.WriteVideoFrame(bodyBitmapBuffer.Dequeue());
                    }
                    else if (!isRecording)
                    {
                        bodyWriter.Close();
                        Console.WriteLine("body writer closed.");
                        break;
                    }
                }
            }
        }

        public void recordBtn_Click(Object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                
                this.recordBtn.Content = "Start Recording";
                this.isRecording = false;
                depthHandler.setRecordingState(false);
                // start writing file process
                this.RecordingTextBlock.Text = "Recording Stoped";

                this.recordBtn.IsEnabled = false;
            }
            else
            {
                int bitRate = 12000000;

                colorVideoPath = "C:/Users/AnılOsman/Desktop/testColor.avi";
                depthHandler.SetVideoPath("C:/Users/AnılOsman/Desktop/testDepth.avi", bitRate);
                BodyIndexPath = "C:/Users/AnılOsman/Desktop/testBody.avi";
                // writer init

                Accord.Math.Rational rationalFrameRate = new Accord.Math.Rational(30);
                colorWriter = new VideoFileWriter();
                colorWriter.Open(colorVideoPath, 1920, 1080, rationalFrameRate, VideoCodec.MPEG4, bitRate);
                
                bodyWriter = new VideoFileWriter();
                bodyWriter.Open(BodyIndexPath, 512, 424, rationalFrameRate, VideoCodec.MPEG4, bitRate);

                Thread colorWriteThread = new Thread(new ThreadStart(colorWrite));
                Thread depthWriteThread = new Thread(new ThreadStart(depthHandler.Write));
                Thread bodyWriteThread = new Thread(new ThreadStart(bodyWrite));

                colorWriteThread.Start();
                depthWriteThread.Start();
                bodyWriteThread.Start();
                
                this.recordBtn.Content = "Stop Recording";
                this.isRecording = true;
                depthHandler.setRecordingState(true);
                // this will fire up the adding data to lists
                this.RecordingTextBlock.Text = "Recording";
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("loaded");

            if (this.colorFrameReader != null)
            {
                this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            }
            if (this.depthFrameReader != null)
            {
                this.depthFrameReader.FrameArrived += this.Reader_DepthFrameArrived;
            }
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_SkeletalFrameArrived;
            }
            if (this.bodyIndexFrameReader != null)
            {
                Console.WriteLine("bodyIndex");
                this.bodyIndexFrameReader.FrameArrived += this.Reader_BodyIndexFrameArrived;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Console.WriteLine("Ciao!");
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
                this.bodyIndexFrameReader.Dispose();
                this.bodyIndexFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            saveCapture();
        }

        // a image save with bitmap encoder
        private void saveCapture()
        {
            if (this.colorBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = System.IO.Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusTextBlock.Text = string.Format("Screanshot saved at: {0}", path);
                }
                catch (IOException)
                {
                    this.StatusTextBlock.Text = string.Format("Screanshot chouldn't saved at: {0}", path);
                }
            }
        }
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {

                if (colorFrame != null)
                {
                    this.fps = 1 / colorFrame.ColorCameraSettings.FrameInterval.TotalSeconds;
                    colorFpsText.Content = "FPS :  " + fps.ToString("0.###");

                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        int width = this.colorFrameDescription.Width;
                        int height = this.colorFrameDescription.Height;
                        // verify data and write the new color frame data to the display bitmap
                        if ((width == this.colorBitmap.PixelWidth) && (height == this.colorBitmap.PixelHeight))
                        {
                            
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(width * height * 4),
                                ColorImageFormat.Bgra);
                            colorResolutionText.Content = string.Format("Resolution :  {0} x {1}", width.ToString(), height.ToString());
                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                            this.colorBitmap.Unlock();

                            colorFrame.CopyConvertedFrameDataToArray(colorPixelBuffer, ColorImageFormat.Bgra);

                            if (isRecording)
                            {
                                this.cBitmap = ByteArrayToBitmap(colorPixelBuffer, width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                this.colorBitmapBuffer.Enqueue(this.cBitmap);
                                System.GC.Collect();
                                //saveCapture();
                                this.recordedColorFrameCount++;
                                if (fps < 16.0)
                                {
                                    Console.WriteLine("fps drop yaşandı");
                                    this.colorBitmapBuffer.Enqueue(this.cBitmap);
                                    //saveCapture();
                                    this.recordedColorFrameCount++;
                                }
                            }
                       
                        }

                        this.RecordingTextBlock.Text = string.Format("Recording: saved color frame count: {0}\n depth: {1}\n body: {2}",
                                                                    this.recordedColorFrameCount,
                                                                    depthHandler.frameCount,
                                                                    this.recordedBodyFrameCount);
                       
                    }
                }
            }
        }
        
        Bitmap ByteArrayToBitmap(byte[] array, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {

            Bitmap bitmapFrame = new Bitmap(width, height, pixelFormat);

            BitmapData bitmapData = bitmapFrame.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);

            IntPtr intPointer = bitmapData.Scan0;
            Marshal.Copy(array, 0, intPointer, array.Length);

            bitmapFrame.UnlockBits(bitmapData);
            return bitmapFrame;

        }

        private void Reader_DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    ushort maxDepth = depthFrame.DepthMaxReliableDistance;
                    ushort minDepth = depthFrame.DepthMinReliableDistance;

                    depthHandler.DepthFrameArrival(depthFrame, ref depthFrameProcessed, this.fps, depthBitmap);

                    depthResolutionText.Content = string.Format("Resolution :  {0} x {1}   min: {2}  max: {3} BBP: {4}", depthHandler.Width.ToString(), depthHandler.Height.ToString(), minDepth, maxDepth, depthHandler.getBPP());
                    skeletalResolutionText.Content = string.Format("Resolution :  {0} x {1}", depthHandler.Width.ToString(), depthHandler.Height.ToString());
                }
            }

            if (depthFrameProcessed)
            {
                RenderDepthPixels();
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            depthBitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthHandler.depthPixels,
                depthBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }




        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusTextBlock.Text = this.kinectSensor.IsAvailable ? "Sensor is running" : "No Sensor";
        }

        private void Reader_SkeletalFrameArrived(object sender, BodyFrameArrivedEventArgs args)
        {
            this.skeletonHandler.renderSkeleton(args);
            //this.skeletalImage = skeletonHandler.getImageSource();
        }

        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyIndexFrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            bool bodyIndexFrameProcessed = false;

            using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                    {
                        int width = this.bodyIndexFrameDescription.Width;
                        int height = this.bodyIndexFrameDescription.Height;
                        // verify data and write the color data to the display bitmap
                        if (((width * height) == bodyIndexBuffer.Size) &&
                            (width == this.bodyIndexBitmap.PixelWidth) && (height == this.bodyIndexBitmap.PixelHeight))
                        {
                            indexResolutionText.Content = string.Format("Resolution :  {0} x {1}", width.ToString(), height.ToString());
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }

                        if (isRecording)
                        {

                            this.bBitmap = ByteArrayToBitmap(this.bodyPixelBuffer, width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                            this.bodyBitmapBuffer.Enqueue(this.bBitmap);
                            System.GC.Collect();
                            this.recordedBodyFrameCount++;
                            if (this.fps < 16.0)
                            {
                                Console.WriteLine("fps drop yaşandı");
                                this.bodyBitmapBuffer.Enqueue(this.bBitmap);
                                this.recordedBodyFrameCount++;
                            }
                        }
                    }
                }
            }

            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
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
                    
                    this.bodyPixelBuffer[i] = (byte) 255;
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this.bodyIndexPixels[i] = 0x00000000;
                    
                    this.bodyPixelBuffer[i] = (byte) 0;
                }
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }


    }
}
