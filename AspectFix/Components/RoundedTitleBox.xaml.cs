using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace AspectFix.Components
{
    /// <summary>
    /// Interaction logic for RoundedTitleBox.xaml
    /// </summary>
    public partial class RoundedTitleBox : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
                       nameof(Title), typeof(string), typeof(RoundedTitleBox), new PropertyMetadata(string.Empty, OnTitleChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            nameof(MaxLength), typeof(int), typeof(RoundedTitleBox), new PropertyMetadata(35));

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        private string _path;

        public RoundedTitleBox()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RoundedTitleBox control) return;

            string newVal = e.NewValue as string;

            if (File.Exists(newVal))
            {
                control._path = newVal;
                newVal = FileProcessor.ShortenString(Path.GetFileName(newVal), control.MaxLength);
            }
            else
            {
                control._path = null;
                newVal = FileProcessor.ShortenString(newVal, control.MaxLength);
            }
            
            //control.SetValue(TitleProperty, newVal);
            control.TitleBox.Text = newVal;

            control.UpdateClickable();
        }

        private void UpdateClickable()
        {
            Trace.WriteLine(_path);
            if (!File.Exists(_path)) return;

            Cursor = Cursors.Hand;
            ClickBorder.MouseDown += Border_MouseDown;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(_path))
            {
                Process.Start("explorer.exe", $"/select,\"{_path}\"");
            }
        }
    }
}
