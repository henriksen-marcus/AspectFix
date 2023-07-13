﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectFix
{
    public enum Orientation
    {
        Portrait,
        Landscape
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
        public int CroppedWidth { get; set; }
        public int CroppedHeight { get; set; }
        public float AspectRatio { get; private set; }
        public float CroppedAspectRatio
        {
            get => (float)Math.Max(CroppedWidth, CroppedHeight) / (float)Math.Min(CroppedWidth, CroppedHeight);
        }

        public VideoFile(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileNameWithoutExtension(path);
            Extension = System.IO.Path.GetExtension(path);
            (Width, Height) = FileProcessor.GetVideoDimensions(path);
            CroppedWidth = Width;
            CroppedHeight = Height;
            AspectRatio = (float)Math.Max(Width, Height) / (float)Math.Min(Width, Height);
        }

        public (int, int) GetDimensions() => (Width, Height);
        
        public override string ToString()
        {
            return $"Filename: {FileName}{Extension}\n" +
                $"Dimensions: {Width}x{Height}\n" +
                $"Orientation: {Orientation}\n" +
                $"Aspect ratio: {AspectRatio}";
        }

        public (int, int) GetCroppedDimensions(int iterations)
        {
            CroppedWidth = Width;
            CroppedHeight = Height;

            for (int i = 0; i < iterations; i++)
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

            return (CroppedWidth, CroppedHeight);
        }
    }
}
