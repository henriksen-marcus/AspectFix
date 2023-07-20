using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public EditView()
        {
            InitializeComponent();
            MainWindow.Instance.OnExitApp += Cleanup;
            InitPreviews();
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

        // Generates the cropped (new) preview image
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

            string newPreviewPath = FileProcessor.GetCroppedPreviewImage(MainWindow.Instance.SelectedFile, IterationCount);
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

            Task<string> task = Task.Run(() => FileProcessor.Crop(MainWindow.Instance.SelectedFile));
            string path = await task;
            if (path == null) MainWindow.Instance.ErrorMessage("Failed to crop video.");

            MainWindow.Instance.ToggleOverlay();
            MainWindow.Instance.FileProcessed();
            Cleanup();
            MainWindow.Instance.ChangeView("Home");
        }


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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Cleanup();
            MainWindow.Instance.ChangeView("Home");
        }
    }
}
