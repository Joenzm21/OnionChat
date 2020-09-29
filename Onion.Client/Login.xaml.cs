using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Onion.Client
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
            this.Nickname.LostFocus += (sender, e) => Caret.Visibility = Visibility.Collapsed;
            this.Nickname.GotFocus += (sender, e) => Caret.Visibility = Visibility.Visible;
            this.Nickname.SelectionChanged += (sender, e) => MoveCaret();
        }

        private void Nickname_GotFocus(object sender, RoutedEventArgs e)
        {
            HintUN.Visibility = Visibility.Collapsed;
        }

        private void Nickname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Nickname.Text)) HintUN.Visibility = Visibility.Visible;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) => GFocus.Focus();

        private void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Nickname.Text) || new Regex(@"[^a-zA-Z0-9\s]").IsMatch(Nickname.Text))
            {
                new MessageWindow("Invaild Nickname", false).ShowDialog();
                return;
            }
            ((MainWindow)Application.Current.MainWindow).Login(Nickname.Text);
        }

        private void MoveCaret()
        {
            var caretLocation = Nickname.GetRectFromCharacterIndex(Nickname.CaretIndex).Location;

            if (!double.IsInfinity(caretLocation.X))
            {
                Canvas.SetLeft(Caret, caretLocation.X);
            }

            if (!double.IsInfinity(caretLocation.Y))
            {
                Canvas.SetTop(Caret, caretLocation.Y);
            }
        }

        private void Nickname_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                EnterButton_Click(null, null);
        }
    }
}
