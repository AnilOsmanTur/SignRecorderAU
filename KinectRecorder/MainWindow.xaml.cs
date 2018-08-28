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
using System.Windows.Forms;
using CsvHelper;
using System.Collections;
using System.Linq;


namespace KinectRecorder
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
        private InfraredHandler infraredHandler;

        //kinect sensor
        private KinectSensor kinectSensor = null;

        // Readers
        private ColorFrameReader colorFrameReader = null;
        private DepthFrameReader depthFrameReader = null;
        private BodyFrameReader bodyFrameReader = null;
        private BodyIndexFrameReader bodyIndexFrameReader = null;
        private InfraredFrameReader infraredFrameReader = null;

        // writeable bitmaps
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap bodyIndexBitmap = null;
        private DrawingImage skeletalImage = null;
        private WriteableBitmap infraredDepthBitmap = null;


        private bool isRecording = false;

        // save check box swithes
        private bool colorSave = true;
        private bool depthSave = true;
        private bool bodySave = true;
        private bool skeletonSave = true;
        private bool infraredSave = true;

        double fps;

        private bool showInfrared = false;
        
        private string fileBasePath;
        private string folderPath = null;
        private int repeatNumber = 0;
        private string basePath = null;

        private List<User> users;
        private List<Word> words;

        private User selectedUser;
        private Word selectedWord;

        private bool isTutorial = false;

        private StreamWriter streamHeader;
        private CsvWriter headerWriter;
        //private HeaderUnit headerUnit;

        private StreamWriter streamWord;
        private CsvWriter wordWriter;

        private StreamWriter streamUser;
        private CsvWriter userWriter;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private bool previewToRecord = true;
        

        public MainWindow()
        {

            // kinect init
            InitKinect();

            this.DataContext = this; 
            this.InitializeComponent();

        }
        private void InitKinect()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();
            InitializeColorStream();
            InitializeDepthStream();
            InitializeSkeletalStream();
            InitializeBodyIndexStream();
            InitializeInfraredStream();
            this.KinectFrameArrivalBindings();
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
            this.infraredDepthBitmap = new WriteableBitmap(depthHandler.Width, depthHandler.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            depthHandler.SetShowState(true);
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

        public void InitializeInfraredStream()
        {
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();
            infraredHandler = new InfraredHandler(this.kinectSensor.InfraredFrameSource.FrameDescription);
            //this.indraredBitmap = new WriteableBitmap(infraredHandler.Width, infraredHandler.Height, 96.0, 96.0, PixelFormats.Gray32Float, null);
            infraredHandler.SetShowState(false);
        }

        private void KinectFrameArrivalBindings()
        {
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
                this.bodyIndexFrameReader.FrameArrived += this.Reader_BodyIndexFrameArrived;
            }
            if (this.infraredFrameReader != null)
            {
                this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;
            }
        }

        private void KinectClose()
        {
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
                this.infraredFrameReader.Dispose();
                this.infraredFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Console.WriteLine("Ciao!");
            KinectClose();
            if(headerWriter != null)
            {
                headerWriter.Dispose();
                streamHeader.Dispose();
            }

            if(userWriter != null)
            {
                streamWord.Dispose();
                streamUser.Dispose();
                
                wordWriter.Dispose();
                userWriter.Dispose();
            }
            
        }
        

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

                return this.infraredDepthBitmap;
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
                previewBtn.IsEnabled = true;

                this.recordBtn.IsEnabled = false;
                this.recordBtn.Content = "Start Recording";
                this.RecordingTextBlock.Text = "Recording Stoped";
                this.isRecording = false;

                this.StatusTextBlock.Text = string.Format("Saved frame counts\n color: {0}    depth: {1}    body: {2}\n skeleton: {3}    infrared: {4}",
                                                                colorHandler.frameCount,
                                                                depthHandler.frameCount,
                                                                bodyIHandler.frameCount,
                                                                skeletonHandler.frameCount,
                                                                infraredHandler.frameCount);
                depthHandler.setRecordingState(false);
                colorHandler.setRecordingState(false);
                bodyIHandler.setRecordingState(false);
                skeletonHandler.setRecordingState(false);
                infraredHandler.setRecordingState(false);

                // writing can be done in one for csv file
                skeletonHandler.WriteAll();
            }
            else
            {
                previewBtn.IsEnabled = false;

                this.isRecording = true;

                CreatePaths();

                writerHelper();
                
                this.recordBtn.Content = "Stop Recording";
                
                // this will fire up the adding data to lists
               
                if (!colorSave && !depthSave && !bodySave && !skeletonSave)
                {
                    this.RecordingTextBlock.Text = "No data checked. Nothing will be saved!";
                }
                else
                {
                    this.RecordingTextBlock.Text = "Recording";
                }

                
                
            }
        }

        private void writerHelper()
        {
            int bitRate = 12000000;
            if (colorSave)
            {
                colorHandler.setRecordingState(true);
                colorHandler.SetVideoPath(fileBasePath + "_Color.avi", bitRate);
                Thread colorWriteThread = new Thread(new ThreadStart(colorHandler.Write));
                colorWriteThread.Priority = ThreadPriority.BelowNormal;
                colorWriteThread.Start();
            }
            else
            {
                colorHandler.setRecordingState(false);
            }

            if (depthSave)
            {
                depthHandler.setRecordingState(true);
                depthHandler.SetVideoPath(fileBasePath + "_Depth.avi", bitRate);
                Thread depthWriteThread = new Thread(new ThreadStart(depthHandler.Write));
                depthWriteThread.Priority = ThreadPriority.BelowNormal;
                depthWriteThread.Start();
            }
            else
            {
                depthHandler.setRecordingState(false);
            }

            if (bodySave)
            {
                bodyIHandler.setRecordingState(true);
                bodyIHandler.SetVideoPath(fileBasePath + "_Body.avi", bitRate);
                Thread bodyWriteThread = new Thread(new ThreadStart(bodyIHandler.Write));
                bodyWriteThread.Priority = ThreadPriority.BelowNormal;
                bodyWriteThread.Start();

            }
            else
            {
                bodyIHandler.setRecordingState(false);
            }

            if (skeletonSave)
            {
                skeletonHandler.setRecordingState(true);
                skeletonHandler.SetFilePath(fileBasePath+"_Skeleton.csv");
            }
            else
            {
                skeletonHandler.setRecordingState(false);
            }
            if (infraredSave)
            {
                infraredHandler.setRecordingState(true);
                infraredHandler.SetVideoPath(fileBasePath + "_Infrared.avi", bitRate);
                Thread infraredWriteThread = new Thread(new ThreadStart(infraredHandler.Write));
                infraredWriteThread.Priority = ThreadPriority.BelowNormal;
                infraredWriteThread.Start();
            }
            else
            {
                infraredHandler.setRecordingState(false);
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
                    if (isRecording)
                    {
                        RecordingTextBlock.Text = string.Format("Recording: saved frame counts\n color: {0}    depth: {1}    body: {2}\n skeleton: {3}    infrared: {4}",
                                                                colorHandler.frameCount,
                                                                depthHandler.frameCount,
                                                                bodyIHandler.frameCount,
                                                                skeletonHandler.frameCount,
                                                                infraredHandler.frameCount);
                    }

                }
            }
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

                    depthHandler.DepthFrameArrival(depthFrame, ref depthFrameProcessed, this.fps, infraredDepthBitmap);

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

            if (depthFrameProcessed && depthHandler.show)
            {
                RenderDepthPixels();
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            infraredDepthBitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, infraredDepthBitmap.PixelWidth, infraredDepthBitmap.PixelHeight),
                depthHandler.depthPixels,
                infraredDepthBitmap.PixelWidth * (int)BytesPerPixel,
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

        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            bool infraredProcessed = false;
            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    infraredHandler.InfraredFrameArrival(infraredFrame, this.fps, ref infraredProcessed, infraredDepthBitmap);
                }
            }
            if (infraredProcessed && infraredHandler.show)
            {
                infraredDepthBitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, infraredDepthBitmap.PixelWidth, infraredDepthBitmap.PixelHeight),
                    infraredHandler.infraredPixels,
                    infraredDepthBitmap.PixelWidth * 4,
                    0);

            }
        }
        

        // color data check box bindings
        private void ColorSaveCheck_Checked(object sender, RoutedEventArgs e)
        {
            colorSave = true;
        }
        private void ColorSaveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            colorSave = false;
        }
        // depth data check box bindings
        private void DepthSaveCheck_Checked(object sender, RoutedEventArgs e)
        {
            depthSave = true;
        }
        private void DepthSaveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            depthSave = false;
        }
        // body index data check box bindings
        private void BodySaveCheck_Checked(object sender, RoutedEventArgs e)
        {
            bodySave = true;
        }
        private void BodySaveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            bodySave = false;
        }
        // skeletal data check box bindings
        private void SkeletonSaveCheck_Checked(object sender, RoutedEventArgs e)
        {
            skeletonSave = true;
        }
        private void SkeletonSaveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            skeletonSave = false;
        }
        // infrared data check box bindings
        private void InfraredSaveCheck_Checked(object sender, RoutedEventArgs e)
        {
            infraredSave = true;
        }
        private void InfraredSaveCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            infraredSave = false;
        }

        private void FileBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                folderPath = folderBrowserDialog1.SelectedPath;
                selectedFolderText.Text = folderPath;

                openHeader();
                readFile();
                fillCombos();
                openCsvWriters();

                addUserBtn.IsEnabled = true;
                addWordBtn.IsEnabled = true;
            }
            else
            {
                selectedFolderText.Text = "Please! Select a directory. Please!!";
            }
            
        }

        private void openCsvWriters()
        {

            if (File.Exists(folderPath + "/sign_app_words.csv"))
            {
                streamWord = new StreamWriter(folderPath + "/sign_app_words.csv", true);
                wordWriter = new CsvWriter(streamWord);
            }
            else
            {
                streamWord = new StreamWriter(folderPath +"/sign_app_words.csv");
                wordWriter = new CsvWriter(streamWord);

                wordWriter.WriteHeader<HeaderUnit>();
                wordWriter.NextRecordAsync();
                streamWord.FlushAsync();
            }

            if (File.Exists(folderPath + "/sign_app_users.csv"))
            {
                streamUser = new StreamWriter(folderPath + "/sign_app_users.csv", true);
                userWriter = new CsvWriter(streamUser);
            }
            else
            {
                streamUser = new StreamWriter(folderPath + "/sign_app_users.csv");
                userWriter = new CsvWriter(streamUser);

                userWriter.WriteHeader<User>();
                userWriter.NextRecordAsync();
                streamUser.FlushAsync();
            }

        }

        private void repeatNumberText_TextChanged(object sender, TextChangedEventArgs e)
        {

            String tmp = repeatNumberText.Text;
            foreach (char c in repeatNumberText.Text.ToCharArray())
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(c.ToString(), "^[0-9]*$"))
                {
                    tmp = tmp.Replace(c.ToString(), "");
                }
            }
            repeatNumberText.Text = tmp;
            if (!Int32.TryParse(tmp, out repeatNumber))
            {
                System.Console.WriteLine(string.Format("string to int parse failed! num {0}", repeatNumber));
                repeatNumber = 0;
                repeatNumberText.Text = "";
            }
            checkReady();
        }

        private void openHeader()
        {
            if (File.Exists(folderPath + "/sign_app_header.csv"))
            {
                streamHeader = new StreamWriter(folderPath + "/sign_app_header.csv", true);
                headerWriter = new CsvWriter(streamHeader);
            }
            else
            {
                streamHeader = new StreamWriter(folderPath + "/sign_app_header.csv");
                headerWriter = new CsvWriter(streamHeader);
                
                headerWriter.WriteHeader<HeaderUnit>();
                headerWriter.NextRecordAsync();
                streamHeader.FlushAsync();
            }
            
        }

        private void readFile()
        {
            StreamReader streamUsers = new StreamReader(folderPath + "/sign_app_users.csv");
            CsvReader readerUsers = new CsvReader(streamUsers);

            StreamReader streamWords = new StreamReader(folderPath + "/sign_app_words.csv");
            CsvReader readerWords = new CsvReader(streamWords);

            users = readerUsers.GetRecords<User>().ToList<User>();
            words = readerWords.GetRecords<Word>().ToList<Word>();

            readerUsers.Dispose();
            streamUsers.Dispose();

            readerWords.Dispose();
            streamWords.Dispose();
        }

        private void fillCombos()
        {

            foreach(User user in users)
            {
                userCombo.Items.Add(user.userName);
            }
            
            foreach(Word word in words)
            {
                wordCombo.Items.Add(word.word);
            }
        }

        private void userCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = users[userCombo.SelectedIndex];
            //System.Console.WriteLine(string.Format("user: {0}, index: {1}, name: {2}", selectedUser.id, userCombo.SelectedIndex, selectedUser.userName));
            checkReady();
        }

        private void wordCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedWord = words[wordCombo.SelectedIndex];
            checkReady();
        }
        
        private void TutorialCheck_Checked(object sender, RoutedEventArgs e)
        {
            isTutorial = true;
        }

        private void TutorialCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            isTutorial = false;
        }

        private void CreatePaths()
        {
            String timeStamp = GetTimestamp(DateTime.Now);
            if (isTutorial)
            {
                basePath = folderPath + "\\" + selectedWord.id.ToString() + "\\Tutorial";                           
            }
            else
            {
                basePath = folderPath + "\\" + selectedWord.id.ToString() + "\\Samples";
            }

            fileBasePath = basePath + "\\" + selectedUser.id.ToString() + "_" + repeatNumber + "_" + timeStamp;

            headerWriter.WriteRecord<HeaderUnit>(new HeaderUnit(selectedWord.id, selectedUser.id, repeatNumber, (isTutorial)? 1 : 0, fileBasePath));
            headerWriter.NextRecordAsync();
            streamHeader.FlushAsync();
            try
            {
                Directory.CreateDirectory(basePath);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("The directory creating sprocess failed {0}", e.ToString());
            }
        }

        private void checkReady()
        {
            if (userCombo.SelectedIndex != -1 && wordCombo.SelectedIndex != -1 && repeatNumber > 0)
            {
                System.Console.WriteLine(wordCombo.SelectedIndex.ToString());
                recordBtn.IsEnabled = true;
            }
            else
            {
                recordBtn.IsEnabled = false;
            }
        }


        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        private void depthInfraSwitch_Click(object sender, RoutedEventArgs e)
        {
            if(showInfrared)
            {
                showInfrared = false;
                depthInfraSwitch.Content = "Depth";
                depthHandler.SetShowState(true);
                infraredHandler.SetShowState(false);
            }
            else
            {
                showInfrared = true; ;
                depthInfraSwitch.Content = "Infrared";
                depthHandler.SetShowState(false);
                infraredHandler.SetShowState(true);
            }
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (previewToRecord)
            {
                previewToRecord = false;
                previewBtn.Content = "record";
                this.kinectSensor.Close();
            }
            else
            {
                previewBtn.Content = "preview";
                previewToRecord = true;
                this.kinectSensor.Open();
            }
            
        }

        private void addWordBtn_Click(object sender, RoutedEventArgs e)
        {
            AddWord dlg = new AddWord();
            dlg.Owner = this;

            dlg.ShowDialog();

            // Process data entered by user if dialog box is accepted
            if (dlg.DialogResult == true)
            {

                if (users.Count > 0)
                {
                    Word word = words[words.Count - 1];
                    word.id = word.id + 1;
                    word.word = dlg.wordName;

                    words.Add(word);
                    
                    // combo box append
                    wordCombo.Items.Add(word.word);

                    wordWriter.WriteRecord<Word>(word);
                    wordWriter.NextRecordAsync();
                    streamWord.FlushAsync();
                }

            }

        }

        private void addUserBtn_Click(object sender, RoutedEventArgs e)
        {

            AddUser dlg = new AddUser();
            dlg.Owner = this;

            dlg.ShowDialog();

            // Process data entered by user if dialog box is accepted
            if (dlg.DialogResult == true)
            {

                if (users.Count > 0)
                {
                    User user = users[users.Count - 1];
                    user.id = user.id + 1;
                    user.userName = dlg.userName;

                    users.Add(user);

                    // combo box append
                    userCombo.Items.Add(user.userName);

                    userWriter.WriteRecord<User>(user);
                    userWriter.NextRecordAsync();
                    streamUser.FlushAsync();
                }

            }
        }

        
    }
}
