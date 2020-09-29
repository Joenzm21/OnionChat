using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// Interaction logic for ChatMessageListControl.xaml
    /// </summary>
    public partial class ChatMessageListControl : Page
    {
        private readonly object Lockobj = new object();

        public ChatMessageListControl(string Username)
        {
            InitializeComponent();
            this.Username.Text = Username;
            FormattedText Formattedtext = new FormattedText(Username, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("#Lato MediumItalic"), 20, Brushes.DodgerBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            Thickness old = Status.Margin;
            old.Left = this.Username.Margin.Left + this.Username.Width / 2 + Formattedtext.Width / 2 + 10;
            Status.Margin = old;
        }
        public ChatMessageListItem AddMessage(string MessageText, bool You)
        {
            string Message = MessageText;
            lock (Lockobj)
            {
                if (You)
                {
                    int i,j;
                    for (i = 0; i < Message.Length; i++)
                        if (char.IsLetterOrDigit(Message[i]))
                        {
                            i--;
                            break;
                        }
                    for (j = Message.Length - 1; j > 0; j--)
                        if (char.IsLetterOrDigit(Message[j]))
                        {
                            j++;
                            break;
                        }
                    char[] Left = Array.Empty<char>();
                    if (i >= 0 && i < Message.Length) Left = Message.Substring(0, i + 1).ToCharArray();
                    string Mid = i < j ? Message.Substring(i + 1, j - i - 1) : "";
                    char[] Right = Array.Empty<char>();
                    if (i < j || j < Message.Length) Right = Message.Substring(j, Message.Length - j).ToCharArray();
                    for (int k = 0; k < Left.Length; k++)
                        switch (Left[k])
                        {
                            case '(':
                                Left[k] = ')';
                                break;
                            case ')':
                                Left[k] = '(';
                                break;
                            case '{':
                                Left[k] = '}';
                                break;
                            case '}':
                                Left[k] = '{';
                                break;
                            case '<':
                                Left[k] = '>';
                                break;
                            case '>':
                                Left[k] = '<';
                                break;
                        }
                    Array.Reverse(Left);
                    for (int k = 0; k < Right.Length; k++)
                        switch (Right[k])
                        {
                            case '(':
                                Right[k] = ')';
                                break;
                            case ')':
                                Right[k] = '(';
                                break;
                            case '{':
                                Right[k] = '}';
                                break;
                            case '}':
                                Right[k] = '{';
                                break;
                            case '<':
                                Right[k] = '>';
                                break;
                            case '>':
                                Right[k] = '<';
                                break;
                        }
                    Array.Reverse(Right);
                    Message = string.Join("", Right) + Mid + string.Join("", Left);
                 }
                ScrollLC.HorizontalContentAlignment = You ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                ChatMessageListItem chatMessageListItem = ListMessage.Items[ListMessage.Items.Add(new ChatMessageListItem(Message, Username.Text, You) { FlowDirection = You ? FlowDirection.RightToLeft : FlowDirection.LeftToRight })] as ChatMessageListItem;
                ScrollLC.ScrollToEnd();
                return chatMessageListItem;
            }
        }
    }
}
