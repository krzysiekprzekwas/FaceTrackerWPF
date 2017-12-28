using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WebEye.Controls.Wpf;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using System.Windows.Forms.Integration;
using System.Windows.Input;


namespace FaceTracker
{
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();
        }


        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    FaceDetectionCheckBox.IsChecked = !FaceDetectionCheckBox.IsChecked.Value;
                    break;
                case Key.E:
                    EyeDetectionCheckBox.IsChecked = !EyeDetectionCheckBox.IsChecked.Value;
                    break;
            }
        }
    }
}