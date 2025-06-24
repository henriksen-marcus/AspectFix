using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                newVal = Services.Utils.ShortenString(Path.GetFileName(newVal), control.MaxLength);
            }
            else
            {
                control._path = null;
                newVal = Services.Utils.ShortenString(newVal, control.MaxLength);
            }
            
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
