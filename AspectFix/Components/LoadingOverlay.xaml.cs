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

        public async Task UpdateProgress(int value)
        {
            while (value < 100)
            {
                await Task.Delay(100);
                ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = value);
                value++;
            }
            ProgressBar.Dispatcher.Invoke(() => ProgressBar.Value = 100);
        }
    }
}