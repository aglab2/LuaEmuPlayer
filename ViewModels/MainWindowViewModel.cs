using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LuaEmuPlayer.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuaEmuPlayer.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly Player _player;

        private string _name = "Initializing...";
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private int _width = 300;
        public int Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }


        private int _height = 700;
        public int Height
        {
            get { return _height; }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        private WriteableBitmap _render;
        public WriteableBitmap Render
        {
            get { return _render; }
            set
            {
                if (_render != value)
                {
                    _render = value;
                    OnPropertyChanged(nameof(Render));
                }
            }
        }

        struct Mouse
        {
            public int x, y;
            public bool left;
            public bool right;
        };
        Mouse _mouse = new();

        public void PointerHandler(double x, double y, bool left, bool right)
        {
            _mouse.x = (int)x;
            _mouse.x = (int)y;
            _mouse.left = left;
            _mouse.right = right;
        }

        public MainWindowViewModel()
        {
            _player = new(OnEmuStateChange, OnError, OnGetWindowWidth, OnGetWindowHeight, OnGetMouseInputs, OnPresent);
        }

        void OnEmuStateChange(Player.State state)
        {
            string line = "";
            switch (state)
            {
                case Player.State.LOADING:
                    line = "Loading Lua scripts...";
                    break;
                case Player.State.SEARCHING_EMULATOR:
                    line = "Searching for emulator...";
                    break;
                case Player.State.ONLY_EMULATOR:
                    line = "Emulator is found, waiting for hack to start...";
                    break;
                case Player.State.ACTIVE:
                    line = "";
                    break;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                Name = line;
            });
        }

        void OnError(string err)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Name = err;
            });
        }

        int OnGetWindowHeight()
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                return _height;
            });
        }

        int OnGetWindowWidth()
        {
            return Dispatcher.UIThread.Invoke(() =>
            {
                return _width;
            });
        }

        Player.MouseInputs OnGetMouseInputs()
        {
            return new Player.MouseInputs() { left = _mouse.left, x = _mouse.x, y = _mouse.y };
        }

        void OnPresent(WriteableBitmap bitmap)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Render = bitmap;
            });
        }
    }
}
