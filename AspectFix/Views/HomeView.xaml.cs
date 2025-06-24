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
using System.Windows.Media.Animation;

namespace AspectFix
{
    public partial class HomeView : UserControl
    {
        private HomeViewModel viewModel;

        private enum BorderAnimState
        {
            None,
            Receive,
            Deny
        }

        private BorderAnimState _borderAnimState = BorderAnimState.None;

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
            return File.Exists(path) && Services.FileProcessor.IsVideoFile(path);
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
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
        }

        private void DropBorder_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
            {
                RunAnim("DashBorderDeny");
                _borderAnimState = BorderAnimState.Deny;
                WarningIcon.Visibility = Visibility.Visible;
                DropTextBlock.Text = "Not a video";
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                string filename = System.IO.Path.GetFileName(files[0]);

                if (!ValidateFile(files[0]))
                {
                    RunAnim("DashBorderDeny");
                    _borderAnimState = BorderAnimState.Deny;
                    DropTextBlock.Text = "Not a video";
                    WarningIcon.Visibility = Visibility.Visible;
                    return;
                }

                RunAnim("DashBorderEnter");
                _borderAnimState = BorderAnimState.Receive;
                RemoveFileButton.Visibility = Visibility.Visible;

                MainWindow.Instance.SetSelectedFile(files[0]);
            }
            else
            {
                RunAnim("DashBorderDeny");
                WarningIcon.Visibility = Visibility.Visible;
                _borderAnimState = BorderAnimState.Deny;
                MainWindow.Instance.ErrorMessage("Couldn't retrieve any files.");
            }
        }

        private void DropBorder_OnDragLeave(object sender, DragEventArgs e)
        {
            switch (_borderAnimState)
            {
                case BorderAnimState.Receive:
                    RunAnim("DashBorderClear");
                    break;
                case BorderAnimState.Deny:
                    RunAnim("DashBorderClearDeny");
                    WarningIcon.Visibility = Visibility.Collapsed;
                    break;
            }

            _borderAnimState = BorderAnimState.None;
            DropTextBlock.Text = "Drag a file here";
        }
        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (_borderAnimState == BorderAnimState.Deny)
            {
                RunAnim("DashBorderClearDeny");
                WarningIcon.Visibility = Visibility.Collapsed;
                _borderAnimState = BorderAnimState.None;
                DropTextBlock.Text = "Drag a file here";
                return;
            }

            FileNameTextBlock.Title = MainWindow.Instance.SelectedFile.Path;

            RunAnim("DashBorderDrop");
            _borderAnimState = BorderAnimState.None;
            DropTextBlock.Text = "Drag a file here";
            ContinueButton.IsEnabled = true;
        }

        private void RunAnim(string name)
        {
            Storyboard sb = this.FindResource(name) as Storyboard;
            if (sb != null) { BeginStoryboard(sb); }
        }

        //private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        //{
        //    viewModel.AddFile("Hello there...");
        //}
    }
}