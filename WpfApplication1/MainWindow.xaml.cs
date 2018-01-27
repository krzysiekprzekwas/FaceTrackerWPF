using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WebEye.Controls.Wpf;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;


namespace FaceTracker
{
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();

            ComponentDispatcher.ThreadIdle += (sender, e) => System.Windows.Forms.Application.RaiseIdle(e);

            mySlider.Maximum = Enum.GetNames(typeof(QualityEnum)).Length - 1;
            
        }


        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    FaceDetectionCheckBox.IsChecked = !FaceDetectionCheckBox.IsChecked.Value;
                    break;
                case Key.H:
                    HistogramEqualizationCheckBox.IsChecked = !HistogramEqualizationCheckBox.IsChecked.Value;
                    break;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var ctx = (FaceTrackViewModel) DataContext;
            ctx.ApplyMaskEnabled = !ctx.ApplyMaskEnabled;
        }
    }
}