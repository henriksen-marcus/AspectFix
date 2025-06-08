using System;
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
            bool useMinutes = value > 60;
            var convertedValue = useMinutes ? value / 60 : value;
            var suffix = useMinutes ? $" minute{(convertedValue > 1 ? "s" : "")} left" : $" second{(convertedValue > 1 ? "s" : "")} left";
            var text = value == 0 ? "Finishing up" : convertedValue.ToString() + suffix;
            ProgressTextBlock.Dispatcher.Invoke(() => ProgressTextBlock.Text = text);
        }
    }
}