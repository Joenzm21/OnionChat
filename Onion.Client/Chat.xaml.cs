using Microsoft.Win32;
using Onion.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for Chat.xaml
    /// </summary>
    public partial class Chat : Page
    {
        private int LastCount = 0;
        private System.Timers.Timer searchBoxTimer = new System.Timers.Timer();
        private bool Found = false;
        private bool IsIdle = false;
        private DispatcherTimer timer;
        private OnionManager onionManager;
        private OpenFileDialog openFileDialog;

        public Chat(OnionManager onionManager)
        {
            this.onionManager = onionManager;
            InitializeComponent();
            this.openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = false;
            searchBoxTimer.Elapsed += SearchBoxTimer_Elapsed;
            searchBoxTimer.Interval = 800;
            timer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.ApplicationIdle,(s, e) => { IsIdle = true; }, Application.Current.Dispatcher);
            timer.Start();
        }
        private void Active()
        {
            if ((ChatUserListItem)Users.SelectedItem != null)
                ((ChatUserListItem)Users.SelectedItem).Status.Background = System.Windows.Media.Brushes.Transparent;
            if (!timer.IsEnabled)
                timer.Start();
            IsIdle = false;
        }

        private void SearchBoxTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            searchBoxTimer.Stop();
            string name = "";
            SearchBox.Dispatcher.Invoke(new Action(() => name = SearchBox.Text));
            Search(name);
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Active();
            Hint.Visibility = Visibility.Collapsed;
        }
        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MessageTextBox.Text)) Hint.Visibility = Visibility.Visible;
        }
        internal void PostMessage(ReceivedEventArgs e)
        {
            ChatUserListItem baseitem = null;
            Dispatcher.Invoke(new Action(() =>
            {
                for(int i = 0; i < Users.Items.Count; i++)
                {
                    if (((ChatUserListItem)Users.Items[i]).User.Text == e.From)
                    {
                        baseitem = (ChatUserListItem)Users.Items[i];
                        break;
                    }
                }
                if (baseitem == null)
                {
                    baseitem = Users.Items[Users.Items.Add(new ChatUserListItem(e.From))] as ChatUserListItem;
                    UpdateListUsers();
                    baseitem.Status.Background = System.Windows.Media.Brushes.DodgerBlue;
                }
                if (IsIdle)
                    baseitem.Status.Background = System.Windows.Media.Brushes.DodgerBlue;
                baseitem.ListControl.AddMessage(e.Data as string, false);
                baseitem.UpdateLastMessage(e.Data as string, false);
            }));
        }
        
        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Active();
            if (MessageTextBox.Text.Length == 0)
            {
                BitmapImage Image = new BitmapImage();
                Image.BeginInit();
                Image.UriSource = new Uri("Image/Disablesend.png", UriKind.Relative);
                Image.EndInit();
                SendIcon.Source = Image;
                SendButton.IsEnabled = false;
            }
            else if (MessageTextBox.Text.Length > 0 && LastCount == 0 && Users.SelectedItem != null &&
                !(((ChatUserListItem)Users.SelectedItem).User.Text == ((ChatMessageListControl)CFrame.Content).Username.Text &&
                    ((ChatUserListItem)Users.SelectedItem).IsDisabled))
            {
                BitmapImage Image = new BitmapImage();
                Image.BeginInit();
                Image.UriSource = new Uri("Image/Send.png", UriKind.Relative);
                Image.EndInit();
                SendIcon.Source = Image;
                SendButton.IsEnabled = true;
            }
            LastCount = MessageTextBox.Text.Length;
        }

        private void Chat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SendButton.IsEnabled)
                SendButton_Click(null, null);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Active();
            string Text = MessageTextBox.Text;
            string User = ((ChatUserListItem)Users.SelectedItem).User.Text;
            MessageTextBox.Clear();
            MessageTextBox.Focus();
            ChatMessageListItem item = ((ChatUserListItem)Users.SelectedItem).ListControl.AddMessage(Text, true);
            ((ChatUserListItem)Users.SelectedItem).UpdateLastMessage(Text, true);
            ((ChatUserListItem)Users.SelectedItem).Status.Background = System.Windows.Media.Brushes.Transparent;
            new Thread(async () => await onionManager.SendMessage(User, Text).ContinueWith(task =>
            {
                if (!task.Result)
                    item.ErrorMessage();
            })).Start();
        }
        public void CheckDisableUser(ChatUserListItem item)
        {
            if (Users.SelectedItem == item && item.IsDisabled)
            {
                SendButton.IsEnabled = false;
                MessageTextBox.Clear();
                BitmapImage Image = new BitmapImage();
                Image.BeginInit();
                Image.UriSource = new Uri("Image/Error.png", UriKind.Relative);
                Image.EndInit();
                ((ChatMessageListControl)CFrame.Content).Status.Source = Image;           
            }
            else
            {
                SendButton.IsEnabled = true;
            }
        }

        private void Users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Active();
            if ((ChatUserListItem)Users.SelectedItem == null) return;
            CFrame.Navigate(((ChatUserListItem)Users.SelectedItem).ListControl);
            ((ChatUserListItem)Users.SelectedItem).Status.Background = System.Windows.Media.Brushes.Transparent;
            CloseUser.Visibility = Visibility.Visible;
            MessageTextBox.Clear();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Active();
            HintS.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(null, null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Active();
            if (Found)
            {
                Found = false;
                return;
            }
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchListBox.Height = 0;
                SearchListBox.Items.Clear();
                HintS.Visibility = Visibility.Visible;
                return;
            }
            else
                HintS.Visibility = Visibility.Collapsed; 
            if (!searchBoxTimer.Enabled)
                searchBoxTimer.Start();
            else
            {
                searchBoxTimer.Stop();
                searchBoxTimer.Start();
            }
        }
        private void Search(string name)
        {
            string[] foundUser = Array.Empty<string>();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foundUser = onionManager.UserList.FindAll(c => c.ID.StartsWith(name)).Where(c => Users.Items.Cast<ChatUserListItem>().ToList().FindIndex(i => i.User.Text == c.ID) == -1).Select(k => k.ID).ToArray();
                if (foundUser.Length > 0)
                    SearchListBox.Dispatcher.Invoke(new Action(() =>
                    {
                        foreach (string s in foundUser)
                            SearchListBox.Items.Add(s);
                        SearchListBox.Height = Math.Min(100, 4 + 23 * foundUser.Length);
                    }));
                else 
                {
                    SearchListBox.Height = 0;
                    SearchListBox.Items.Clear();
                }
            }));
        }
        private void UpdateListUsers()
        {
            if (Users.SelectedItem == null && Users.Items.Count > 0)
                Users.SelectedIndex = 0;
            if (Users.Items.Count == 0)
            {
                CFrame.Content = null;
                CFrame.NavigationService.RemoveBackEntry();
                MessageTextBox.Clear();
            }
        }

        private void SearchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Active();
            if (string.IsNullOrWhiteSpace(SearchListBox.SelectedItem as string)) return;
            string name = SearchListBox.SelectedItem as string;
            Found = true;
            HintS.Visibility = Visibility.Visible;
            SearchBox.Clear();
            SearchListBox.Height = 0;
            SearchListBox.Items.Clear();
            if (Users.Items.Cast<ChatUserListItem>().ToList().FindIndex(c => c.User.Text == name) != -1) return;
            Users.SelectedIndex = Users.Items.Add(new ChatUserListItem(name));
            UpdateListUsers();
        }

        private void SearchListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Found = true;
        }

        private void CloseUser_Click(object sender, RoutedEventArgs e)
        {
            CFrame.Content = null;
            CFrame.NavigationService.RemoveBackEntry();
            Users.Items.Remove(Users.SelectedItem);
            MessageTextBox.Clear();
            CloseUser.Visibility = Visibility.Collapsed;
        }
    }
}
