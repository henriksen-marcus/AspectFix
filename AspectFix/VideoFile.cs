using System;
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
        public Orientation Orientation { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public VideoFile(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileNameWithoutExtension(path);
            Extension = System.IO.Path.GetExtension(path);
            (Width, Height) = FileProcessor.GetVideoDimensions(path);
            Orientation = GetOrientation();
        }

        public Orientation GetOrientation()
        {
            if (Width > Height) return Orientation.Landscape;
            else return Orientation.Portrait;
        }

        public (int, int) GetDimensions() => (Width, Height);
        
    }
}
