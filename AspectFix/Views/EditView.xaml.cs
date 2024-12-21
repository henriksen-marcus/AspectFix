using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AspectFix.Views;
using Point = System.Windows.Point;

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for EditView.xaml
    /// </summary>
    public partial class EditView : UserControl
    {
        private BitmapImage _oldPreview;
        private BitmapImage _newPreview;

        private int _iterationCount = 1;
        public int IterationCount
        {
            get => _iterationCount;
            set
            {
                if (value > 3) _iterationCount = 3;
                else if (value < 1) _iterationCount = 1;
                else _iterationCount = value;

                IterationsTextBlock.Text = "Iterations: " + _iterationCount.ToString();
            }
        }

        private bool _hasInitiated = false;
        private FileProcessor.CropOptions _currentCropOptions;

        public EditView()
        {
            InitializeComponent();
            DataContext = MainWindow.Instance.EditViewModel;

            MainWindow.Instance.OnExitApp += Cleanup;
            if (MainWindow.Instance.SelectedFile != null)
            {
                InitPreviews();
            }
            else MainWindow.Instance.ErrorMessage("File got lost!");

            //LayoutUpdated += EditView_LayoutUpdated;

            UpdateCropButton();
        }

        public void InitPreviews()
        {
            string oldPreviewPath = FileProcessor.GetPreviewImage(MainWindow.Instance.SelectedFile);
            if (oldPreviewPath == null)
            {
                MainWindow.Instance.ErrorMessage("Failed to generate preview images.");
                return;
            }

            using (FileStream stream = File.OpenRead(oldPreviewPath))
            {
                _oldPreview = new BitmapImage();
                _oldPreview.BeginInit();
                _oldPreview.CacheOption = BitmapCacheOption.OnLoad;
                _oldPreview.StreamSource = stream;
                _oldPreview.EndInit();
            }

            ImagePreviewOld.Source = _oldPreview;
            var w = MainWindow.Instance.SelectedFile.Width;
            var h = MainWindow.Instance.SelectedFile.Height;
            OriginalTitle.Title = "Original " + w + "x" + h;

            UpdatePreview();
        }

        /// <summary>
        /// Generates the cropped (new) preview image
        /// </summary>
        private void UpdatePreview()
        {
            // Delete previous preview before making a new one else we
            // will get an error because the previous file is in use
            if (_newPreview != null)
            {
                _newPreview.StreamSource?.Dispose();
                _newPreview = null;
                GC.Collect();
                //File.Delete("preview_new.jpg");
            }

            //if (_currentCropOptions.Equals(default))
            //{
            //    ImagePreviewNew.Source = _oldPreview;
            //    return;
            //}

            string newPreviewPath = FileProcessor.GetCroppedPreviewImage(MainWindow.Instance.SelectedFile, GetCropOptions());
            if (newPreviewPath != null)
            {
                using (FileStream stream = File.OpenRead(newPreviewPath))
                using (var memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);
                    _newPreview = new BitmapImage();
                    _newPreview.BeginInit();
                    _newPreview.CacheOption = BitmapCacheOption.OnLoad;
                    _newPreview.StreamSource = memStream;
                    _newPreview.EndInit();
                }

                ImagePreviewNew.Source = _newPreview;

                var w = MainWindow.Instance.SelectedFile.CroppedWidth;
                var h = MainWindow.Instance.SelectedFile.CroppedHeight;
                CroppedTitle.Title = "Cropped " + w + "x" + h;
            }
            else MainWindow.Instance.ErrorMessage("Failed to generate preview image :(");
        }

        /// <summary>
        /// Releases preview images from memory and deletes the files
        /// </summary>
        private void Cleanup()
        {
            _oldPreview?.StreamSource?.Dispose();
            _newPreview?.StreamSource?.Dispose();
            _oldPreview = null;
            _newPreview = null;
            GC.Collect();

            File.Delete("preview_new.jpg");
            File.Delete("preview_old.jpg");
        }

        /// <returns> If processing is possible based on if any changes have been made. </returns>
        public bool CanWeCrop()
        {
            var options = GetCropOptions();
            VideoFile vid = MainWindow.Instance.SelectedFile;

            if (vid == null) return false;

            var croppedDimensions = vid.GetCroppedDimensions(options);
            (int, int) croppedDimsArr = (croppedDimensions.Item1, croppedDimensions.Item2);

            // We can't process the video if there aren't any changes
            return vid.GetDimensions() != croppedDimsArr || vid.Rotation != 0;
        }

        /// <summary>
        /// Enable/disable the crop button based on conditions
        /// </summary>
        public void UpdateCropButton()
        {
            Components.RoundedButton btn = CropButton;
            if (btn != null) btn.IsEnabled = CanWeCrop();

        }

        private FileProcessor.CropOptions GetCropOptions()
        {
            bool auto = AspectDropDown.SelectedIndex == 0;
            bool crop = AspectDropDown.SelectedIndex != 2;
            var options = new FileProcessor.CropOptions
            {
                IsAuto = auto,
                ShouldCrop = crop,
                Iterations = IterationCount,
        };
            return options;
        }

        private void AspectDropDown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_hasInitiated)
            {
                _hasInitiated = true;
                return;
            }

            UpdateCropButton();

            if (AspectDropDown.SelectedIndex == 1)
            {
                IterationsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                IterationsPanel.Visibility = Visibility.Collapsed;
            }

            UpdatePreview();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Cleanup();
            MainWindow.Instance.ChangeView("Home");
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            int oldIterationCount = IterationCount;
            IterationCount++;
            if (oldIterationCount != IterationCount) UpdatePreview();
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            int oldIterationCount = IterationCount;
            IterationCount--;
            if (oldIterationCount != IterationCount) UpdatePreview();
        }
        
        private async void CropButton_Click(object sender, RoutedEventArgs e)
        {
            // Processing file overlay
            MainWindow.Instance.ToggleOverlay();

            // Update progress timer
            var timer = new Timer(500); 
            timer.Elapsed += (timerSender, timerEvent) => 
            {
                MainWindow.Instance.LoadingOverlay.UpdateProgress(GetProgress());
            };

            var options = GetCropOptions();
            _currentCropOptions = options;

            // Process the file on a separate thread
            await Task.Run(async () =>
            {
                timer.Start();
                Task<string> task = Task.Run(() => FileProcessor.Crop(MainWindow.Instance.SelectedFile, options));
                string path = await task;
                timer.Stop();

                if (path == null) MainWindow.Instance.ErrorMessage("Failed to crop video.");

                MainWindow.Instance.LoadingOverlay.UpdateProgress(100);
                await Task.Delay(300);
            });

            Trace.WriteLine("Est file size new (MB): " + EstimateVideoSize(MainWindow.Instance.SelectedFile));
            Trace.WriteLine("Est file size old (MB): " + OldEst());

            // Add to recent files list
            MainWindow.Instance.HomeViewModel.AddFile(MainWindow.Instance.SelectedFile.Path);
            MainWindow.Instance.LoadingOverlay.UpdateProgress(0);
            MainWindow.Instance.ToggleOverlay();
            MainWindow.Instance.FileProcessed();
            Cleanup();
            MainWindow.Instance.ChangeView("Home");
        }

        /// <returns>The estimated progress of processing the video file from 0-100</returns>
        private int GetProgress()
        {
            string newFileName = $"{MainWindow.Instance.SelectedFile.FileName}.cropped{MainWindow.Instance.SelectedFile.Extension}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(MainWindow.Instance.SelectedFile.Path), newFileName);
            if (!File.Exists(newFilePath))
            {
                Trace.WriteLine("File does not exist");
                return 0;
            }
            FileInfo oldFileInfo = new FileInfo(MainWindow.Instance.SelectedFile.Path);
            FileInfo newFileInfo = new FileInfo(newFilePath);

            var oldDims = MainWindow.Instance.SelectedFile.GetDimensions();
            var newDims = MainWindow.Instance.SelectedFile.GetCroppedDimensions(_currentCropOptions);
            var oldArea = oldDims.Item1 * oldDims.Item2;
            var newArea = newDims.Item1 * newDims.Item2;
            var percentageChange = (double)newArea / (double)oldArea + 0.1;
            //Console.WriteLine("Expected file size in MB: " + (double)oldFileInfo.Length/1000000 * percentageChange);
            //Console.WriteLine("Actual file size in MB: " + (double)newFileInfo.Length/1000000);
            //Console.WriteLine("Percentage change: " + percentageChange);
            int progress = FileProcessor.Lerp(0, 100, (double)newFileInfo.Length/((double)oldFileInfo.Length * percentageChange));
            if (progress > 100) progress = 100;
            return (int)progress;
        }

        private void RotateLeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SelectedFile?.AddRotation(-90);
            UpdatePreview();
            UpdateCropButton();
        }

        private void RotateRightButton_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SelectedFile?.AddRotation(90);
            UpdatePreview();
            UpdateCropButton();
        }




        public double OldEst()
        {
            FileInfo oldFileInfo = new FileInfo(MainWindow.Instance.SelectedFile.Path);

            var oldDims = MainWindow.Instance.SelectedFile.GetDimensions();
            var newDims = MainWindow.Instance.SelectedFile.GetCroppedDimensions(_currentCropOptions);
            var oldArea = oldDims.Item1 * oldDims.Item2;
            var newArea = newDims.Item1 * newDims.Item2;
            var percentageChange = (double)newArea / (double)oldArea + 0.1;
            //Console.WriteLine("Expected file size in MB: " + (double)oldFileInfo.Length/1000000 * percentageChange);
            //Console.WriteLine("Actual file size in MB: " + (double)newFileInfo.Length/1000000);
            //Console.WriteLine("Percentage change: " + percentageChange);
            return (double)oldFileInfo.Length / 1000000 * percentageChange;
        }



        public static double EstimateVideoSize(VideoFile videoFile)
        {
            // Get the original video bitrate (in kbps) using your method or FFmpeg
            double originalVideoBitrate = FileProcessor.GetVideoBitrate(videoFile.Path); // in kbps
            double originalAudioBitrate = FileProcessor.GetAudioBitrate(videoFile.Path); // in kbps

            // If trimming is applied, calculate the new duration
            double newDurationInSeconds = (videoFile.TrimEnd - videoFile.TrimStart).TotalSeconds;

            // If no trimming, use the full length
            if (newDurationInSeconds <= 0 || newDurationInSeconds > videoFile.Length)
            {
                newDurationInSeconds = videoFile.Length;
            }

            // Scale the video bitrate based on cropping (adjust the bitrate for new resolution)
            double originalResolution = videoFile.Width * videoFile.Height;
            double croppedResolution = videoFile.CroppedWidth * videoFile.CroppedHeight;

            // Adjust the video bitrate based on the ratio of the cropped resolution to the original resolution
            double scaledVideoBitrate = originalVideoBitrate * (croppedResolution / originalResolution);

            // Convert bitrates to bytes per second (bps = kbps * 1000 / 8)
            double videoBytesPerSecond = scaledVideoBitrate * 1000 / 8;
            double audioBytesPerSecond = originalAudioBitrate * 1000 / 8;

            // Estimate the file size in bytes
            double estimatedSizeInBytes = newDurationInSeconds * (videoBytesPerSecond + audioBytesPerSecond);

            // Convert bytes to megabytes (MB = bytes / (1024 * 1024))
            double estimatedSizeInMegabytes = estimatedSizeInBytes / (1024 * 1024);

            return estimatedSizeInMegabytes;
        }
    }
}
