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

namespace AspectFix.Components
{
    internal class ResizeAdorner : Adorner
    {
        private readonly VisualCollection _adornerVisuals;
        private readonly Thumb _topLeft;
        private readonly Thumb _topRight;
        private readonly Thumb _bottomLeft;
        private readonly Thumb _bottomRight;
        private readonly FrameworkElement elm;

        //private readonly System.Timers.Timer timer;
        private const double BtnSize = 12.0;

        //public double TargetWidth = 100;
        //public double TargetHeight = 100;

        public double MinWidth = 20;
        public double MaxWidth = 500;
        public double MinHeight = 20;
        public double MaxHeight = 500;

        public ResizeAdorner(UIElement adornerElement) : base(adornerElement)
        {
            _adornerVisuals = new VisualCollection(this);
            _topLeft = new Thumb() { Style = (Style)Application.Current.FindResource("AfThumbStyleTL") };
            _topRight = new Thumb() { Style = (Style)Application.Current.FindResource("AfThumbStyleTR") };
            _bottomLeft = new Thumb() { Style = (Style)Application.Current.FindResource("AfThumbStyleBL") };
            _bottomRight = new Thumb() { Style = (Style)Application.Current.FindResource("AfThumbStyleBR") };

            _topLeft.DragDelta += TopLeft_DragDelta;
            _topRight.DragDelta += TopRight_DragDelta;
            _bottomLeft.DragDelta += BottomLeft_DragDelta;
            _bottomRight.DragDelta += BottomRight_DragDelta;

            _adornerVisuals.Add(_topLeft);
            _adornerVisuals.Add(_topRight);
            _adornerVisuals.Add(_bottomLeft);
            _adornerVisuals.Add(_bottomRight);

            elm = (FrameworkElement)AdornedElement;

            //timer = new System.Timers.Timer(4);
            //timer.Elapsed += async (sender, e) => await HandleTimer();
            //timer.Start();
        }

        //private Task HandleTimer()
        //{
        //    if (AdornedElement is FrameworkElement elm)
        //        elm.Dispatcher.Invoke(() =>
        //        {
        //            // Interpolate width and height
        //            var newWidth = Lerp(elm.Width, TargetWidth, 0.2);
        //            var newHeight = Lerp(elm.Height, TargetHeight, 0.2);

        //            // Check if both properties are close enough to the target
        //            if (Math.Abs(newWidth - TargetWidth) < 0.5 && Math.Abs(newHeight - TargetHeight) < 0.1)
        //            {
        //                newWidth = TargetWidth; // Snap to target width
        //                newHeight = TargetHeight; // Snap to target height
        //                timer.Stop(); // Stop the timer if both targets are reached
        //            }

        //            // Update the properties
        //            elm.Width = newWidth;
        //            elm.Height = newHeight;

        //            Trace.WriteLine($"Updated: Width={elm.Width}, Height={elm.Height}");
        //        });

        //    return Task.CompletedTask;
        //}

        //private void onSizeChanged()
        //{
        //    if (!isInterpolating)
        //    {
        //        var elm = (FrameworkElement)AdornedElement;
        //        elm.Width = TargetWidth;
        //        elm.Height = TargetHeight;
        //        return;
        //    }

        //    if (timer.Enabled == false)
        //        timer.Start();
        //}

        private void TopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width - e.HorizontalChange;
            var newHeight = elm.Height - e.VerticalChange;
            setSize(newWidth, newHeight);

            //if (newWidth > 0 && newHeight > 0)
            //{
            //    TargetWidth = newWidth;
            //    TargetHeight = newHeight;
            //    onSizeChanged();
            //}
        }

        private void TopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height - e.VerticalChange;
            setSize(newWidth, newHeight);
        }

        private void BottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width - e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            setSize(newWidth, newHeight);
        }

        private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            setSize(newWidth, newHeight);
        }

        private void setSize(double newWidth, double newHeight)
        {
            if (newWidth > MaxWidth) newWidth = MaxWidth;
            if (newWidth < MinWidth) newWidth = MinWidth;
            if (newHeight > MaxHeight) newHeight = MaxHeight;
            if (newHeight < MinHeight) newHeight = MinHeight;

            elm.Width = newWidth;
            elm.Height = newHeight;
        }

        protected override Visual GetVisualChild(int index) => _adornerVisuals[index];
        protected override int VisualChildrenCount => _adornerVisuals.Count;
        protected override Size ArrangeOverride(Size finalSize)
        {
            double half = BtnSize / 2;
            double offset = 6;

            _topLeft.Arrange(new Rect(-half + offset, -half + offset, BtnSize, BtnSize));
            _topRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - half - offset, -half + offset, BtnSize, BtnSize));
            _bottomLeft.Arrange(new Rect(-half + offset, AdornedElement.DesiredSize.Height - half - offset, BtnSize, BtnSize));
            _bottomRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - half - offset, AdornedElement.DesiredSize.Height - half - offset, BtnSize, BtnSize));

            return base.ArrangeOverride(finalSize);
        }
    }
}
