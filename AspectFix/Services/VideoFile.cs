using System;
using System.Threading.Tasks;
using static AspectFix.FileProcessor;

namespace AspectFix.Services
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
        public string NewPath { get; private set; }
        public string FileName { get; private set; }
        public string Extension { get; private set; }
        public Orientation Orientation => (Width > Height) ? Orientation.Landscape : Orientation.Portrait;
        public Orientation CroppedOrientation => (CroppedWidth > CroppedHeight) ? Orientation.Landscape : Orientation.Portrait;
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double CroppedWidth { get; private set; }
        public double CroppedHeight { get; private set; }
        public double FinalWidth { get; set; }
        public double FinalHeight { get; set; }
        public double FinalX { get; set; }
        public double FinalY { get; set; }
        public TimeSpan Length { get; private set; }
        public double NonBlackFrame { get; private set; }
        public double AspectRatio { get; private set; }
        public double CroppedAspectRatio => Math.Max(CroppedWidth, CroppedHeight) / Math.Min(CroppedWidth, CroppedHeight);
        public BlackPixels BlackPixels { get; private set; }
        public int Rotation { get; private set; } = 0;
        public TimeSpan TrimStart { get; private set; }
        public TimeSpan TrimEnd { get; private set; }

        public VideoFile(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileNameWithoutExtension(Path);
            Extension = System.IO.Path.GetExtension(Path);
            
            Task.Run(InitVideoInfoAsync);
        }

        private async Task InitVideoInfoAsync()
        {
            NewPath = FileProcessor.GetNewFilePath(Path);
            Length = TimeSpan.FromSeconds(FileProcessor.GetVideoLength(Path));
            TrimEnd = Length;
            (Width, Height) = FileProcessor.GetVideoDimensions(Path);

            CroppedWidth = Width;
            CroppedHeight = Height;
            AspectRatio = Math.Max(Width, Height) / Math.Min(Width, Height);

            NonBlackFrame = FileProcessor.GetNonBlackFrameTime(this);
            var t = FileProcessor.GetBlackPixels(this);
            BlackPixels = new BlackPixels(t.Item1, t.Item2, t.Item3, t.Item4);
        }

        public (double, double) GetDimensions() => (Width, Height);
        
        public override string ToString()
        {
            return $"Filename: {FileName}{Extension}\n" +
                $"Dimensions: {Width}x{Height}\n" +
                $"Orientation: {Orientation}\n" +
                $"Aspect ratio: {AspectRatio}";
        }

        /// <param name="options">User defined options on how the video should be cropped</param>
        /// <returns>The cropped dimensions and the x, y position of the video in the frame</returns>
        public (double, double, double, double) GetCroppedDimensions(CropOptions options)
        {
            if (!options.ShouldCrop) return (Width, Height, 0, 0);
            if (options.IsAuto) return GetAnalyzedCroppedDimensions();

            CroppedWidth = Width;
            CroppedHeight = Height;

            for (var i = 0; i < options.Iterations; i++)
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

        public (double, double, double, double) GetFinalDimensions(CropOptions options)
        {
            return (FinalWidth, FinalHeight, FinalX/*BlackPixels.left*/, FinalY/*BlackPixels.top*/);
        }

        /// <summary>
        /// Uses the stored black pixel values to return the dimensions of the video cropped to remove black bars
        /// </summary>
        /// <returns>The cropped dimensions and the x, y position of the video in the frame</returns>
        public (double, double, double, double) GetAnalyzedCroppedDimensions()
        {
            double width = Width - BlackPixels.left - BlackPixels.right;
            double height = Height - BlackPixels.top - BlackPixels.bottom;
            int x = BlackPixels.left;
            int y = BlackPixels.top;

            Console.WriteLine($"w:{width} h:{height} x:{x} y:{y}");

            return (width, height, x, y);
        }

        /// <returns>A new string with the dimensions clamped to 480p</returns>
        public string GetLQScale(double width, double height)
        {
            //int minResolution = (int)Math.Max(width, height);

            //if (width > height)
            //    return (width < height) ? $"{(width < minResolution ? minResolution : width)}:-1" : $"{480}:-1";

            //return (height < 480) ? $"-1:{height}" : "-1:480";
            int targetHeight = height > 480 ? 480 : (int)height;
            return $"-1:{targetHeight}";
        }

        public void AddRotation(int degrees)
        {
            if (Rotation + degrees >= 480) Rotation = Rotation + degrees - 480;
            else if (Rotation + degrees < 0) Rotation = 480 + Rotation + degrees;
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
