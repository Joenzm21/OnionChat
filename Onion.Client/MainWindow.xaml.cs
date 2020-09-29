using Microsoft.Win32;
using Onion.Client;
using Onion.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal OnionManager manager;
        public Chat chatPage;

        public MainWindow()
        {
            if (!File.Exists("config.ini"))
            {
                MessageBox.Show("Config file is missing", "Config Isn't Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            manager = new OnionManager("config.ini");
            manager.OnReceived += Manager_OnReceived; manager.OnShutdown += Manager_OnShutdown; manager.OnRebuilded += Manager_OnRebuilded;
            manager.OnRemovedUser += Manager_OnRemovedUser;
            manager.OnSync += Manager_OnSync;
            InitializeComponent();
        }

        private void Manager_OnSync(object sender, SyncEventArgs e)
        {
            try
            {
                PeersCount.Dispatcher.Invoke(new Action(() =>
                    PeersCount.Text = "Peers: " + e.PeersCount));
                UsersCount.Dispatcher.Invoke(new Action(() =>
                LinksCount.Text = "Links: " + e.LinksCount));
                UsersCount.Dispatcher.Invoke(new Action(() =>
                UsersCount.Text = "Users: " + e.UsersCount));
            }
            catch { }
        }

        private void Manager_OnRemovedUser(object sender, RemovedUserEventArgs e)
        {
            chatPage.Dispatcher.Invoke(new Action(() =>
            {
                foreach (ChatUserListItem item in chatPage.Users.Items)
                    if (e.Users.Contains(item.User.Text))
                    {
                        item.IsDisabled = true;
                        chatPage.CheckDisableUser(item);
                    }
            }));
        }

        private void Manager_OnRebuilded(object sender, EventArgs e)
        {

        }

        private void Manager_OnShutdown(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            new MessageWindow("Onion Network is shutdown !!", true).ShowDialog()));
        }

        private void Manager_OnReceived(object sender, ReceivedEventArgs e) => chatPage.PostMessage(e);

        internal void Login(string nickName)
        {
            switch (manager.Register(nickName))
            {
                case StatusCode.Ok:
                    IDLabel.Content = "ID: " + manager.ID;
                    GFocus.Focusable = false;
                    MFrame.Navigate(chatPage = new Chat(manager));
                    break;
                case StatusCode.Closed:
                    new MessageWindow("Server is closed", true).ShowDialog();
                    break;
                case StatusCode.Loop:
                    new MessageWindow("Config file is wrong", true).ShowDialog();
                    break;
                case StatusCode.Error:
                    new MessageWindow("This nickname is full", false).ShowDialog();
                    break;
            }
        }

        private void Drag(object sender, MouseEventArgs e) => DragMove();
        private void CloseWindow(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) => GFocus.Focus();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            manager.Dispose();
        }

        private void IDLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(manager.ID);
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
