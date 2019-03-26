using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace KH_NET_UI
{
    /// <summary>
    /// Interaction logic for ProcessSelect_ProcessItem.xaml
    /// </summary>
    public partial class ProcessSelect_ProcessItem : UserControl
    {
        public ProcessSelect_ProcessItem()
        {
            InitializeComponent();
        }
        public int ProcessID { get; private set; }
        public string ProcessName { get; private set; }
        public void SetContent(Icon icon, string name, string hex, int id)
        {
            if (icon.Size != new System.Drawing.Size(0,0)) { Process_Image.Source = icon.ToImageSource(); }
            ProcessName_lbl.Content = name;
            ProcessID_Hex_lbl.Content = hex;
            ProcessID_Int_lbl.Content = id.ToString();
            ProcessID = id;
            ProcessName = name;
        }

    }
    public static class IconExtensions
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);


        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new System.ComponentModel.Win32Exception();
            }

            return wpfBitmap;
        }

    }
}
