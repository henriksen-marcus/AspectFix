using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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

        private bool _hasInititated = false;
        private FileProcessor.CropOptions _currentCropOptions;

        public EditView()
        {
            InitializeComponent();
            MainWindow.Instance.OnExitApp += Cleanup;
            if (MainWindow.Instance.SelectedFile != null) InitPreviews();

            CheckIfWeCanCrop();
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
            }
            else MainWindow.Instance.ErrorMessage("Failed to generate preview image.");
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

        /// <summary>
        /// Enable/disable the crop button based on conditions
        /// </summary>
        public void CheckIfWeCanCrop()
        {
            var options = GetCropOptions();
            VideoFile vid = MainWindow.Instance.SelectedFile;
            Components.RoundedButton btn = CropButton;
            if (btn != null && vid != null)
            {
                var dim1 = vid.GetCroppedDimensions(options);
                (int, int) dim2 = (dim1.Item1, dim1.Item2);
                btn.IsEnabled = vid.GetDimensions() != dim2 || vid.Rotation != 0;
            }
        }

        private FileProcessor.CropOptions GetCropOptions()
        {
            bool auto = AspectDropDown.SelectedIndex == 0;
            bool crop = AspectDropDown.SelectedIndex != 2;
            var options = new FileProcessor.CropOptions
            {
                isAuto = auto,
                shouldCrop = crop,
                iterations = IterationCount,
        };
            return options;
        }

        private void AspectDropDown_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_hasInititated)
            {
                _hasInititated = true;
                return;
            }

            CheckIfWeCanCrop();

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
            MainWindow.Instance.ToggleOverlay();
            var timer = new Timer(100);
            timer.Elapsed += (timerSender, timerEvent) => 
            {
                MainWindow.Instance.LoadingOverlay.UpdateProgress(GetProgress());
            };
            var options = GetCropOptions();
            _currentCropOptions = options;

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
                Console.WriteLine("File does not exist");
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
            CheckIfWeCanCrop();
        }

        private void RotateRightButton_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.SelectedFile?.AddRotation(90);
            UpdatePreview();
            CheckIfWeCanCrop();
        }
    }
}
