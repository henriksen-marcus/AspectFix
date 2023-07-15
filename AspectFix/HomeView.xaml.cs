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
        public HomeView()
        {
            InitializeComponent();
            MainWindow.Instance.OnFileProcessed += ResetUI;
            MainWindow.Instance.OnToggleDragOverlay += ToggleDragOverlay;
            MainWindow.Instance.OnFileEnteredWindow += CheckHeldFile;
        }

        private void CheckHeldFile(string path)
        {
            if (CheckFile(path))
                DropBorder.AllowDrop = true;
            else
                DropBorder.AllowDrop = false;
        }

        private bool CheckFile(string path)
        {
            return (File.Exists(path) && FileProcessor.IsVideoFile(path));
        }

        private void ToggleDragOverlay(bool isValidFile)
        {
            throw new NotImplementedException();
        }

        // When the user releases the mouse button with a file in hand
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string filename = System.IO.Path.GetFileName(files[0]);
            FileNameTextBlock.Text = FileProcessor.ShortenString(filename, 22);
            MainWindow.Instance.SetSelectedFile(files[0]);
            RemoveFileButton.Visibility = Visibility.Visible;

            if (CheckFile(files[0])) ContinueButton.IsEnabled = true;
        }

        public void ToggleDrop()
        {
            DropBorder.AllowDrop = false;
        }

        // Enable this button when we have a valid file in our drag box,
        // this function changes the view to the edit view
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ChangeView("Edit");
        }

        private void ResetUI()
        {
            MainWindow.Instance.SetSelectedFile(null);
            FileNameTextBlock.Text = "No file selected";
            ContinueButton.IsEnabled = false;
            RemoveFileButton.Visibility = Visibility.Collapsed;
        }

        
        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
        }
    }
}