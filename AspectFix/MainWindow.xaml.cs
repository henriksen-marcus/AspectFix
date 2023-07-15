﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel Viewmodel;
        public VideoFile SelectedFile { get; private set; }
        public static MainWindow Instance { get; private set; }

        // ---------- Events ---------- //
        public delegate void FileProcessedEventHandler();
        public event FileProcessedEventHandler OnFileProcessed;

        public delegate void ExitAppEventHandler();
        public event ExitAppEventHandler OnExitApp;

        public delegate void ToggleDragOverlayEventHandler(bool isValidFile);
        public event ToggleDragOverlayEventHandler OnToggleDragOverlay;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            Viewmodel = new MainViewModel();
            DataContext = Viewmodel;
        }

        public void FileProcessed() => OnFileProcessed?.Invoke();
        public void ExitApp() => OnExitApp?.Invoke();
        public void ToggleDragOverlay(bool isValidFile) => OnToggleDragOverlay?.Invoke(isValidFile);

        public void SetSelectedFile(string path)
        {
            if (path == null) SelectedFile = null;
            else SelectedFile = new VideoFile(path);
        }

        // For dragging the window around
        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExitApp();
            Application.Current.Shutdown();
        }

        public void ToggleOverlay()
        {
            LoadingOverlay.Visibility = LoadingOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            //MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#656500"));
        }

        public void ChangeView(string viewName)
        {
            if (viewName == "Home")
                Viewmodel.SelectedViewModel = new HomeViewModel();
            else if (viewName == "Edit")
                Viewmodel.SelectedViewModel = new EditViewModel();

            GC.Collect();
        }

        // We need a central handler for messages, because we can't show a MessageBox from a background thread
        public void ErrorMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)));
        }
    }
}
