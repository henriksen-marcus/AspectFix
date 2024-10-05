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


        public static readonly DependencyProperty CropBarsWidthProperty =
            DependencyProperty.Register(nameof(CropBarsWidth), typeof(double), typeof(EditView),
                new PropertyMetadata(50.0, OnCropBarsWidthChanged));

        // Property accessor
        public double CropBarsWidth
        {
            get => (double)GetValue(CropBarsWidthProperty);
            set => SetValue(CropBarsWidthProperty, value);
        }

        private static void OnCropBarsWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle property changes, if needed (e.g., trigger UI updates or logging)
            var mainWindow = d as MainWindow;
            double newValue = (double)e.NewValue;
            Trace.WriteLine($"CropBarsWidth changed to: {newValue}");
        }

        public static readonly DependencyProperty LeftColumnWidthProperty =
            DependencyProperty.Register(nameof(LeftColumnWidth), typeof(GridLength), typeof(EditView),
                new PropertyMetadata(new GridLength(50, GridUnitType.Pixel)));

        public static readonly DependencyProperty RightColumnWidthProperty =
            DependencyProperty.Register(nameof(RightColumnWidth), typeof(GridLength), typeof(EditView),
                new PropertyMetadata(new GridLength(50, GridUnitType.Pixel)));

        public GridLength LeftColumnWidth
        {
            get => (GridLength)GetValue(LeftColumnWidthProperty);
            set => SetValue(LeftColumnWidthProperty, value);
        }

        public GridLength RightColumnWidth
        {
            get => (GridLength)GetValue(RightColumnWidthProperty);
            set => SetValue(RightColumnWidthProperty, value);
        }

        // Tracking movement distance
        public double CropMovement { get; private set; }

        private bool _isDragging = false;
        private Point _startPoint;

        private void MoveCropRegion(double delta)
        {
            CropMovement += delta;
            double newLeftWidth = LeftColumnWidth.Value + delta;
            double newRightWidth = RightColumnWidth.Value - delta;

            if (newLeftWidth >= 0 && newRightWidth >= 0)
            {
                LeftColumnWidth = new GridLength(newLeftWidth, GridUnitType.Pixel);
                RightColumnWidth = new GridLength(newRightWidth, GridUnitType.Pixel);
            }
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

                ImagePreviewNew.Source = _oldPreview;//_newPreview;

                var w = MainWindow.Instance.SelectedFile.CroppedWidth;
                var h = MainWindow.Instance.SelectedFile.CroppedHeight;
                CroppedTitle.Title = "Cropped " + w + "x" + h;

                // Update the crop preview regions to reflect the actual new processed image's aspect ratio
                // Defer layout calculations using Dispatcher to ensure layout pass is complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var actualAspectRatio = Math.Max(_newPreview.Width, _newPreview.Height) / Math.Min(_newPreview.Width, _newPreview.Height);
                    var gridHeight = ((Grid)this.FindName("MyGrid")).ActualHeight;
                    var newWidth = gridHeight / actualAspectRatio;
                    CropBarsWidth = newWidth;

                }), System.Windows.Threading.DispatcherPriority.Loaded);
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

        private Rectangle draggedRectangle;
        private void CropRegion_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(MyGrid); // Get the mouse starting position
            Mouse.Capture((UIElement)sender); // Capture the mouse to the rectangle
            LeftColumnWidth = new GridLength(100, GridUnitType.Pixel);
        }

        private void CropRegion_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(MyGrid);
                double delta = currentPoint.X - _startPoint.X; // Calculate horizontal movement in pixels

                // Update how much we moved from the original position
                CropMovement += delta;

                // Adjust the column widths based on dragging
                double newLeftWidth = LeftColumnWidth.Value + delta;
                double newRightWidth = RightColumnWidth.Value - delta;

                // Ensure that columns remain within valid width ranges
                if (newLeftWidth >= 0 && newRightWidth >= 0)
                {
                    LeftColumnWidth = new GridLength(newLeftWidth, GridUnitType.Pixel);
                    RightColumnWidth = new GridLength(newRightWidth, GridUnitType.Pixel);
                }

                // Update the starting point to the current point for smooth dragging
                _startPoint = currentPoint;
            }
        }

        private void CropRegion_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var endPoint = e.GetPosition(MyGrid);
            var move = endPoint.X - _startPoint.X;
            Trace.WriteLine("movement: " + move);
            _isDragging = false;
            Mouse.Capture(null); // Release the mouse capture
        }
    }
}
