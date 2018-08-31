﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectRecorder
{
    /// <summary>
    /// Interaction logic for preview.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        private ImageSource colorPreviewBitmap;
        private WriteableBitmap depthPreviewBitmap;
        private ImageSource infraredPreviewBitmap;
        private ImageSource bodyIndexPreviewBitmap;
        private ColorHandler ch;
        private DepthHandler dh;
        private int count = 0;
        public PreviewWindow()
        {
            InitializeComponent();
            ch = ColorHandler.Instance;
            ch.openReader();
            dh = DepthHandler.Instance;
            dh.openReader();

            depthPreviewBitmap = new WriteableBitmap(dh.Width, dh.Height, 96.0, 96.0, PixelFormats.Gray16, null);

            ComponentDispatcher.ThreadIdle += new System.EventHandler(ComponentDispatcher_ThreadIdle);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            ch.closeReader();
        }


        void ComponentDispatcher_ThreadIdle(object sender, EventArgs e)
        {
            if (count < ch.readerFrameCount) 
            { 
                color_preview.Source = ch.Read();
                dh.Read(ref depthPreviewBitmap);
                depth_preview.Source = depthPreviewBitmap;
                count++;
            }        
        }


    }
}