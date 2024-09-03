using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Path = System.IO.Path;

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
        /// <summary>
        /// The PID of the current running process
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
            Viewmodel = new MainViewModel();
            DataContext = Viewmodel;

            if (!File.Exists("ffmpeg.exe") || !File.Exists("ffprobe.exe"))
            {
                ErrorMessage("ffmpeg or ffprobe not found. Please place ffmpeg.exe and ffprobe.exe in the same folder as AspectFix.exe.");
                Application.Current.Shutdown();
            }
        }

        public void FileProcessed()
        {
            OnFileProcessed?.Invoke();
            CurrentPID = -1;
            SelectedFile = null;
        } 

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

        private void CloseButton_Overlay_OnClick(object sender, RoutedEventArgs e)
        {
            CloseButton_OnClick(sender, e);
        }
    }
}
