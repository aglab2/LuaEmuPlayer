using LuaEmuPlayer.Models;
using LuaEmuPlayer.ViewModels;
using System;
using System.Windows.Forms;
using static LuaEmuPlayer.Models.Player;

namespace LuaEmuPlayerGDI
{
    public partial class LuaEmuPlayerForm : Form
    {
        Player _player;

        public LuaEmuPlayerForm()
        {
            InitializeComponent();
        }

        private void LuaEmuPlayerForm_Load(object sender, EventArgs e)
        {
            _player = new Player(OnEmuStateChange, OnError, OnWidth, OnHeight, OnGetMouseInputs, OnPresent, this);
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private T SafeInvoke<T>(Func<T> func)
        {
            if (InvokeRequired)
            {
                return (T)Invoke(func);
            }
            else
            {
                return func();
            }
        }

        private void OnPresent(Frame frame)
        {
            SafeInvoke(() => {
                if (frame is null)
                {
                    framePictureBox.Image = null;
                }
                else
                {
                    framePictureBox.Image = frame.Bitmap;
                }
            });
        }

        private MouseInputs OnGetMouseInputs()
        {
            return SafeInvoke(() =>
            {
                var position = framePictureBox.PointToClient(Cursor.Position);
                return new MouseInputs
                {
                    x = position.X,
                    y = position.Y,
                    left = MouseButtons.HasFlag(MouseButtons.Left),
                };
            });
        }

        private int OnHeight()
        {
            return SafeInvoke(() => framePictureBox.Height);
        }

        private int OnWidth()
        {
            return SafeInvoke(() => framePictureBox.Width);
        }

        private void OnError(string err)
        {
            SafeInvoke(() => statusLabel.Text = err);
        }

        private void OnEmuStateChange(State state)
        {
            SafeInvoke(() =>
            {
                switch (state)
                {
                    case State.LOADING:
                        statusLabel.Text = "Loading...";
                        break;
                    case State.SEARCHING_EMULATOR:
                        statusLabel.Text = "Searching for the emulator";
                        break;
                    case State.ONLY_EMULATOR:
                        statusLabel.Text = "Emulator is running, load the ROM";
                        break;
                    case State.ACTIVE:
                        statusLabel.Text = "";
                        break;
                }
            });
        }
    }
}
