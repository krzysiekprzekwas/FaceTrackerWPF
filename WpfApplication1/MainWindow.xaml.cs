﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
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
using System.Windows.Interop;


namespace FaceTracker
{
    public partial class MainWindow
    {

        public MainWindow()
        {
            InitializeComponent();

            ComponentDispatcher.ThreadIdle += (sender, e) => System.Windows.Forms.Application.RaiseIdle(e);
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

        private new void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("(\\d*\\.)?\\d+"); 
            return !regex.IsMatch(text);
        }
    }
}