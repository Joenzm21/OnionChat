using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for ChatUserListItem.xaml
    /// </summary>
    public partial class ChatUserListItem : UserControl
    {
        public bool IsDisabled = false;

        public ChatMessageListControl ListControl { get; set; }
        public ChatUserListItem(string Username)
        {
            InitializeComponent();
            User.Text = Username;
            ListControl = new ChatMessageListControl(Username);
        }
        public void UpdateLastMessage(string Message, bool You)
        {
            int Length = Message.Length - 1;
            FormattedText Formattedtext = new FormattedText(Message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("#Lato MediumItalic"), 10, Brushes.DodgerBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            if (Formattedtext.Width > 160)
                while (true)
                {
                    string Result = Message.Substring(0, Length);
                    Formattedtext = new FormattedText(Result, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        new Typeface("#Lato MediumItalic"), 10, Brushes.DodgerBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    if (Formattedtext.Width < 160)
                    {
                        Message = Result + "...";
                        break;
                    }
                    Length--;
                }
            if (You) Message = "You: " + Message;
            LastMessage.Text = Message + " | " + DateTime.Now.ToString("hh:mm"); 
        }
    }
}
