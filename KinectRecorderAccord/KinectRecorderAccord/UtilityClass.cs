using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;

namespace KinectRecorderAccord
{
    class UtilityClass
    {
        internal static Bitmap ByteArrayToBitmap(byte[] array, int width, int height, System.Drawing.Imaging.PixelFormat pixelFormat)
        {

            Bitmap bitmapFrame = new Bitmap(width, height, pixelFormat);

            System.Drawing.Imaging.BitmapData bitmapData = bitmapFrame.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);

            IntPtr intPointer = bitmapData.Scan0;
            Marshal.Copy(array, 0, intPointer, array.Length);

            bitmapFrame.UnlockBits(bitmapData);
            return bitmapFrame;

        }

        // iterate over Enum
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }


    }
}
