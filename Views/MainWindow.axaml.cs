using Avalonia.Controls;
using Avalonia.Input;
using LuaEmuPlayer.ViewModels;

namespace LuaEmuPlayer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PointerPressedHandler(object sender, PointerPressedEventArgs args)
        {
            var point = args.GetCurrentPoint(sender as Control);
            var x = point.Position.X;
            var y = point.Position.Y;
            var left = point.Properties.IsLeftButtonPressed;
            var right = point.Properties.IsRightButtonPressed;
            (DataContext as MainWindowViewModel).PointerHandler(x, y, left, right);
        }

        private void PointerReleasedHandler(object sender, PointerReleasedEventArgs args)
        {
            var point = args.GetCurrentPoint(sender as Control);
            var x = point.Position.X;
            var y = point.Position.Y;
            var left = point.Properties.IsLeftButtonPressed;
            var right = point.Properties.IsRightButtonPressed;
            (DataContext as MainWindowViewModel).PointerHandler(x, y, left, right);
        }
    }
}