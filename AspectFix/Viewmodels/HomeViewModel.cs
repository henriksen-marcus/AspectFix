using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectFix
{
    public class HomeViewModel : BaseViewModel
    {
        public ObservableCollection<string> RecentFiles { get; set; } = new();

        public void AddFile(string path)
        {
            if (RecentFiles.Count == 0) RecentFiles.Add("Recent files");
            RecentFiles.Add(path);
        }
    }
}
