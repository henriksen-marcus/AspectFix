using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using AspectFix.Views;

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        private HomeViewModel viewModel;
        private bool _isHoldingValidFile = false;

        public HomeView()
        {
            InitializeComponent();
            MainWindow.Instance.OnFileProcessed += ResetUI;
            MainWindow.Instance.OnToggleDragOverlay += ToggleDragOverlay;
            viewModel = MainWindow.Instance.HomeViewModel;

            DataContext = viewModel;
        }

        public void UpdateRecentFilesList()
        {
            //RecentFilesList.Items
        }

        private bool ValidateFile(string path)
        {
            return File.Exists(path) && FileProcessor.IsVideoFile(path);
        }

        private void ToggleDragOverlay(bool isValidFile)
        {
            throw new NotImplementedException();
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
            FileNameTextBlock.Title = "No file selected";
            ContinueButton.IsEnabled = false;
            RemoveFileButton.Visibility = Visibility.Collapsed;
            _isHoldingValidFile = false;
        }

        
        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
        }

        // When the user releases the mouse button with a file in hand
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (!_isHoldingValidFile) return;

            ContinueButton.IsEnabled = true;
            FileNameTextBlock.Title = MainWindow.Instance.SelectedFile.Path;
            RemoveFileButton.Visibility = Visibility.Visible;
        }

        private void DropBorder_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                string filename = System.IO.Path.GetFileName(files[0]);

                if (!ValidateFile(files[0])) return;

                DashedOutline.Stroke = new SolidColorBrush(Color.FromRgb(50, 205, 50));
                MainWindow.Instance.SetSelectedFile(files[0]);
                _isHoldingValidFile = true;
            }
            else MainWindow.Instance.ErrorMessage("Couldn't retrieve any files.");
        }

        private void DropBorder_OnMouseLeave(object sender, MouseEventArgs e)
        {
            DropBorder.AllowDrop = true;
            DashedOutline.Stroke = new SolidColorBrush(Colors.White);
        }

        private void DropBorder_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropBorder.AllowDrop = true;
            DashedOutline.Stroke = new SolidColorBrush(Colors.White);
        }

        //private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        //{
        //    viewModel.AddFile("Hello there...");
        //}
    }
}