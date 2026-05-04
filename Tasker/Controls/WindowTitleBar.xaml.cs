using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace TaskManager.Controls
{
    public partial class WindowTitleBar : UserControl
    {
        public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(
            nameof(TitleText),
            typeof(string),
            typeof(WindowTitleBar),
            new PropertyMetadata("Tasker"));

        public string TitleText
        {
            get => (string)GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        public WindowTitleBar()
        {
            InitializeComponent();
        }

        private Window? Host => Window.GetWindow(this);

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            if (Host == null)
            {
                return;
            }

            Host.StateChanged += (_, _) => SyncMaxIcon();
            SyncMaxIcon();
        }

        private void SyncMaxIcon()
        {
            if (Host == null || MaximizeIcon == null || MaxBtn == null)
            {
                return;
            }

            MaximizeIcon.Kind = Host.WindowState == WindowState.Maximized
                ? PackIconKind.WindowRestore
                : PackIconKind.WindowMaximize;
            MaxBtn.ToolTip = Host.WindowState == WindowState.Maximized ? "Окно" : "Развернуть";
        }

        private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var w = Host;
            if (w == null)
            {
                return;
            }

            if (e.ClickCount == 2 && w.ResizeMode == ResizeMode.CanResize)
            {
                w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                e.Handled = true;
                return;
            }

            try
            {
                w.DragMove();
            }
            catch
            {
                // ignore
            }
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            if (Host != null)
            {
                Host.WindowState = WindowState.Minimized;
            }
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
            {
                return;
            }

            Host.WindowState = Host.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Host?.Close();
    }
}
