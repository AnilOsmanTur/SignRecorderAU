using System;
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
        private ImageSource depthPreviewBitmap;
        private ImageSource infraredPreviewBitmap;
        private ImageSource bodyIndexPreviewBitmap;
        private ColorHandler ch;
        private int count = 0;
        public PreviewWindow()
        {
            InitializeComponent();
            ch = ColorHandler.Instance;
            ch.openReader();
            ComponentDispatcher.ThreadIdle += new System.EventHandler(ComponentDispatcher_ThreadIdle);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            ch.closeReader();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            
        }

        void ComponentDispatcher_ThreadIdle(object sender, EventArgs e)
        {
            if (count < ch.readerFrameCount) 
            { 
                color_preview.Source = ch.Read();
                count++;
            }
        }


    }
}
