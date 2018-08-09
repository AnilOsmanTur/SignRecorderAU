using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectRecorder
{
    class HeaderUnit
    {
        public int Word_Id { get; set; }
        public int User_Id { get; set; }
        public int Repeat { get; set; }
        public int Tutorial { get; set; }
        public string filePath { get; set; }

        public HeaderUnit(int wid, int uid, int rep, int tut, string path)
        {
            Word_Id = wid;
            User_Id = uid;
            Repeat = rep;
            Tutorial = tut;
            filePath = path;
        }
    }
}
