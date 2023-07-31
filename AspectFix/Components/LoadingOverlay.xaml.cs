using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AspectFix
{
    public partial class LoadingOverlay : UserControl
    {
        public LoadingOverlay()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int value)
        {
            //ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = value);
            var text = value == 100 ? "Finishing up..." : value.ToString() + "%";
            ProgressTextBlock.Dispatcher.Invoke(() => ProgressTextBlock.Text = text);
        }
    }
}