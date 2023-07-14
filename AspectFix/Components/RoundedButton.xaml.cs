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

namespace AspectFix.Components
{
    /// <summary>
    /// Interaction logic for RoundedButton.xaml
    /// </summary>
    public partial class RoundedButton : UserControl
    {

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                                  "Text", typeof(string), typeof(RoundedButton), new PropertyMetadata(string.Empty));
        
        // We need this for text properties, else it will "not be accessible"
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
                                                        "FontSize", typeof(double), typeof(RoundedButton), new PropertyMetadata(18.0));
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(
                                             "Enabled", typeof(bool), typeof(RoundedButton), new PropertyMetadata(true));

        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
                                                        "Width", typeof(double), typeof(RoundedButton), new PropertyMetadata(100.0));
        
        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
                                                        "Height", typeof(double), typeof(RoundedButton), new PropertyMetadata(100.0));

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(nameof(Click), RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(RoundedButton));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }


        public RoundedButton()
        {
            InitializeComponent();
        }

        private void CustomButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent));
        }
    }
}
