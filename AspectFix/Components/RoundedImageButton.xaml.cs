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

namespace AspectFix.Components
{
    /// <summary>
    /// Interaction logic for RoundedImageButton.xaml
    /// </summary>
    public partial class RoundedImageButton : UserControl
    {
        private static readonly double AnimationTime = 0.01d;
        private static readonly double AnimationScale = 1.04d;

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
                                                        "FontSize", typeof(double), typeof(RoundedImageButton), new PropertyMetadata(18.0));
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(
                                             "Enabled", typeof(bool), typeof(RoundedImageButton), new PropertyMetadata(true));

        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
                                                        "Width", typeof(double), typeof(RoundedImageButton), new PropertyMetadata(100.0));

        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
                                                        "Height", typeof(double), typeof(RoundedImageButton), new PropertyMetadata(30.0));

        public static readonly DependencyProperty ShouldAnimateScaleProperty = DependencyProperty.Register(
            "ShouldAnimateScale", typeof(bool), typeof(RoundedImageButton), new PropertyMetadata(true));

        public bool ShouldAnimateScale
        {
            get => (bool)GetValue(ShouldAnimateScaleProperty);
            set => SetValue(ShouldAnimateScaleProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
                                                        "ImageSource", typeof(ImageSource), typeof(RoundedImageButton), new PropertyMetadata(null));

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(nameof(Click), RoutingStrategy.Bubble,
                                                        typeof(RoutedEventHandler), typeof(RoundedImageButton));

        
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }


        public RoundedImageButton()
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
    }
}
