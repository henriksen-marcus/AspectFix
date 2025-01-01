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
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
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
                    DropTextBlock.Text = "⚠ Not a video";
                    return;
                }

                RunAnim("DashBorderEnter");
                _borderAnimState = BorderAnimState.Receive;
                RemoveFileButton.Visibility = Visibility.Visible;

                MainWindow.Instance.SetSelectedFile(files[0]);
                FileNameTextBlock.Title = MainWindow.Instance.SelectedFile.Path;
            }
            else
            {
                RunAnim("DashBorderDeny");
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
                    break;
                default:
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
                _borderAnimState = BorderAnimState.None;
                DropTextBlock.Text = "Drag a file here";
                return;
            }

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