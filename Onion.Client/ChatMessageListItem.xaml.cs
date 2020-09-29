using Onion.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for ChatMessageListItem.xaml
    /// </summary>
    public partial class ChatMessageListItem : UserControl
    {
        private string from;
        private bool You;

        public ChatMessageListItem(string Message, string from, bool You)
        {
            InitializeComponent();
            this.from = from;
            this.You = You;
            FormattedText Formattedtext = new FormattedText(Message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("#Lato MediumItalic"), 17, You ? Brushes.White : Brushes.DodgerBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            CBorder.Background = You ? Brushes.DodgerBlue : Brushes.White;
            CPath.Fill = You ? Brushes.DodgerBlue : Brushes.White;
            CBorder.Width = Formattedtext.Width + 30;
            TextBox.Text = Message;
            TextBox.Foreground = You ? Brushes.White : Brushes.DodgerBlue;
            Thickness old = Status.Margin;
            old.Left = CBorder.Margin.Left + CBorder.Width + 5;
            Status.Margin = old;
        }
        internal void ErrorMessage()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Status.Cursor = Cursors.Hand;
                BitmapImage Image = new BitmapImage();
                Image.BeginInit();
                Image.UriSource = new Uri("Image/Error.png", UriKind.Relative);
                Image.EndInit();
                Status.Source = Image;
            }));
        }
    }
}
