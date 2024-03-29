﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using CsvHelper;
using System.Collections;
using System.Threading;

namespace KinectRecorder
{
    class SkeletonHandler
    {
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private readonly Pen previewBonePen = new Pen(Brushes.HotPink, 6);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;
        private DrawingGroup previewDrawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;
        private DrawingImage previewImageSource;
        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        // writing variables
        // skeleton data array
        private double[] skeletonDataArray;
        private int stride = 7;
        private Queue<double[]> skeletonBuffer = new Queue<double[]>();

        private bool skeletonRecording = false;
        private String skeletonFilePath;
        public UInt32 frameCount = 0;

        private double[] depthSkeleton;
        int skeletonCounter = 0;

        private CsvWriter csvWriter = null;
        private StreamWriter stream = null;

        private CsvReader csvReader = null;
        private StreamReader streamReader = null;

        static SkeletonHandler instance = new SkeletonHandler();

        private List<SkeletonData> skeletons;

        public static SkeletonHandler Instance
        {
            get { return instance; }
        }

        public void SkeletonHandlerSet(int width, int height, CoordinateMapper coordMap)
        {

            this.displayHeight = height;
            this.displayWidth = width;

            coordinateMapper = coordMap;

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            this.previewDrawingGroup = new DrawingGroup();
            this.previewImageSource = new DrawingImage(this.previewDrawingGroup);

        }

        public void openReader()
        {
            skeletonCounter = 0;
            streamReader = new StreamReader(skeletonFilePath);
            csvReader = new CsvReader(streamReader);

            skeletons = csvReader.GetRecords<SkeletonData>().ToList<SkeletonData>();
            
        }

        public void closeReader()
        {
            streamReader.Dispose();
            csvReader.Dispose();

            skeletons.Clear();
        }

        public void Read()
        {
            try
            {
                using (DrawingContext dc = this.previewDrawingGroup.Open())
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    depthSkeleton = skeletons[skeletonCounter].getDepthValues();

                    this.DrawPreviewBody(depthSkeleton, dc);

                    //this.DrawPreviewHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                    //this.DrawPreviewHand(body.HandRightState, jointPoints[JointType.HandRight], dc);



                    skeletonCounter++;
                }
            }
            catch (Exception e){
                Console.WriteLine("iskelet datası yok");
                Console.WriteLine(e);
            }
         
        }

        public void DrawPreviewBody(double[] depthValues, DrawingContext dc)
        {
            foreach (var bone in bones)
            {
                DrawBoneFromArray(depthValues, bone.Item1, bone.Item2, dc);
            }

            for (int i = 0; i < 25; i++)
            {
                Point point = new Point(depthValues[i * 2], depthValues[i * 2 + 1]);

                dc.DrawEllipse(this.trackedJointBrush, null, point, JointThickness, JointThickness);

            }
        }


        public DrawingImage getImageSource()
        {
            return this.imageSource;
        }

        public DrawingImage getPreviewImageSource()
        {
            return this.previewImageSource;
        }

        public void renderSkeleton(BodyFrameArrivedEventArgs args)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = args.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    int bodyCount = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            bodyCount++;
                            skeletonDataArray = new double[25 * 7];
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            //var values = UtilityClass.GetValues<JointType>();
                            //foreach (JointType jointType in values)
                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position); // point mapped to depth
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position); // points mapped to color

                                if (bodyCount == 1 && skeletonRecording)
                                {

                                    int i = (int)jointType;
                                    i *= stride;
                                    skeletonDataArray[i++] = position.X;
                                    skeletonDataArray[i++] = position.Y;
                                    skeletonDataArray[i++] = position.Z;
                                    skeletonDataArray[i++] = depthSpacePoint.X;
                                    skeletonDataArray[i++] = depthSpacePoint.Y;
                                    skeletonDataArray[i++] = colorSpacePoint.X;
                                    skeletonDataArray[i++] = colorSpacePoint.Y;
                                }
                                //System.Console.WriteLine(string.Format("body count {0}", bodyCount));
                            }

                            if (bodyCount == 1 && skeletonRecording)
                            {
                                skeletonBuffer.Enqueue(skeletonDataArray);
                                frameCount++;
                            }


                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }
  

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }


        private void DrawBoneFromArray(double[] jointDepthValues, JointType jointType0, JointType jointType1, DrawingContext drawingContext)
        {
            Point point0 = new Point(jointDepthValues[(int)jointType0 * 2], jointDepthValues[(int)(jointType0) * 2 + 1]);

            Point point1 = new Point(jointDepthValues[(int)jointType1 * 2], jointDepthValues[(int)(jointType1) * 2 + 1]);

            Pen drawPen = previewBonePen;

            //drawPen = drawingPen;
            
            drawingContext.DrawLine(drawPen, point0, point1);
        }



        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        public void SetFilePath(string path)
        {
            skeletonFilePath = path;
            OpenSkeletonWriter();
        }

        public void OpenSkeletonWriter()
        {
            freeWriters();
            stream = new StreamWriter(skeletonFilePath);
            csvWriter = new CsvWriter(stream);
            csvWriter.WriteHeader<SkeletonData>();
            csvWriter.NextRecord();
        }

        private void freeWriters()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
            if (csvWriter != null)
            {
                csvWriter.Dispose();
                csvWriter = null;

            }
            if (skeletonBuffer.Count != 0)
            {
                skeletonBuffer.Clear();
            }
        }

        public void setRecordingState(bool state)
        {
            skeletonRecording = state;
            frameCount = 0;
        }

        public void WriteAll()
        {

            Thread skeletonWriteThread = new Thread(new ThreadStart(Write));
            skeletonWriteThread.Priority = ThreadPriority.BelowNormal;
            skeletonWriteThread.Start();
            
        }

        private void Write()
        {

            int count = skeletonBuffer.Count;
            for (int i = 0; i < count; i++)
            {
                csvWriter.WriteRecord<SkeletonData>(new SkeletonData(i, skeletonBuffer.Dequeue()));
                csvWriter.NextRecord();
            }
            freeWriters();
        }

    }
}
