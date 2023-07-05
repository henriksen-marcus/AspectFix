using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string selectedFile = "";
        private string ffmpegPath = "ffmpeg.exe";
        private string ffprobePath = "ffprobe.exe";

        public delegate void FileProcessedEventHandler();
        public event FileProcessedEventHandler OnFileProcessed;

        public MainWindow()
        {
            InitializeComponent();
            OnFileProcessed += ResetUI;
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {

        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string filename = Path.GetFileName(files[0]);
            FileNameTextBlock.Text = filename;
            selectedFile = files[0];
        }

        private (int, int) GetVideoDimensions(string filePath)
        {
            // Create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffprobePath;
            startInfo.Arguments = $"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x {filePath}";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Read the output
            string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Clean up the process
            process.Close();

            // Parse the output to retrieve the width and height values
            string[] dimensions = output.Trim().Split('x');
            int width = int.Parse(dimensions[0]);
            int height = int.Parse(dimensions[1]);

            return (width, height);
        }

        private void Crop(string filePath)
        {
            (int width, int height) = GetVideoDimensions(filePath);

            // Cropping from 16:9 to 9:16
            int newWidth = height * 9 / 16;

            string fileName = Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath);
            string newFileName = fileName.Replace(extension, ".cropped.mp4");
            string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

            // Saves to the same location as original file
            string arguments = $"-i {filePath} -filter:v \"crop={newWidth}:{height}\" {newFilePath}";

            string ffmpegPath = "ffmpeg.exe";

            // Create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffmpegPath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Read the output
            string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Clean up the process
            process.Close();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedFile == "")
            {
                MessageBox.Show("Please select a file first!");
                return;
            }

            ToggleOverlay();
            Crop(selectedFile);
            ToggleOverlay();

            OnFileProcessed?.Invoke();
        }

        private void ToggleOverlay()
        {
            DarkenOverlay.Visibility = DarkenOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            OverlayUI.Visibility = OverlayUI.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetUI()
        {
            FileNameTextBlock.Text = "No file selected";
            selectedFile = "";
        }
    }
}