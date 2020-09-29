using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        private bool exitOnClose;

        private event EventHandler<PostMessageEventArgs> Apply;

        public MessageWindow(string BaseMessage, bool exitOnClose)
        {
            this.exitOnClose = exitOnClose;
            InitializeComponent();
            MessageText.Text = BaseMessage;
            Apply += new EventHandler<PostMessageEventArgs>((sender, e) =>            
                MessageText.Dispatcher.Invoke(new Action(() => MessageText.Text = e.Message)));
        }
        public void PostMessage(string Message) =>
            Apply?.Invoke(this, new PostMessageEventArgs() { Message = Message });

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closed(object sender, EventArgs e)
        {
            if (exitOnClose)
                Application.Current.Shutdown();
        }
    }
    class PostMessageEventArgs
    {
        public string Message { get; set; }
    }
}
