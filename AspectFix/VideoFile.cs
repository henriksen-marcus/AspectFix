using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AspectFix.FileProcessor;

namespace AspectFix
{
    public enum Orientation
    {
        Portrait,
        Landscape
    }

    /// <summary>
    /// Holds information on how many black pixels are on each side of the video
    /// </summary>
    public struct BlackPixels
    {
        public int left, top, right, bottom;

        public BlackPixels(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }

    public class VideoFile
    {
        public string Path { get; private set; }
        public string FileName { get; private set; }
        public string Extension { get; private set; }
        public Orientation Orientation => (Width > Height) ? Orientation.Landscape : Orientation.Portrait;
        public Orientation CroppedOrientation => (CroppedWidth > CroppedHeight) ? Orientation.Landscape : Orientation.Portrait;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CroppedWidth { get; private set; }
        public int CroppedHeight { get; private set; }
        public double Length { get; private set; }
        public double NonBlackFrame { get; private set; }
        public float AspectRatio { get; private set; }
        public float CroppedAspectRatio => (float)Math.Max(CroppedWidth, CroppedHeight) / (float)Math.Min(CroppedWidth, CroppedHeight);
        public BlackPixels BlackPixels { get; private set; }
        public int Rotation { get; private set; } = 0;

        public VideoFile(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileNameWithoutExtension(Path);
            Extension = System.IO.Path.GetExtension(Path);

            Task.Run(async () => await InitVideoInfo());
        }

        private async Task InitVideoInfo()
        {
            Length = FileProcessor.GetVideoLength(Path);
            (Width, Height) = FileProcessor.GetVideoDimensions(Path);

            CroppedWidth = Width;
            CroppedHeight = Height;
            AspectRatio = (float)Math.Max(Width, Height) / (float)Math.Min(Width, Height);

            NonBlackFrame = FileProcessor.GetNonBlackFrameTime(this);
            var t = FileProcessor.GetBlackPixels(this);
            BlackPixels = new BlackPixels(t.Item1, t.Item2, t.Item3, t.Item4);
        }

        public (int, int) GetDimensions() => (Width, Height);
        
        public override string ToString()
        {
            return $"Filename: {FileName}{Extension}\n" +
                $"Dimensions: {Width}x{Height}\n" +
                $"Orientation: {Orientation}\n" +
                $"Aspect ratio: {AspectRatio}";
        }

        /// <param name="options">User defined options on how the video should be cropped</param>
        /// <returns>The cropped dimensions and the x, y position of the video in the frame</returns>
        public (int, int, int, int) GetCroppedDimensions(CropOptions options)
        {
            if (!options.shouldCrop) return (Width, Height, 0, 0);
            if (options.isAuto) return GetAnalyzedCroppedDimensions();

            CroppedWidth = Width;
            CroppedHeight = Height;

            for (var i = 0; i < options.iterations; i++)
            {
                // If the video is in landscape, return dimensions cropped to portrait and vice versa
                if (CroppedOrientation == Orientation.Landscape)
                {
                    // We use the cropped width/height because of iterations
                    CroppedWidth = (int)(CroppedHeight / CroppedAspectRatio);
                }
                else
                {
                    CroppedHeight = (int)(CroppedWidth / CroppedAspectRatio);
                }
            }

            return (CroppedWidth, CroppedHeight, 0, 0);
        }

        /// <summary>
        /// Uses the stored black pixel values to return the dimensions of the video cropped to remove black bars
        /// </summary>
        /// <returns>The cropped dimensions and the x, y position of the video in the frame</returns>
        public (int, int, int, int) GetAnalyzedCroppedDimensions()
        {
            int width = Width - BlackPixels.left - BlackPixels.right;
            int height = Height - BlackPixels.top - BlackPixels.bottom;
            int x = BlackPixels.left;
            int y = BlackPixels.top;
            return (width, height, x, y);
        }

        /// <returns>A new string with the dimensions clamped to 360p</returns>
        public string GetLQScale(int width, int height)
        {
            if (width > height)
                return (width < 360) ? $"{width}:-1" : "360:-1";

            return (height < 360) ? $"-1:{height}" : "-1:360";
        }

        public void AddRotation(int degrees)
        {
            if (Rotation + degrees >= 360) Rotation = Rotation + degrees - 360;
            else if (Rotation + degrees < 0) Rotation = 360 + Rotation + degrees;
            else Rotation += degrees;
        }

        /// <returns>The ffmpeg equivalent of the current rotation of this video</returns>
        public string GetRotateCommand()
        {
            string transposeValue;
            switch (Rotation)
            {
                case 90:
                    transposeValue = "1";
                    break;
                case 180:
                    transposeValue = "2,transpose=2";
                    break;
                case 270:
                    transposeValue = "2";
                    break;
                default:
                    transposeValue = null;
                    break;
            }
            return transposeValue;
        }
    }
}
