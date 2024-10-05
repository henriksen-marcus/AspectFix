using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AspectFix.Views;

namespace AspectFix.Components
{
    public partial class WindowTitleBox : UserControl
    {

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(WindowTitleBox), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private bool _infoVisible = false;

        public WindowTitleBox()
        {
            InitializeComponent();
        }

        private void ShowElements()
        {
            var storyboard = (Storyboard)FindResource("ShowInfoStoryboard");
            storyboard.Begin();
        }

        private void HideElements()
        {
            var storyboard = (Storyboard)FindResource("HideInfoStoryboard");
            storyboard.Begin();
        }

        private void RoundedButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_infoVisible) HideElements();
            else ShowElements();
            
            _infoVisible = !_infoVisible;
        }

        private void RoundedImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            const string url = "https://github.com/henriksen-marcus/AspectFix";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Required to open URLs in .NET Core/5.0+
                });
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ErrorMessage("Couldn't open link. " + ex.Message);
            }
        }
    }
}
