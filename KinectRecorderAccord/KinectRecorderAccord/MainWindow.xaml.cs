﻿using System;
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
        /// Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4; 

        // frameType handlers
        private SkeletonHandler skeletonHandler;
        private DepthHandler depthHandler;
        private ColorHandler colorHandler;
        private BodyIndexHandler bodyIHandler;

        //kinect sensor
        private KinectSensor kinectSensor = null;

        // Readers
        private ColorFrameReader colorFrameReader = null;
        private DepthFrameReader depthFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private BodyIndexFrameReader bodyIndexFrameReader = null;

        // writeable bitmaps
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;
        private WriteableBitmap bodyIndexBitmap = null;
        private DrawingImage skeletalImage = null;

        private String SkeletalDataPath;
        
        private bool isRecording = false;

        double fps;

        //private readonly object _lock = new object();

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
            colorHandler = new ColorHandler(this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra)); //, _lock);
            this.colorBitmap = new WriteableBitmap(colorHandler.Width, colorHandler.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
        }

        public void InitializeDepthStream()
        {
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();
            depthHandler = new DepthHandler(this.kinectSensor.DepthFrameSource.FrameDescription);
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
            bodyIHandler = new BodyIndexHandler(this.kinectSensor.BodyIndexFrameSource.FrameDescription);
            // create the bitmap to display
            this.bodyIndexBitmap = new WriteableBitmap(bodyIHandler.Width, bodyIHandler.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            
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

        public void recordBtn_Click(Object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                
                this.recordBtn.Content = "Start Recording";
                this.isRecording = false;
                depthHandler.setRecordingState(false);
                colorHandler.setRecordingState(false);
                bodyIHandler.setRecordingState(false);

                // start writing file process
                this.RecordingTextBlock.Text = "Recording Stoped";

                this.recordBtn.IsEnabled = false;
            }
            else
            {
                this.isRecording = true;
                depthHandler.setRecordingState(true);
                colorHandler.setRecordingState(true);
                bodyIHandler.setRecordingState(true);

                this.recordBtn.Content = "Stop Recording";
                
                // this will fire up the adding data to lists
                this.RecordingTextBlock.Text = "Recording";

                int bitRate = 12000000;

                colorHandler.SetVideoPath("C:/Users/AnılOsman/Desktop/testColor.avi", bitRate);
                depthHandler.SetVideoPath("C:/Users/AnılOsman/Desktop/testDepth.avi", bitRate);
                bodyIHandler.SetVideoPath("C:/Users/AnılOsman/Desktop/testBody.avi", bitRate);

                Thread colorWriteThread = new Thread(new ThreadStart(colorHandler.Write));
                Thread depthWriteThread = new Thread(new ThreadStart(depthHandler.Write));
                Thread bodyWriteThread = new Thread(new ThreadStart(bodyIHandler.Write));

                colorWriteThread.Priority = ThreadPriority.BelowNormal;
                depthWriteThread.Priority = ThreadPriority.BelowNormal;
                bodyWriteThread.Priority = ThreadPriority.BelowNormal;

                colorWriteThread.Start();
                depthWriteThread.Start();
                bodyWriteThread.Start();
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

                    int width = colorFrame.FrameDescription.Width;
                    int height = colorFrame.FrameDescription.Height;

                    colorHandler.ColorFrameArrival(colorFrame, ref colorBitmap, fps);

                    colorResolutionText.Content = string.Format("Resolution :  {0} x {1}", width.ToString(), height.ToString());
                    RecordingTextBlock.Text = string.Format("Recording: saved color frame count: {0}\n depth: {1}\n body: {2}",
                                                                colorHandler.frameCount,
                                                                depthHandler.frameCount,
                                                                bodyIHandler.frameCount);
                    
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

                    depthResolutionText.Content = string.Format("Resolution :  {0} x {1}   min: {2}  max: {3} BBP: {4}", 
                                                                depthHandler.Width.ToString(),
                                                                depthHandler.Height.ToString(),
                                                                minDepth,
                                                                maxDepth,
                                                                depthHandler.getBPP());
                    skeletalResolutionText.Content = string.Format("Resolution :  {0} x {1}", 
                                                                depthHandler.Width.ToString(),
                                                                depthHandler.Height.ToString());
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
                    bodyIHandler.BodyIndexFrameArrival(bodyIndexFrame, ref bodyIndexFrameProcessed, this.fps, bodyIndexBitmap);

                    indexResolutionText.Content = string.Format("Resolution :  {0} x {1}", 
                                            bodyIndexFrame.FrameDescription.Width.ToString(),
                                            bodyIndexFrame.FrameDescription.Height.ToString());
                }
            }

            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                bodyIHandler.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }


    }
}
