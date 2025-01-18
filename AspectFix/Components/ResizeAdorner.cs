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
using System.ComponentModel;
using System.Windows.Media.Media3D;

namespace AspectFix.Components
{
    public class ThumbDraggedEventArgs : EventArgs
    {
        public double DeltaX { get; }
        public double DeltaY { get; }
        public int Sender { get; }

        public ThumbDraggedEventArgs(double deltaX, double deltaY, int sender)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
            Sender = sender;
        }
    }

    internal class ResizeAdorner : Adorner
    {
        private readonly VisualCollection _adornerVisuals;
        private readonly Thumb _topLeft;
        private readonly Thumb _topRight;
        private readonly Thumb _bottomLeft;
        private readonly Thumb _bottomRight;
        private readonly FrameworkElement elm;

        private const double BtnSize = 16.0;

        public double MinWidth = 20;
        public double MaxWidth = 500;
        public double MinHeight = 20;
        public double MaxHeight = 500;
        public bool BlockResizeX = false;
        public bool BlockResizeY = false;

        public new double Width
        {
            get => elm.Width;
            set => elm.Width = value;
        }
        public new double Height
        {
            get => elm.Height;
            set => elm.Height = value;
        }

        public event EventHandler<ThumbDraggedEventArgs> ThumbDragged;

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
        //            ArrangeOverride()
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
            ThumbDragged?.Invoke(this, new ThumbDraggedEventArgs(newWidth - elm.Width, newHeight - elm.Height, 0));
            SetSize(newWidth, newHeight);
        }

        private void TopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height - e.VerticalChange;
            ThumbDragged?.Invoke(this, new ThumbDraggedEventArgs(newWidth - elm.Width, newHeight - elm.Height, 1));
            SetSize(newWidth, newHeight);
        }

        private void BottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width - e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            ThumbDragged?.Invoke(this, new ThumbDraggedEventArgs(newWidth - elm.Width, newHeight - elm.Height, 2));
            SetSize(newWidth, newHeight);
        }

        private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = elm.Width + e.HorizontalChange;
            var newHeight = elm.Height + e.VerticalChange;
            ThumbDragged?.Invoke(this, new ThumbDraggedEventArgs(newWidth - elm.Width, newHeight - elm.Height, 3));
            SetSize(newWidth, newHeight);
        }

        private void SetSize(double newWidth, double newHeight)
        {
            if (!BlockResizeX)
            {
                if (newWidth > MaxWidth) newWidth = MaxWidth;
                if (newWidth < MinWidth) newWidth = MinWidth;
                elm.Width = newWidth;
            }

            if (!BlockResizeY)
            {
                if (newHeight > MaxHeight) newHeight = MaxHeight;
                if (newHeight < MinHeight) newHeight = MinHeight;
                elm.Height = newHeight;
            }
        }

        protected override Visual GetVisualChild(int index) => _adornerVisuals[index];
        protected override int VisualChildrenCount => _adornerVisuals.Count;
        protected override Size ArrangeOverride(Size finalSize)
        {
            double half = BtnSize / 2;
            double offset = 7; // Higher = closer to middle

            _topLeft.Arrange(new Rect(-half + offset, -half + offset, BtnSize, BtnSize));
            _topRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - half - offset, -half + offset, BtnSize, BtnSize));
            _bottomLeft.Arrange(new Rect(-half + offset, AdornedElement.DesiredSize.Height - half - offset, BtnSize, BtnSize));
            _bottomRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - half - offset, AdornedElement.DesiredSize.Height - half - offset, BtnSize, BtnSize));

            return base.ArrangeOverride(finalSize);
        }
    }
}
