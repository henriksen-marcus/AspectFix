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
            using (FileStream stream = File.OpenRead(oldPreviewPath))
            {
                _oldPreview = new BitmapImage();
                _oldPreview.BeginInit();
                _oldPreview.CacheOption = BitmapCacheOption.OnLoad;
                _oldPreview.StreamSource = stream; // Set the stream as the StreamSource
                _oldPreview.EndInit();
            }

            ImagePreviewOld.Source = _oldPreview;
            UpdatePreview();
        }

        // Generates the cropped (new) preview image
        private void UpdatePreview()
        {
            string newPreviewPath = FileProcessor.GetCroppedPreviewImage(MainWindow.Instance.SelectedFile, 1);
            if (newPreviewPath != null)
            {
                if (_newPreview != null)
                {
                    _newPreview.StreamSource?.Dispose();
                    _newPreview = null;
                    GC.Collect();
                }
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
            else MessageBox.Show("Failed to generate preview image.");
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            IterationCount++;
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            IterationCount--;
        }
        
        private void CropButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ToggleOverlay();
            var path = FileProcessor.Crop(MainWindow.Instance.SelectedFile);
            if (path == null) MessageBox.Show("Failed to crop video.");
            MainWindow.Instance.ToggleOverlay();

            MainWindow.Instance.FileProcessed();
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
    }
}
