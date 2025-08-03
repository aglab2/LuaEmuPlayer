using LuaEmuPlayer.ViewModels;
using NLua;
using NLua.Exceptions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LuaEmuPlayer.Models.Emulator;

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
            if (ret == KeraLua.LuaStatus.OK || ret == KeraLua.LuaStatus.Yield)
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

        public delegate void PresentDelegate(Frame frame);
        readonly PresentDelegate _present;

        readonly object _uiDispatcher;

        public struct MouseInputs
        {
            public bool left;
            public int x;
            public int y;
        };
        public delegate MouseInputs GetMouseInputs();
        readonly GetMouseInputs _getMouseInputs;

        readonly Emulator _emulator = new Emulator();
        readonly GUI _gui = new GUI();
        readonly Forms _forms = new Forms();

        SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
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

        void DrawImage(string path, int x, int y, int width, int height)
        {
            _gui.DrawImage(path, x - GetWindowWidth(), y, width, height);
        }

        void DrawString(int x, int y, string message, string foreColor = null, string backColor = null, int? fontSize = null, string fontFamily = null, string fontStyle = null, string horizAlign = null, string vertAlign = null)
        {
            _gui.DrawString(x - GetWindowWidth(), y, message, foreColor, backColor, fontSize, fontFamily, fontStyle, horizAlign, vertAlign);
        }

        void DrawBox(int x, int y, int x2, int y2, string line = null, string background = null)
        {
            _gui.DrawBox(x - GetWindowWidth(), y, x2 - GetWindowWidth(), y2, line, background);
        }

        long NewForm(int width, int height, string title, LuaFunction onClose)
        {
            return _forms.Form(width, height, title, async () => await RunUIBlock(onClose), _uiDispatcher);
        }

        bool FormDestroy(long? handle)
        {
            return _forms.Destroy(handle);
        }

        string FormGetProperty(long handle, string name)
        {
            return _forms.GetProperty(handle, name);
        }

        void FormSetProperty(long handle, string name, object value)
        {
            _forms.SetProperty(handle, name, value);
        }

        long FormLabel(long handle, string caption, int x, int y, int width, int height)
        {
            return _forms.Label(handle, caption, x, y, width, height);
        }

        long FormDropdown(long handle, LuaTable items, int x, int y, int width, int height)
        {
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

            return _forms.Dropdown(handle, dropdownItems, x, y, width, height);
        }

        long FormCheckbox(long handle, string caption, int x, int y)
        {
            return _forms.Checkbox(handle, caption, x, y);
        }

        long FormButton(long handle, string caption, LuaFunction onClick, int x, int y, int width, int height)
        {
            return _forms.Button(handle, caption, async () => await RunUIBlock(onClick), x, y, width, height);
        }

        bool FormIsChecked(long handle)
        {
            return _forms.IsChecked(handle);
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
            _lua["memory.read_u8"] = new Func<uint, byte>(EmuReadByte);

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
            _lua["gui.drawImage"] = new Action<string, int, int, int, int>(DrawImage);
            _lua["gui.drawString"] = new Action<int, int, string, string, string, int?, string, string, string, string>(DrawString);
            _lua["gui.drawBox"] = new Action<int, int, int, int, string, string> (DrawBox);

            _lua.NewTable("forms");
            _lua["forms.newform"] = new Func<int, int, string, LuaFunction, long>(NewForm);
            _lua["forms.destroy"] = new Func<long?, bool>(FormDestroy);
            _lua["forms.getproperty"] = new Func<long, string, string>(FormGetProperty);
            _lua["forms.setproperty"] = new Action<long, string, object>(FormSetProperty);
            _lua["forms.label"] = new Func<long, string, int, int, int, int, long>(FormLabel);
            _lua["forms.dropdown"] = new Func<long, LuaTable, int, int, int, int, long>(FormDropdown);
            _lua["forms.checkbox"] = new Func<long, string, int, int, long>(FormCheckbox);
            _lua["forms.button"] = new Func<long, string, LuaFunction, int, int, int, int, long>(FormButton);
            _lua["forms.ischecked"] = new Func<long, bool>(FormIsChecked);

            return _lua.LoadFile("IronMarioTracker.lua");
        }

        public Player(EmuStateChangeDelegate emuStateChange, ErrorDelegate error, GetWindowInfo width, GetWindowInfo height, GetMouseInputs getMouseInputs, PresentDelegate present, object uiDispatcher)
        {
            _emuStateChange = emuStateChange;
            _error = error;
            _getWindowHeight = height;
            _getWindowWidth = width;
            _getMouseInputs = getMouseInputs;
            _present = present;
            _uiDispatcher = uiDispatcher;

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
                await _semaphore.WaitAsync();
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
                    _semaphore.Release();
                    var present = _gui.SwapBuffers();
                    if (!(present is null) || !(residue is null))
                    {
                        _present(present);
                    }
                    if (!(residue is null))
                    {
                        residue.Dispose();
                    }
                    await Task.Delay(delayMs);
                }
            }
        }

        async Task RunUIBlock(LuaFunction func)
        {
            await _semaphore.WaitAsync();
            try
            {
                _lua.NewThread(func, out var thread);
                thread.Resume();
                thread.Dispose();
            }
            catch (LuaScriptException e)
            {
                _error(e.Message);
            }
            catch (Exception e)
            {
                _error(e.Message);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
