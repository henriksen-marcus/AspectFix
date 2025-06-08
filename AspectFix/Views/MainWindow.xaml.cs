using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using AspectFix.Components;
using AspectFix.Services;
using Path = System.IO.Path;

namespace AspectFix.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel MainViewModel  { get; private set; }
        public HomeViewModel HomeViewModel { get; private set; }
        public EditViewModel EditViewModel  { get; private set; }
        public VideoFile SelectedFile { get; private set; }
        public static MainWindow Instance   { get; private set; }

        /// <summary>
        /// The PID of the current running process.
        /// </summary>
        public int CurrentPID { get; set; } = -1;

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
            MainViewModel = new MainViewModel();
            HomeViewModel = (HomeViewModel)MainViewModel.SelectedViewModel;
            EditViewModel = /*(EditViewModel)MainViewModel.SelectedViewModel;*/new EditViewModel();
            DataContext = MainViewModel;

            if (!File.Exists("ffmpeg.exe") || !File.Exists("ffprobe.exe"))
            {
                ErrorMessage("ffmpeg or ffprobe not found. Please place ffmpeg.exe and ffprobe.exe in the same folder as AspectFix.exe.");
                Application.Current.Shutdown();
            }

            Loaded += MainWindow_Loaded;


            //SelectedFile = new VideoFile(@"C:\Users\Zipur\Videos\vlc-record-2025-01-17-22h15m59s-playlist.m3u8-.mp4");
            //SelectedFile = new VideoFile(@"F:\YoutubeVideos\aspectTest.mp4");
            //SelectedFile = new VideoFile(@"F:\YoutubeVideos\Color Matte_1.mp4");
            //SelectedFile = new VideoFile(@"C:\Users\Zipur\Downloads\Iron Man 2 (2010) [2160p] [4K] [BluRay] [5.1] [YTS.MX]\Iron.Man.2.2010.2160p.4K.BluRay.x265.10bit.AAC5.1-[YTS.MX].mkv");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        public void FileProcessed()
        {
            OnFileProcessed?.Invoke();
            CurrentPID = -1;
            SelectedFile = null;
        } 

        public void ExitApp() => OnExitApp?.Invoke();

        public void SetSelectedFile(string path)
        {
            if (path == null) SelectedFile = null;
            else SelectedFile = new VideoFile(path);
        }

        // For dragging the window around
        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExitApp();

            if (CurrentPID != -1)
            {
                var process = Process.GetProcessById(CurrentPID);
                process.Kill();
                process.WaitForExit();
                if (process.HasExited == false) ErrorMessage("Could not close ffmpeg. PID: " + CurrentPID);
                else
                {
                    // Delete the leftover corrupt file
                    try
                    {
                        string newpath = Path.Combine(Path.GetDirectoryName(SelectedFile.Path), SelectedFile.FileName + ".cropped" + SelectedFile.Extension);
                        File.Delete(newpath);
                    }
                    catch { }
                }
            }
            
            Application.Current.Shutdown();
        }

        public void ToggleOverlay()
        {
            LoadingOverlay.Visibility = LoadingOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            MainGridOverlay.Visibility = LoadingOverlay.Visibility;
        }

        public void ChangeView(string viewName)
        {
            switch (viewName)
            {
                case "Home":
                    MainViewModel.SelectedViewModel = HomeViewModel;
                    //DataContext = new HomeViewModel();
                    break;
                case "Edit":
                    MainViewModel.SelectedViewModel = EditViewModel;
                    break;
            }

            GC.Collect();
        }

        // We need a central handler for messages, because we can't show a MessageBox from a background thread
        public void ErrorMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)));
        }

        private void CloseButton_Overlay_OnClick(object sender, RoutedEventArgs e)
        {
            CloseButton_OnClick(sender, e);
        }
    }
}
