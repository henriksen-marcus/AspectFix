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
    /// Interaction logic for EditView.xaml
    /// </summary>
    public partial class EditView : UserControl
    {
        public EditView()
        {
            // Get video preview frames before rendering the view

            InitializeComponent();
        }

        private void CropButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.ToggleOverlay();
            FileProcessor.Crop(MainWindow.Instance.SelectedFile);
            MainWindow.Instance.ToggleOverlay();

            MainWindow.Instance.OnFileProcessed();
        }
    }
}
