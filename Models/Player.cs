using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LuaEmuPlayer.Models.Emulator;
using static LuaEmuPlayer.Models.Form;

namespace LuaEmuPlayer.Models
{
    static class Extensions
    {
        public static void Resume(this LuaThread thr)
        {
            // We leave nothing on the stack if we error
            var oldMainTop = thr.State.MainThread.GetTop();
            var oldCoTop = thr.State.GetTop();

            var ret = thr.State.Resume(null, 0);
            if (ret is KeraLua.LuaStatus.OK or KeraLua.LuaStatus.Yield)
            {
                return;
            }

            var type = thr.State.Type(-1);
            var coErr = thr.State.ToString(-1);
            thr.State.SetTop(oldCoTop);
            thr.State.MainThread.SetTop(oldMainTop);

            throw new LuaScriptException(coErr, string.Empty);
        }
    }

    internal class Player
    {
        public enum State
        {
            LOADING,
            SEARCHING_EMULATOR,
            ONLY_EMULATOR,
            ACTIVE,
        }

        public delegate void EmuStateChangeDelegate(State state);
        readonly EmuStateChangeDelegate _emuStateChange;

        public delegate void ErrorDelegate(string err);
        readonly ErrorDelegate _error;

        public delegate int GetWindowInfo();
        readonly GetWindowInfo _getWindowHeight;
        readonly GetWindowInfo _getWindowWidth;

        public delegate void PresentDelegate(WriteableBitmap bitmap);
        readonly PresentDelegate _present;

        public struct MouseInputs
        {
            public bool left;
            public int x;
            public int y;
        };
        public delegate MouseInputs GetMouseInputs();
        readonly GetMouseInputs _getMouseInputs;

        readonly Emulator _emulator = new();
        readonly GUI _gui = new();

        Lua _lua;
        LuaThread _ironMarioThread;

        // TODO: Probably should be a delegate to VM
        bool buttonL_ = false;
        bool buttonR_ = false;
        bool buttonA_ = false;
        bool buttonB_ = false;

        int _frameCount = 0;

        string _hash = "";

        string GetRomHash()
        {
            return _hash;
        }

        delegate void VoidDelegate();
        void ConsoleClear()
        {
            // -
        }

        int GetFrameCount()
        {
            return _frameCount;
        }

        float EmuReadFloat(uint off, bool be)
        {
            return _emulator.ReadFloat(off);
        }

        ushort EmuReadUShortBE(uint off)
        {
            return _emulator.ReadUShort(off);
        }

        uint EmuReadUIntBE(uint off)
        {
            return _emulator.ReadUInt(off);
        }

        byte EmuReadByte(uint off)
        {
            return _emulator.ReadByte(off);
        }

        void Cancel()
        {
            _ironMarioThread.State.Yield(0);
        }

        Dictionary<string, bool> ReadJoyPad()
        {
            return new Dictionary<string, bool>()
            {
                { "P1 L", buttonL_ },
                { "P1 R", buttonR_ },
                { "P1 A", buttonA_ },
                { "P1 B", buttonB_ },
            };
        }

        int GetWindowWidth()
        {
            return _gui.Height * 4 / 3;
        }

        int GetWindowHeight()
        {
            return _gui.Height;
        }

        int SetGameExtraPadding(int w, int h, int wp, int hp)
        {
            return 0;
        }

        LuaTable GetMouse()
        {
            var inputs = _getMouseInputs();
            var mouse = _lua.GetTable("__mouse__");
            mouse["Left"] = inputs.left;
            mouse["X"] = inputs.x + GetWindowWidth();
            mouse["Y"] = inputs.y;

            return mouse;
        }

        int DrawImage(string path, int x, int y, int width, int height)
        {
            _gui.DrawImage(path, x - GetWindowWidth(), y, width, height);
            return 0;
        }

        int DrawString(int x, int y, string message, string foreColor = null, string backColor = null, int? fontSize = null, string fontFamily = null, string fontStyle = null, string horizAlign = null, string vertAlign = null)
        {
            _gui.DrawString(x - GetWindowWidth(), y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizAlign, vertAlign);
            return 0;
        }

        class ControlsRegistrar
        {
            Dictionary<long, Control> _controls = new();

            public long Register(Control control)
            {
                return Dispatcher.UIThread.Invoke(() =>
                {
                    long id = (long)control.Tag;
                    _controls[id] = control;
                    control.Unloaded += (_, _) => Unregister(id);
                    return id;
                });
            }

            void Unregister(long handle)
            {
                _controls.Remove(handle);
            }

            public Control Get(long handle)
            {
                return Dispatcher.UIThread.Invoke(() =>
                {
                    if (!_controls.TryGetValue(handle, out Control control))
                    {
                        return null;
                    }
                    return control;
                });
            }
        };
        readonly ControlsRegistrar _formControls = new();

        long NewForm(int width, int height, string title, LuaFunction onClose)
        {
            Form form = Dispatcher.UIThread.Invoke(() =>
            {
                Form form = new(height, height, title, () => onClose.Call());
                form.Show();
                return form;
            });

            return _formControls.Register(form);
        }

        bool FormDestroy(long handle)
        {
            var form = _formControls.Get(handle) as Form;
            if (form is null)
            {
                return false;
            }

            Dispatcher.UIThread.InvokeAsync(() => form.Close());
            return true;
        }

        string FormGetProperty(long handle, string name)
        {
            var control = _formControls.Get(handle);
            if (control is null)
            {
                return null;
            }

            switch (name)
            {
                case "Height":
                    return Dispatcher.UIThread.Invoke(() => control.Height).ToString();
                case "Width":
                    return Dispatcher.UIThread.Invoke(() => control.Width).ToString();
                case "SelectedItem":
                    if (control is ComboBox dropdown)
                    {
                        return Dispatcher.UIThread.Invoke(() => dropdown.SelectedItem).ToString();
                    }
                    break;
            }

            return null;
        }

        void FormSetProperty(long handle, string name, object value)
        {
            var control = _formControls.Get(handle);
            if (control is null)
            {
                return;
            }

            switch(name)
            {
                case "Checked":
                    var checkbox = control as CheckBox;
                    if (checkbox is not null)
                    {
                        Dispatcher.UIThread.Invoke(() => checkbox.IsChecked = (bool)value).ToString();
                    }
                    break;
                case "SelectedItem":;
                    if (control is ComboBox dropdown)
                    {
                        Dispatcher.UIThread.Invoke(() => dropdown.SelectedItem = value).ToString();
                    }
                    break;
            }
        }

        long FormLabel(long handle, string caption, int x, int y, int width, int height)
        {
            var form = _formControls.Get(handle) as Form;
            return Dispatcher.UIThread.Invoke(() =>
            {
                return _formControls.Register(form.AddLabel(x, y, width, height, caption));
            });
        }

        long FormDropdown(long handle, LuaTable items, int x, int y, int width, int height)
        {
            var form = _formControls.Get(handle) as Form;
            List<string> dropdownItems = new List<string>();
            int i = 0;
            while (true)
            {
                i++;
                var item = items[i];
                if (item is null)
                {
                    break;
                }

                if (item is string str)
                {
                    dropdownItems.Add(str);
                }
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                return _formControls.Register(form.AddCombobox(x, y, dropdownItems));
            });
        }

        long FormCheckbox(long handle, string caption, int x, int y)
        {
            var form = _formControls.Get(handle) as Form;
            return Dispatcher.UIThread.Invoke(() =>
            {
                return _formControls.Register(form.AddCheckbox(x, y, caption));
            });
        }

        long FormButton(long handle, string caption, LuaFunction onClick, int x, int y, int width, int height)
        {
            var form = _formControls.Get(handle) as Form;
            return Dispatcher.UIThread.Invoke(() =>
            {
                return _formControls.Register(form.AddButton(x, y, width, height, caption, () => onClick.Call()));
            });
        }

        LuaFunction openIronMario()
        {
            var ironMarioScript = File.ReadAllLines("IronMarioTracker.lua");
            foreach (var line in ironMarioScript)
            {
                if (line.Contains("ROM_HASH"))
                {
                    var split = line.Split('"');
                    if (split.Length > 2)
                        _hash = split[1];
                }
            }

            _lua = new Lua();
            _lua.State.Encoding = Encoding.UTF8;
            _lua.NewTable("gameinfo");
            _lua["gameinfo.getromhash"] = new Func<string>(GetRomHash);

            _lua.NewTable("console");
            _lua["console.clear"] = (VoidDelegate) ConsoleClear;

            _lua.NewTable("emu");
            _lua["emu.framecount"] = new Func<int>(GetFrameCount);
            _lua["emu.frameadvance"] = (VoidDelegate)Cancel;

            _lua.NewTable("memory");
            _lua["memory.readfloat"] = new Func<uint, bool, float>(EmuReadFloat);
            _lua["memory.read_u16_be"] = new Func<uint, ushort>(EmuReadUShortBE);
            _lua["memory.read_u32_be"] = new Func<uint, uint>(EmuReadUIntBE);
            _lua["memory.readbyte"] = new Func<uint, byte>(EmuReadByte);

            _lua.NewTable("joypad");
            _lua["joypad.get"] = new Func<Dictionary<string, bool>>(ReadJoyPad);

            _lua.NewTable("client");
            _lua["client.bufferwidth"] = new Func<int>(GetWindowWidth);
            _lua["client.bufferheight"] = new Func<int>(GetWindowHeight);
            _lua["client.SetGameExtraPadding"] = new Func<int, int, int, int, int>(SetGameExtraPadding);

            _lua.NewTable("__mouse__");
            _lua.NewTable("input");
            _lua["input.getmouse"] = new Func<LuaTable>(GetMouse);

            _lua.NewTable("gui");
            _lua["gui.drawImage"] = new Func<string, int, int, int, int, int>(DrawImage);
            _lua["gui.drawString"] = new Func<int, int, string, string, string, int?, string, string, string, string, int>(DrawString);

            _lua.NewTable("forms");
            _lua["forms.newform"] = new Func<int, int, string, LuaFunction, long>(NewForm);
            _lua["forms.destroy"] = new Func<long, bool>(FormDestroy);
            _lua["forms.getproperty"] = new Func<long, string, string>(FormGetProperty);
            _lua["forms.setproperty"] = new Action<long, string, object>(FormSetProperty);
            _lua["forms.label"] = new Func<long, string, int, int, int, int, long>(FormLabel);
            _lua["forms.dropdown"] = new Func<long, LuaTable, int, int, int, int, long>(FormDropdown);
            _lua["forms.checkbox"] = new Func<long, string, int, int, long>(FormCheckbox);
            _lua["forms.button"] = new Func<long, string, LuaFunction, int, int, int, int, long>(FormButton);

            return _lua.LoadFile("IronMarioTracker.lua");
        }

        public Player(EmuStateChangeDelegate emuStateChange, ErrorDelegate error, GetWindowInfo width, GetWindowInfo height, GetMouseInputs getMouseInputs, PresentDelegate present)
        {
            _emuStateChange = emuStateChange;
            _error = error;
            _getWindowHeight = height;
            _getWindowWidth = width;
            _getMouseInputs = getMouseInputs;
            _present = present;

            Task.Run(Scan);
        }

        async Task Scan()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            _emuStateChange(State.LOADING);
            LuaFunction ironMario;
            try
            {
                ironMario = openIronMario();
            }
            catch (Exception e)
            {
                _error(e.Message);
                return;
            }
            _lua.NewThread(ironMario, out _ironMarioThread);

            _emuStateChange(State.SEARCHING_EMULATOR);
            PrepareResult prevResult = PrepareResult.NOT_FOUND;

            int delayMs = 1000;
            while (true)
            {
                _frameCount++;
                var residue = _gui.CheckHeight(_getWindowHeight(), _getWindowWidth());
                try
                {
                    delayMs = 1000;
                    PrepareResult newResult = _emulator.Prepare();
                    if (newResult != prevResult)
                    {
                        prevResult = newResult;
                        switch (_emulator.Prepare())
                        {
                            case PrepareResult.NOT_FOUND:
                                _emuStateChange(State.SEARCHING_EMULATOR);
                                break;
                            case PrepareResult.ONLY_EMULATOR:
                                _emuStateChange(State.ONLY_EMULATOR);
                                break;
                            case PrepareResult.OK:
                                _emuStateChange(State.ACTIVE);
                                break;
                        }
                    }

                    if (newResult != PrepareResult.OK)
                        continue;

                    delayMs = 60;
                    _ironMarioThread.Resume();
                }
                catch (LuaException luaEx)
                {
                    _ironMarioThread.Dispose();
                    _lua.NewThread(ironMario, out _ironMarioThread);
                    _error(luaEx.Message);
                }
                catch (Exception e)
                {
                    _error(e.Message);
                }
                finally
                {
                    var present = _gui.SwapBuffers();
                    if (present is not null || residue is not null)
                    {
                        _present(present);
                    }
                    if (residue is not null)
                    {
                        residue.Dispose();
                    }
                    await Task.Delay(delayMs);
                }
            }
        }
    }
}
