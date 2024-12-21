using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Timers;
using System.Threading;
using System.Xml.Linq;

namespace AspectFix.Services
{
    internal class ResizeAdorner : Adorner
    {
        VisualCollection AdornerVisuals;
        Thumb topLeft, topRight, bottomLeft, bottomRight;

        private System.Timers.Timer timer;

        private const double BtnSize = 10.0;
        private double targetWidth = 100, targetHeight = 100;
        private bool isInterpolating = true;

        public ResizeAdorner(UIElement adornerElement) : base(adornerElement)
        {

            var thumbStyle = (Style)Application.Current.FindResource("AfThumbStyle");

            AdornerVisuals = new VisualCollection(this);
            topLeft = new Thumb() { Style = thumbStyle };
            topRight = new Thumb() { Background = Brushes.Coral, Height = BtnSize, Width = BtnSize };
            bottomLeft = new Thumb() { Background = Brushes.Coral, Height = BtnSize, Width = BtnSize };
            bottomRight = new Thumb() { Background = Brushes.Coral, Height = BtnSize, Width = BtnSize };

            topLeft.DragDelta += TopLeft_DragDelta;
            topRight.DragDelta += TopRight_DragDelta;
            bottomLeft.DragDelta += BottomLeft_DragDelta;
            bottomRight.DragDelta += BottomRight_DragDelta;

            AdornerVisuals.Add(topLeft);
            AdornerVisuals.Add(topRight);
            AdornerVisuals.Add(bottomLeft);
            AdornerVisuals.Add(bottomRight);


            timer = new System.Timers.Timer(4);
            timer.Elapsed += async (sender, e) => await HandleTimer();
            timer.Start();
        }

        private Task HandleTimer()
        {
            if (AdornedElement is FrameworkElement elm)
                elm.Dispatcher.Invoke(() =>
                {
                    // Interpolate width and height
                    var newWidth = Lerp(elm.Width, targetWidth, 0.2);
                    var newHeight = Lerp(elm.Height, targetHeight, 0.2);

                    // Check if both properties are close enough to the target
                    if (Math.Abs(newWidth - targetWidth) < 0.5 && Math.Abs(newHeight - targetHeight) < 0.1)
                    {
                        newWidth = targetWidth; // Snap to target width
                        newHeight = targetHeight; // Snap to target height
                        timer.Stop(); // Stop the timer if both targets are reached
                    }

                    // Update the properties
                    elm.Width = newWidth;
                    elm.Height = newHeight;

                    Trace.WriteLine($"Updated: Width={elm.Width}, Height={elm.Height}");
                });

            return Task.CompletedTask;
        }

        double Lerp(double start, double end, double speed)
        {
            return start + (end - start) * speed;
        }

        private void onSizeChanged()
        {
            if (!isInterpolating)
            {
                var elm = (FrameworkElement)AdornedElement;
                elm.Width = targetWidth;
                elm.Height = targetHeight;
                return;
            }

            if (timer.Enabled == false)
                timer.Start();
        }

        private void TopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var elm = (FrameworkElement)AdornedElement;
            var newWidth = elm.Width - e.HorizontalChange;
            var newHeight = elm.Height - e.VerticalChange;
            if (newWidth > 0 && newHeight > 0)
            {
                /*elm.Width = newWidth;
                elm.Height = newHeight;*/
                targetWidth = newWidth;
                targetHeight = newHeight;

                onSizeChanged();

            }
        }

        private void TopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var elm = (FrameworkElement)AdornedElement;
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height - e.VerticalChange;
            if (newWidth > 0 && newHeight > 0)
            {
                /*elm.Width = newWidth;
                elm.Height = newHeight;*/
                var w = elm.Width;
            }
        }

        private void BottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var elm = (FrameworkElement)AdornedElement;
            var newWidth = elm.Width - e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            if (newWidth > 0 && newHeight > 0)
            {
                elm.Width = newWidth;
                elm.Height = newHeight;
            }
        }

        private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var elm = (FrameworkElement)AdornedElement;
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            if (newWidth > 0 && newHeight > 0)
            {
                elm.Width = newWidth;
                elm.Height = newHeight;
            }
        }

        protected override Visual GetVisualChild(int index) => AdornerVisuals[index];
        protected override int VisualChildrenCount => AdornerVisuals.Count;
        protected override Size ArrangeOverride(Size finalSize)
        {
            topLeft.Arrange(new Rect(-BtnSize / 2, -BtnSize / 2, BtnSize, BtnSize));
            topRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - BtnSize / 2, -BtnSize / 2, BtnSize, BtnSize));
            bottomLeft.Arrange(new Rect(-BtnSize / 2, AdornedElement.DesiredSize.Height - BtnSize / 2, BtnSize, BtnSize));
            bottomRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - BtnSize / 2, AdornedElement.DesiredSize.Height - BtnSize / 2, BtnSize, BtnSize));

            return base.ArrangeOverride(finalSize);
        }
    }
}
