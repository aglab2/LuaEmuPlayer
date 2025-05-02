using Avalonia.Threading;
using LuaEmuPlayer.Models;
using System.ComponentModel;
using System.Threading;

namespace LuaEmuPlayer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly Player _player;

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public MainWindowViewModel()
        {
            _player = new(OnEmuStateChange);
        }

        void OnEmuStateChange(Player.State state)
        {
            string line = "";
            switch (state)
            {
                case Player.State.INITIAL:
                    line = "Searching for emulator...";
                    break;
                case Player.State.ONLY_EMULATOR:
                    line = "Emulator is found, waiting for hack to start";
                    break;
                case Player.State.GAME_ACTIVE:
                    line = "Game is active!";
                    break;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                Name = line;
            });
        }
    }
}
