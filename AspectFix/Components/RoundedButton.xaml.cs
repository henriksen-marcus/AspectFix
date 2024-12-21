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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace AspectFix.Components
{
    /// <summary>
    /// Interaction logic for RoundedButton.xaml
    /// </summary>
    public partial class RoundedButton : UserControl
    {
        private static readonly double AnimationTime = 0.01d;
        private static readonly double AnimationScale = 1.04d;

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                                  "Text", typeof(string), typeof(RoundedButton), new PropertyMetadata(string.Empty));
        
        // We need this for text properties, else it will "not be accessible"
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public new static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
                                                        "FontSize", typeof(double), typeof(RoundedButton), new PropertyMetadata(18.0));
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(
                                             "Enabled", typeof(bool), typeof(RoundedButton), new PropertyMetadata(true));

        public static readonly DependencyProperty ShouldAnimateScaleProperty = DependencyProperty.Register(
            "ShouldAnimateScale", typeof(bool), typeof(RoundedButton), new PropertyMetadata(true));

        public bool ShouldAnimateScale
        {
            get => (bool)GetValue(ShouldAnimateScaleProperty);
            set => SetValue(ShouldAnimateScaleProperty, value);
        }

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

        private void CustomButton_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!ShouldAnimateScale) return;

            Button button = (Button)sender;

            ScaleTransform scaleTransform = new ScaleTransform(1, 1, 0.5, 0.5);
            button.RenderTransform = scaleTransform;

            button.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                To = AnimationScale,
                Duration = TimeSpan.FromSeconds(AnimationTime)
            };
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                To = AnimationScale,
                Duration = TimeSpan.FromSeconds(AnimationTime)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        
        }

        private void Button_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!ShouldAnimateScale) return;

            Button button = (Button)sender;

            ScaleTransform scaleTransform = new ScaleTransform(AnimationScale, AnimationScale);
            button.RenderTransform = scaleTransform;

            button.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(AnimationTime)
            };
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(AnimationTime)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        private void CustomButton_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue) return;

            Storyboard sb = this.FindResource("ButtonEnabled") as Storyboard;
            if (sb != null) { BeginStoryboard(sb); }
        }
    }
}
