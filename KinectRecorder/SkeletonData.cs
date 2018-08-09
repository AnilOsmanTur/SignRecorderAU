using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectRecorder
{
    class SkeletonData
    {
        // to check the frame
        public int frameID { get; set; }

        // first 3 are camera space points
        //     Base of the spine.
        public double SpineBase_X { get; set; } // = 0,
        public double SpineBase_Y { get; set; }
        public double SpineBase_Z { get; set; }
        // depth space mapped points
        public double SpineBase_depth_X { get; set; }
        public double SpineBase_depth_Y { get; set; }
        // color space mapped points
        public double SpineBase_color_X { get; set; }
        public double SpineBase_color_Y { get; set; }
        
        //     Middle of the spine.
        public double SpineMid_X { get; set; }  // = 1,
        public double SpineMid_Y { get; set; }
        public double SpineMid_Z { get; set; }
        
        public double SpineMid_depth_X { get; set; }
        public double SpineMid_depth_Y { get; set; }

        public double SpineMid_color_X { get; set; }
        public double SpineMid_color_Y { get; set; }
        
        //     Neck.
        public double Neck_X { get; set; } // = 2,
        public double Neck_Y { get; set; }
        public double Neck_Z { get; set; }

        public double Neck_depth_X { get; set; }
        public double Neck_depth_Y { get; set; }

        public double Neck_color_X { get; set; }
        public double Neck_color_Y { get; set; }
        
        //     Head.
        public double Head_X { get; set; } // = 3,
        public double Head_Y { get; set; }
        public double Head_Z { get; set; }
        
        public double Head_depth_X { get; set; }
        public double Head_depth_Y { get; set; }

        public double Head_color_X { get; set; }
        public double Head_color_Y { get; set; }

        //     Left shoulder.
        public double ShoulderLeft_X { get; set; } // = 4,
        public double ShoulderLeft_Y { get; set; }
        public double ShoulderLeft_Z { get; set; }

        public double ShoulderLeft_depth_X { get; set; }
        public double ShoulderLeft_depth_Y { get; set; }

        public double ShoulderLeft_color_X { get; set; }
        public double ShoulderLeft_color_Y { get; set; }

        //     Left elbow.
        public double ElbowLeft_X { get; set; } // = 5,
        public double ElbowLeft_Y { get; set; }
        public double ElbowLeft_Z { get; set; }

        public double ElbowLeft_depth_X { get; set; }
        public double ElbowLeft_depth_Y { get; set; }

        public double ElbowLeft_color_X { get; set; }
        public double ElbowLeft_color_Y { get; set; }

        //     Left wrist.
        public double WristLeft_X { get; set; } // = 6,
        public double WristLeft_Y { get; set; }
        public double WristLeft_Z { get; set; }

        public double WristLeft_depth_X { get; set; }
        public double WristLeft_depth_Y { get; set; }

        public double WristLeft_color_X { get; set; }
        public double WristLeft_color_Y { get; set; }

        //     Left hand.
        public double HandLeft_X { get; set; } // = 7,
        public double HandLeft_Y { get; set; }
        public double HandLeft_Z { get; set; }

        public double HandLeft_depth_X { get; set; }
        public double HandLeft_depth_Y { get; set; }

        public double HandLeft_color_X { get; set; }
        public double HandLeft_color_Y { get; set; }

        //     Right shoulder.
        public double ShoulderRight_X { get; set; } // = 8,
        public double ShoulderRight_Y { get; set; }
        public double ShoulderRight_Z { get; set; }

        public double ShoulderRight_depth_X { get; set; }
        public double ShoulderRight_depth_Y { get; set; }

        public double ShoulderRight_color_X { get; set; }
        public double ShoulderRight_color_Y { get; set; }
        
        //     Right elbow.
        public double ElbowRight_X { get; set; } // = 9,
        public double ElbowRight_Y { get; set; }
        public double ElbowRight_Z { get; set; }

        public double ElbowRight_depth_X { get; set; }
        public double ElbowRight_depth_Y { get; set; }

        public double ElbowRight_color_X { get; set; }
        public double ElbowRight_color_Y { get; set; }

        //     Right wrist.
        public double WristRight_X { get; set; } // = 10,
        public double WristRight_Y { get; set; }
        public double WristRight_Z { get; set; }

        public double WristRight_depth_X { get; set; }
        public double WristRight_depth_Y { get; set; }

        public double WristRight_color_X { get; set; }
        public double WristRight_color_Y { get; set; }

        //     Right hand.
        public double HandRight_X { get; set; } // = 11,
        public double HandRight_Y { get; set; }
        public double HandRight_Z { get; set; }

        public double HandRight_depth_X { get; set; }
        public double HandRight_depth_Y { get; set; }

        public double HandRight_color_X { get; set; }
        public double HandRight_color_Y { get; set; }

        //     Left hip.
        public double HipLeft_X { get; set; } // = 12,
        public double HipLeft_Y { get; set; }
        public double HipLeft_Z { get; set; }

        public double HipLeft_depth_X { get; set; }
        public double HipLeft_depth_Y { get; set; }

        public double HipLeft_color_X { get; set; }
        public double HipLeft_color_Y { get; set; }

        //     Left knee.
        public double KneeLeft_X { get; set; } // = 13,
        public double KneeLeft_Y { get; set; }
        public double KneeLeft_Z { get; set; }

        public double KneeLeft_depth_X { get; set; }
        public double KneeLeft_depth_Y { get; set; }

        public double KneeLeft_color_X { get; set; }
        public double KneeLeft_color_Y { get; set; }

        //     Left ankle.
        public double AnkleLeft_X { get; set; } // = 14,
        public double AnkleLeft_Y { get; set; }
        public double AnkleLeft_Z { get; set; }

        public double AnkleLeft_depth_X { get; set; }
        public double AnkleLeft_depth_Y { get; set; }

        public double AnkleLeft_color_X { get; set; }
        public double AnkleLeft_color_Y { get; set; }

        //     Left foot.
        public double FootLeft_X { get; set; } // = 15,
        public double FootLeft_Y { get; set; }
        public double FootLeft_Z { get; set; }

        public double FootLeft_depth_X { get; set; }
        public double FootLeft_depth_Y { get; set; }

        public double FootLeft_color_X { get; set; }
        public double FootLeft_color_Y { get; set; }

        //     Right hip.
        public double HipRight_X { get; set; } // = 16,
        public double HipRight_Y { get; set; }
        public double HipRight_Z { get; set; }

        public double HipRight_depth_X { get; set; }
        public double HipRight_depth_Y { get; set; }

        public double HipRight_color_X { get; set; }
        public double HipRight_color_Y { get; set; }

        //     Right knee.
        public double KneeRight_X { get; set; } // = 17,
        public double KneeRight_Y { get; set; }
        public double KneeRight_Z { get; set; }

        public double KneeRight_depth_X { get; set; }
        public double KneeRight_depth_Y { get; set; }

        public double KneeRight_color_X { get; set; }
        public double KneeRight_color_Y { get; set; }

        //     Right ankle.
        public double AnkleRight_X { get; set; } // = 18,
        public double AnkleRight_Y { get; set; }
        public double AnkleRight_Z { get; set; }

        public double AnkleRight_depth_X { get; set; }
        public double AnkleRight_depth_Y { get; set; }

        public double AnkleRight_color_X { get; set; }
        public double AnkleRight_color_Y { get; set; }

        //     Right foot.
        public double FootRight_X { get; set; } // = 19,
        public double FootRight_Y { get; set; }
        public double FootRight_Z { get; set; }

        public double FootRight_depth_X { get; set; }
        public double FootRight_depth_Y { get; set; }

        public double FootRight_color_X { get; set; }
        public double FootRight_color_Y { get; set; }
        
        //     Between the shoulders on the spine.
        public double SpineShoulder_X { get; set; } // = 20,
        public double SpineShoulder_Y { get; set; }
        public double SpineShoulder_Z { get; set; }

        public double SpineShoulder_depth_X { get; set; }
        public double SpineShoulder_depth_Y { get; set; }

        public double SpineShoulder_color_X { get; set; }
        public double SpineShoulder_color_Y { get; set; }

        //     Tip of the left hand.
        public double HandTipLeft_X { get; set; } // = 21,
        public double HandTipLeft_Y { get; set; }
        public double HandTipLeft_Z { get; set; }

        public double HandTipLeft_depth_X { get; set; }
        public double HandTipLeft_depth_Y { get; set; }

        public double HandTipLeft_color_X { get; set; }
        public double HandTipLeft_color_Y { get; set; }
        
        //     Left thumb.
        public double ThumbLeft_X { get; set; } // = 22,
        public double ThumbLeft_Y { get; set; }
        public double ThumbLeft_Z { get; set; }

        public double ThumbLeft_depth_X { get; set; }
        public double ThumbLeft_depth_Y { get; set; }

        public double ThumbLeft_color_X { get; set; }
        public double ThumbLeft_color_Y { get; set; }
        
        //     Tip of the right hand.
        public double HandTipRight_X { get; set; } // = 23,
        public double HandTipRight_Y { get; set; }
        public double HandTipRight_Z { get; set; }

        public double HandTipRight_depth_X { get; set; }
        public double HandTipRight_depth_Y { get; set; }

        public double HandTipRight_color_X { get; set; }
        public double HandTipRight_color_Y { get; set; }

        //     Right thumb.
        public double ThumbRight_X { get; set; } // = 24,
        public double ThumbRight_Y { get; set; }
        public double ThumbRight_Z { get; set; }

        public double ThumbRight_depth_X { get; set; }
        public double ThumbRight_depth_Y { get; set; }

        public double ThumbRight_color_X { get; set; }
        public double ThumbRight_color_Y { get; set; }

        public SkeletonData() { }

        public SkeletonData( int id, double[] a)
        {
            frameID = id;
            int i=0;
            SpineBase_X = a[i++];
            SpineBase_Y = a[i++];
            SpineBase_Z = a[i++];
            SpineBase_depth_X = a[i++];
            SpineBase_depth_Y = a[i++];
            SpineBase_color_X = a[i++];
            SpineBase_color_Y = a[i++];
            //     Middle of the spine.
            SpineMid_X = a[i++];
            SpineMid_Y = a[i++];
            SpineMid_Z = a[i++];
            SpineMid_depth_X = a[i++];
            SpineMid_depth_Y = a[i++];
            SpineMid_color_X = a[i++];
            SpineMid_color_Y = a[i++];
            //     Neck.
            Neck_X = a[i++];
            Neck_Y = a[i++];
            Neck_Z = a[i++];
            Neck_depth_X = a[i++];
            Neck_depth_Y = a[i++];
            Neck_color_X = a[i++];
            Neck_color_Y = a[i++];
            //     Head
            Head_X = a[i++];
            Head_Y = a[i++];
            Head_Z = a[i++];
            Head_depth_X = a[i++];
            Head_depth_Y = a[i++];
            Head_color_X = a[i++];
            Head_color_Y = a[i++];
            //     Left shoulder.
            ShoulderLeft_X = a[i++];
            ShoulderLeft_Y = a[i++];
            ShoulderLeft_Z = a[i++];
            ShoulderLeft_depth_X = a[i++];
            ShoulderLeft_depth_Y = a[i++];
            ShoulderLeft_color_X = a[i++];
            ShoulderLeft_color_Y = a[i++];
            //     Left elbow.
            ElbowLeft_X = a[i++];
            ElbowLeft_Y = a[i++];
            ElbowLeft_Z = a[i++];
            ElbowLeft_depth_X = a[i++];
            ElbowLeft_depth_Y = a[i++];
            ElbowLeft_color_X = a[i++];
            ElbowLeft_color_Y = a[i++];
            //     Left wrist.
            WristLeft_X = a[i++];
            WristLeft_Y = a[i++];
            WristLeft_Z = a[i++];
            WristLeft_depth_X = a[i++];
            WristLeft_depth_Y = a[i++];
            WristLeft_color_X = a[i++];
            WristLeft_color_Y = a[i++];
            //     Left hand.
            HandLeft_X = a[i++];
            HandLeft_Y = a[i++];
            HandLeft_Z = a[i++];
            HandLeft_depth_X = a[i++];
            HandLeft_depth_Y = a[i++];
            HandLeft_color_X = a[i++];
            HandLeft_color_Y = a[i++];
            //     Right shoulder.
            ShoulderRight_X = a[i++];
            ShoulderRight_Y = a[i++];
            ShoulderRight_Z = a[i++];
            ShoulderRight_depth_X = a[i++];
            ShoulderRight_depth_Y = a[i++];
            ShoulderRight_color_X = a[i++];
            ShoulderRight_color_Y = a[i++];
            //     Right elbow.
            ElbowRight_X = a[i++];
            ElbowRight_Y = a[i++];
            ElbowRight_Z = a[i++];
            ElbowRight_depth_X = a[i++];
            ElbowRight_depth_Y = a[i++];
            ElbowRight_color_X = a[i++];
            ElbowRight_color_Y = a[i++];
            //     Right wrist.
            WristRight_X = a[i++];
            WristRight_Y = a[i++];
            WristRight_Z = a[i++];
            WristRight_depth_X = a[i++];
            WristRight_depth_Y = a[i++];
            WristRight_color_X = a[i++];
            WristRight_color_Y = a[i++];
            //     Right hand.
            HandRight_X = a[i++];
            HandRight_Y = a[i++];
            HandRight_Z = a[i++];
            HandRight_depth_X = a[i++];
            HandRight_depth_Y = a[i++];
            HandRight_color_X = a[i++];
            HandRight_color_Y = a[i++];
            //     Left hip.
            HipLeft_X = a[i++];
            HipLeft_Y = a[i++];
            HipLeft_Z = a[i++];
            HipLeft_depth_X = a[i++];
            HipLeft_depth_Y = a[i++];
            HipLeft_color_X = a[i++];
            HipLeft_color_Y = a[i++];
            //     Left knee.
            KneeLeft_X = a[i++];
            KneeLeft_Y = a[i++];
            KneeLeft_Z = a[i++];
            KneeLeft_depth_X = a[i++];
            KneeLeft_depth_Y = a[i++];
            KneeLeft_color_X = a[i++];
            KneeLeft_color_Y = a[i++];
            //     Left ankle.
            AnkleLeft_X = a[i++];
            AnkleLeft_Y = a[i++];
            AnkleLeft_Z = a[i++];
            AnkleLeft_depth_X = a[i++];
            AnkleLeft_depth_Y = a[i++];
            AnkleLeft_color_X = a[i++];
            AnkleLeft_color_Y = a[i++];
            //     Left foot.
            FootLeft_X = a[i++];
            FootLeft_Y = a[i++];
            FootLeft_Z = a[i++];
            FootLeft_depth_X = a[i++];
            FootLeft_depth_Y = a[i++];
            FootLeft_color_X = a[i++];
            FootLeft_color_Y = a[i++];
            //     Right hip.
            HipRight_X = a[i++];
            HipRight_Y = a[i++];
            HipRight_Z = a[i++];
            HipRight_depth_X = a[i++];
            HipRight_depth_Y = a[i++];
            HipRight_color_X = a[i++];
            HipRight_color_Y = a[i++];
            //     Right knee.
            KneeRight_X = a[i++];
            KneeRight_Y = a[i++];
            KneeRight_Z = a[i++];
            KneeRight_depth_X = a[i++];
            KneeRight_depth_Y = a[i++];
            KneeRight_color_X = a[i++];
            KneeRight_color_Y = a[i++];
            //     Right ankle.
            AnkleRight_X = a[i++];
            AnkleRight_Y = a[i++];
            AnkleRight_Z = a[i++];
            AnkleRight_depth_X = a[i++];
            AnkleRight_depth_Y = a[i++];
            AnkleRight_color_X = a[i++];
            AnkleRight_color_Y = a[i++];
            //     Right foot.
            FootRight_X = a[i++];
            FootRight_Y = a[i++];
            FootRight_Z = a[i++];
            FootRight_depth_X = a[i++];
            FootRight_depth_Y = a[i++];
            FootRight_color_X = a[i++];
            FootRight_color_Y = a[i++];
            //     Between the shoulders on the spine.
            SpineShoulder_X = a[i++];
            SpineShoulder_Y = a[i++];
            SpineShoulder_Z = a[i++];
            SpineShoulder_depth_X = a[i++];
            SpineShoulder_depth_Y = a[i++];
            SpineShoulder_color_X = a[i++];
            SpineShoulder_color_Y = a[i++];
            //     Tip of the left hand.
            HandTipLeft_X = a[i++];
            HandTipLeft_Y = a[i++];
            HandTipLeft_Z = a[i++];
            HandTipLeft_depth_X = a[i++];
            HandTipLeft_depth_Y = a[i++];
            HandTipLeft_color_X = a[i++];
            HandTipLeft_color_Y = a[i++];
            //     Left thumb.
            ThumbLeft_X = a[i++];
            ThumbLeft_Y = a[i++];
            ThumbLeft_Z = a[i++];
            ThumbLeft_depth_X = a[i++];
            ThumbLeft_depth_Y = a[i++];
            ThumbLeft_color_X = a[i++];
            ThumbLeft_color_Y = a[i++];
            //     Tip of the right hand.
            HandTipRight_X = a[i++];
            HandTipRight_Y = a[i++];
            HandTipRight_Z = a[i++];
            HandTipRight_depth_X = a[i++];
            HandTipRight_depth_Y = a[i++];
            HandTipRight_color_X = a[i++];
            HandTipRight_color_Y = a[i++];
            //     Right thumb.
            ThumbRight_X = a[i++];
            ThumbRight_Y = a[i++];
            ThumbRight_Z = a[i++];
            ThumbRight_depth_X = a[i++];
            ThumbRight_depth_Y = a[i++];
            ThumbRight_color_X = a[i++];
            ThumbRight_color_Y = a[i++];
        }
    }
}
