using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.IO;

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        private string selectedFile = "";

        public HomeView()
        {
            InitializeComponent();
            MainWindow.Instance.FileProcessed += ResetUI;
        }

        // When the user releases the mouse button with a file in hand
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string filename = System.IO.Path.GetFileName(files[0]);
            FileNameTextBlock.Text = ShortenString(filename, 22);
            MainWindow.Instance.SelectedFile = files[0];
            RemoveFileButton.Visibility = Visibility.Visible;

            if (File.Exists(files[0]) && IsVideoFile(files[0]))
            {
                ContinueButton.IsEnabled = true;
            }
        }

        // Enable this button when we have a valid file in our drag box,
        // this function changes the view to the edit view
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ChangeView("Edit");
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {

        }

        private void ResetUI()
        {
            selectedFile = "";
            FileNameTextBlock.Text = "No file selected";
            ContinueButton.IsEnabled = false;
            RemoveFileButton.Visibility = Visibility.Collapsed;
        }

        public static string ShortenString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            if (maxLength < 4)
                return input.Substring(0, maxLength); // Return the start of the string if maxLength is too small

            int startLength = (maxLength - 3) / 2;
            int endLength = (maxLength - 3) - startLength;

            return input.Substring(0, startLength) + "..." + input.Substring(input.Length - endLength);
        }

        public static bool IsVideoFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath);

            // Supported extensions by ffmpeg
            string[] supportedVideoTypes = {
                ".3g2", ".3gp", ".asf", ".avi", ".flv", ".m2v", ".m4v", ".mkv", ".mov", ".mp4",
                ".mpeg", ".mpg", ".rm", ".swf", ".vob", ".webm", ".wmv"
            };

            // Compare the file extension (ignoring case)
            return supportedVideoTypes.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
        }
    }
}