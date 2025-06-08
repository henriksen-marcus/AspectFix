using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AspectFix.Components;
using AspectFix.Services;
using AspectFix.Views;
using static AspectFix.FileProcessor;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace AspectFix
{
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
        ResizeAdorner _resizeAdorner;
        private Point _dragStartPoint;
        private bool _isDragging = false;

        public EditView()
        {
            InitializeComponent();
            DataContext = MainWindow.Instance.EditViewModel;

            MainWindow.Instance.OnExitApp += Cleanup;
            Loaded += EditView_Loaded;

            if (MainWindow.Instance.SelectedFile != null)
            {
                InitPreviews();
            }
            else MainWindow.Instance.ErrorMessage("File got lost!");

            //LayoutUpdated += EditView_LayoutUpdated;

            UpdateCropButton();
        }


        // TODO: When rotating, make canvas fit new size to allow resizeAdorner to size properly. Flip width/height of resizeAdorner output.


        private void EditView_Loaded(object sender, RoutedEventArgs e)
        {
            _resizeAdorner = new ResizeAdorner(ResizeAdorner);
            _resizeAdorner.ThumbDragged += ResizeAdorner_ThumbDragged;

            AdornerLayer.GetAdornerLayer(ResizeAdorner).Add(_resizeAdorner);

            if (NewImageContainer.Parent is not FrameworkElement parentGrid)
            {
                MainWindow.Instance.ErrorMessage("Could not initialize cropping system.");
                MyCanvas.Visibility = Visibility.Collapsed;
                return;
            }

            if (ImagePreviewNew.ImageSource is BitmapSource bitmapSource)
            {
                // Get the image's original dimensions
                double imageWidth = bitmapSource.PixelWidth;
                double imageHeight = bitmapSource.PixelHeight;

                double rectangleWidth = NewImageContainer.ActualWidth;
                double rectangleHeight = NewImageContainer.ActualHeight;

                double scale = Math.Min(rectangleWidth / imageWidth, rectangleHeight / imageHeight);

                double scaledWidth = imageWidth * scale;
                double scaledHeight = imageHeight * scale;

                MyCanvas.Width = scaledWidth;
                MyCanvas.Height = scaledHeight;
            }

            _resizeAdorner.MaxWidth = MyCanvas.Width;
            _resizeAdorner.MaxHeight = MyCanvas.Height;

            var cropOptions = GetCropOptions();
            //if (cropOptions.ShouldCrop) ResetCrop();
            /*else*/ FillCrop();

            Console.WriteLine($"Set canvas size. Adorner h: {_resizeAdorner.Height} Canvas height:  {MyCanvas.Height}");
        }

        //private void ScaleDimensions(double inWidth, double inHeight, double inX, double inY, )

        private void ResizeAdorner_ThumbDragged(object sender, ThumbDraggedEventArgs e)
        {
            // This was a fucking nightmare

            double newLeft = Canvas.GetLeft(ResizeAdorner) - e.DeltaX / 2;
            double newTop = Canvas.GetTop(ResizeAdorner) - e.DeltaY / 2;

            newLeft = Clamp(newLeft, 0, MyCanvas.ActualWidth - ResizeAdorner.ActualWidth);
            newTop = Clamp(newTop, 0, MyCanvas.ActualHeight - ResizeAdorner.ActualHeight);

            // Prevent width/height increase on right and bottom by dragging top and left thumbs when at 0
            bool blockLeft = newLeft == 0 && e.DeltaX > 0;
            bool blockTop = newTop == 0 && e.DeltaY > 0;

            _resizeAdorner.BlockResizeX = (blockLeft && e.Sender is 0 or 2);
            _resizeAdorner.BlockResizeY = (blockTop && e.Sender is 0 or 1);

            bool moveX = true;
            bool moveY = true;

            switch (e.Sender)
            {
                case 1:
                    if (Canvas.GetLeft(ResizeAdorner) + ResizeAdorner.ActualWidth + e.DeltaX >= MyCanvas.ActualWidth) moveX = false;
                    else _resizeAdorner.MaxWidth = MyCanvas.ActualWidth - newLeft;

                    _resizeAdorner.MaxHeight = MyCanvas.ActualHeight - newTop;
                    break;
                case 2: // Bottom left
                    _resizeAdorner.MaxWidth = MyCanvas.ActualWidth - newLeft;

                    if (Canvas.GetTop(ResizeAdorner) + ResizeAdorner.ActualHeight + e.DeltaY >= MyCanvas.ActualHeight) moveY = false;
                    else _resizeAdorner.MaxHeight = MyCanvas.ActualHeight - newTop;

                    break;
                case 3: // Bottom right
                    if (Canvas.GetLeft(ResizeAdorner) + ResizeAdorner.ActualWidth + e.DeltaX >= MyCanvas.ActualWidth) moveX = false;
                    else _resizeAdorner.MaxWidth = MyCanvas.ActualWidth - newLeft;
                    
                    if (Canvas.GetTop(ResizeAdorner) + ResizeAdorner.ActualHeight + e.DeltaY >= MyCanvas.ActualHeight) moveY = false;
                    else _resizeAdorner.MaxHeight = MyCanvas.ActualHeight - newTop;
                    break;
                default:
                    _resizeAdorner.MaxWidth = MyCanvas.ActualWidth - newLeft;
                    _resizeAdorner.MaxHeight = MyCanvas.ActualHeight - newTop;
                    break;
            }

            // Prevent movement if new size is less than the minimum size
            if (moveX) moveX = !(ResizeAdorner.ActualWidth + e.DeltaX <= _resizeAdorner.MinWidth);
            if (moveY) moveY = !(ResizeAdorner.ActualHeight + e.DeltaY <= _resizeAdorner.MinHeight);

            if (moveX) Canvas.SetLeft(ResizeAdorner, newLeft);
            if (moveY) Canvas.SetTop(ResizeAdorner, newTop);
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

            w = MainWindow.Instance.SelectedFile.CroppedWidth;
            h = MainWindow.Instance.SelectedFile.CroppedHeight;
            CroppedTitle.Title = "Cropped " + w + "x" + h;

            ImagePreviewNew.ImageSource = _oldPreview;

            UpdatePreview();
        }

        /// <summary>
        /// Generates the cropped (new) preview image
        /// </summary>
        private void UpdatePreview()
        {
            /* Delete previous preview before making a new one else we
               will get an error because the previous file is in use */
            if (_newPreview != null)
            {
                _newPreview.StreamSource?.Dispose();
                _newPreview = null;
                GC.Collect();
            }

            //if (_currentCropOptions.Equals(default))
            //{
            //    ImagePreviewNew.ImageSource = _oldPreview;
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

                ImagePreviewNew.ImageSource = _oldPreview;

                (double w, double h, _, _) = MainWindow.Instance.SelectedFile.GetCroppedDimensions(GetCropOptions());
                //var h = MainWindow.Instance.SelectedFile.CroppedHeight;
                CroppedTitle.Title = "Cropped " + w + "x" + h;
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

        /// <returns> If processing is possible based on if any changes have been made. </returns>
        public bool CanWeCrop()
        {
            var options = GetCropOptions();
            VideoFile vid = MainWindow.Instance.SelectedFile;

            if (vid == null) return false;

            var croppedDimensions = vid.GetCroppedDimensions(options);
            (double, double) croppedDimsArr = (croppedDimensions.Item1, croppedDimensions.Item2);

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

            // Insert final size properties based on resize adorner changes
            var selFile = MainWindow.Instance.SelectedFile;
            double wScale = _resizeAdorner.Width / MyCanvas.Width;
            double hScale = _resizeAdorner.Height / MyCanvas.Height;
            double finalWidth = wScale * selFile.Width;
            double finalHeight = hScale * selFile.Height;

            //double xScale = Canvas.GetLeft(ResizeAdorner);
            //double yScale = Canvas.GetTop(ResizeAdorner);
            //double finalX = xScale * finalWidth;
            //double finalY = yScale * finalHeight;

            double xRatio = Canvas.GetLeft(ResizeAdorner) / MyCanvas.Width;
            double yRatio = Canvas.GetTop(ResizeAdorner) / MyCanvas.Height;

            double finalX = xRatio * selFile.Width;
            double finalY = yRatio * selFile.Height;


            Console.WriteLine($"wScale {wScale} hScale {hScale} finalWidth {finalWidth} finalHeight {finalHeight} origWidth {selFile.Width} origHeight {selFile.Height}");

            selFile.FinalWidth = finalWidth;
            selFile.FinalHeight = finalHeight;
            selFile.FinalX = finalX;
            selFile.FinalY = finalY;

            try
            {
                await CropWithProgressUIAsync();
                Console.WriteLine("Cropping complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            // Add to recent files list
            MainWindow.Instance.HomeViewModel.AddFile(MainWindow.Instance.SelectedFile.Path);
            MainWindow.Instance.ToggleOverlay();
            MainWindow.Instance.FileProcessed();
            Cleanup();
            MainWindow.Instance.ChangeView("Home");
        }

        //void UpdateProgressBar(long size)
        //{
        //    MainWindow.Instance.LoadingOverlay.UpdateProgress(100);
        //}

        public async Task CropWithProgressUIAsync()
        {
            var options = GetCropOptions();
            _currentCropOptions = options;

            // Start Crop on a background thread
            var cropTask = Task.Run(() => FileProcessor.Crop(MainWindow.Instance.SelectedFile, options));

            // Await crop completion (handle exceptions as needed)
            var result = await cropTask;

            Console.WriteLine("RESULT: " + result);

            if (result == null) // arbitrary minimum
            {
                Dispatcher.Invoke(() => MainWindow.Instance.LoadingOverlay.UpdateProgress(0));
                MainWindow.Instance.ErrorMessage("Cropping failed. Please check your input and try again.");
            }
            else
            {
                Dispatcher.Invoke(() => MainWindow.Instance.LoadingOverlay.UpdateProgress(100));
            }
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

            var imageBrush = ImagePreviewNew;
            if (imageBrush.ImageSource is BitmapSource bitmapSource)
            {
                // Get the image's original dimensions
                double imageWidth = bitmapSource.PixelWidth;
                double imageHeight = bitmapSource.PixelHeight;

                double rectangleWidth = NewImageContainer.ActualWidth;
                double rectangleHeight = NewImageContainer.ActualHeight;

                double scale = Math.Min(rectangleWidth / imageWidth, rectangleHeight / imageHeight);

                double scaledWidth = imageWidth * scale;
                double scaledHeight = imageHeight * scale;

                MyCanvas.Width = scaledWidth;
                MyCanvas.Height = scaledHeight;
            }

            _resizeAdorner.MaxWidth = MyCanvas.Width;
            _resizeAdorner.MaxHeight = MyCanvas.Height;
        }
        
        private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(MyCanvas);
                ResizeAdorner.CaptureMouse(); // Allow dragging when mouse is outside the rectangle
                e.Handled = true;
                Cursor = Cursors.Hand;
            }
        }

        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(MyCanvas);

                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                double newLeft = Canvas.GetLeft(ResizeAdorner) + deltaX;
                double newTop = Canvas.GetTop(ResizeAdorner) + deltaY;

                newLeft = Clamp(newLeft, 0, MyCanvas.ActualWidth - ResizeAdorner.ActualWidth);
                newTop = Clamp(newTop, 0, MyCanvas.ActualHeight - ResizeAdorner.ActualHeight);

                _resizeAdorner.MaxWidth = MyCanvas.ActualWidth - newLeft;
                _resizeAdorner.MaxHeight = MyCanvas.ActualHeight - newTop;

                Canvas.SetLeft(ResizeAdorner, newLeft);
                Canvas.SetTop(ResizeAdorner, newTop);

                _dragStartPoint = currentPoint;
            }
        }

        private void Rectangle_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ResizeAdorner.ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private void ResetCropButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var scaleX = MainWindow.Instance.SelectedFile.Width / NewImageContainer.ActualWidth;
            //var scaleY = MainWindow.Instance.SelectedFile.Height / NewImageContainer.ActualHeight;

            //(double w, double h, _, _) = MainWindow.Instance.SelectedFile.GetCroppedDimensions(GetCropOptions());

            //_resizeAdorner.overrideWidth(w / scaleX);
            //_resizeAdorner.overrideHeight(h / scaleY);

            //Canvas.SetLeft(ResizeAdorner, MyCanvas.ActualWidth / 2 - _resizeAdorner.Width / 2);
            //Canvas.SetTop(ResizeAdorner, MyCanvas.ActualHeight / 2 - _resizeAdorner.Height / 2);

            ResetCrop();
        }

        private void FillCropButton_OnClick(object sender, RoutedEventArgs e)
        {
            FillCrop();
        }

        private void FillCrop()
        {
            var imageBrush = ImagePreviewOld;

            // Get the image's original dimensions
            double imageWidth = ImagePreviewOld.ActualWidth;
            double imageHeight = ImagePreviewOld.ActualHeight;

            double rectangleWidth = NewImageContainer.ActualWidth;
            double rectangleHeight = NewImageContainer.ActualHeight;

            double scale = Math.Min(rectangleWidth / imageWidth, rectangleHeight / imageHeight);

            double scaledWidth = imageWidth * scale;
            double scaledHeight = imageHeight * scale;

            MyCanvas.Width = scaledWidth;
            MyCanvas.Height = scaledHeight;
            

            Canvas.SetLeft(ResizeAdorner, 0);
            Canvas.SetTop(ResizeAdorner, 0);

            _resizeAdorner.overrideWidth(MyCanvas.Width);
            _resizeAdorner.overrideHeight(MyCanvas.Height);
        }

        private void ResetCrop()
        {
            (double w, double h, _, _) = MainWindow.Instance.SelectedFile.GetCroppedDimensions(GetCropOptions());

            double scale2 = Math.Min(
                NewImageContainer.ActualWidth / MainWindow.Instance.SelectedFile.Width,
                NewImageContainer.ActualHeight / MainWindow.Instance.SelectedFile.Height
            );

            _resizeAdorner.overrideWidth(w * scale2);
            _resizeAdorner.overrideHeight(h * scale2);

            //Console.WriteLine($"Width {MainWindow.Instance.SelectedFile.Width} Height {MainWindow.Instance.SelectedFile.Height} CroppedW {MainWindow.Instance.SelectedFile.CroppedWidth} CroppedH {MainWindow.Instance.SelectedFile.CroppedHeight}");
            Canvas.SetLeft(ResizeAdorner, MyCanvas.Width / 2 - _resizeAdorner.Width / 2);
            Canvas.SetTop(ResizeAdorner, MyCanvas.Height / 2 - _resizeAdorner.Height / 2);
        }
    }
}
