﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

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
            set => elm.Width = Services.Utils.Clamp(value, MinWidth, MaxWidth);
        }
        public new double Height
        {
            get => elm.Height;
            set => elm.Height = Services.Utils.Clamp(value, MinHeight, MaxHeight);
        }

        public event EventHandler<ThumbDraggedEventArgs> ThumbDragged;
        public event EventHandler ThumbDragCompleted;

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

            _topLeft.DragCompleted += Thumb_DragCompleted;
            _topRight.DragCompleted += Thumb_DragCompleted;
            _bottomLeft.DragCompleted += Thumb_DragCompleted;
            _bottomRight.DragCompleted += Thumb_DragCompleted;

            _adornerVisuals.Add(_topLeft);
            _adornerVisuals.Add(_topRight);
            _adornerVisuals.Add(_bottomLeft);
            _adornerVisuals.Add(_bottomRight);

            elm = (FrameworkElement)AdornedElement;
        }

        /// <summary>
        /// Set the width directly without checking for validity.
        /// </summary>
        public void overrideWidth(double newWidth)
        {
            elm.Width = newWidth;
        }

        /// <summary>
        /// Set the height directly without checking for validity.
        /// </summary>
        public void overrideHeight(double newHeight)
        {
            elm.Height = newHeight;
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

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ThumbDragCompleted?.Invoke(this, EventArgs.Empty);
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
