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

namespace AspectFix
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel viewmodel;
        public string SelectedFile { get; set; } = "";

        public static MainWindow Instance { get; private set; }

        public delegate void FileProcessedEventHandler();
        public event FileProcessedEventHandler FileProcessed;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            viewmodel = new MainViewModel();
            DataContext = viewmodel;
        }

        public void OnFileProcessed()
        {
            FileProcessed?.Invoke();
        }

        void HomeClick(object sender, RoutedEventArgs e)
        {
            viewmodel.SelectedViewModel = new HomeViewModel();
        }

        void EditClick(object sender, RoutedEventArgs e)
        {
            viewmodel.SelectedViewModel = new EditViewModel();
        }

        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void ToggleOverlay()
        {
            DarkenOverlay.Visibility = DarkenOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            OverlayUI.Visibility = OverlayUI.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
