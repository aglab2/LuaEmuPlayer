using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Generic;

namespace LuaEmuPlayer.Models
{
    internal class Forms
    {
        Dictionary<long, Control> _controls = new();
        public delegate void Callback();

        long Register(Control control)
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

        Control Get(long handle)
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

        public long Form(int width, int height, string title, Callback onClose, object _)
        {
            Form form = Dispatcher.UIThread.Invoke(() =>
            {
                Form form = new(width, height, title, () => onClose());
                form.Show();
                return form;
            });

            return Register(form);
        }

        public bool Destroy(long? handle)
        {
            if (handle is null)
            {
                return false;
            }

            var form = Get(handle.Value) as Form;
            if (form is null)
            {
                return false;
            }

            Dispatcher.UIThread.Invoke(() => form.Close());
            return true;
        }

        public string GetProperty(long handle, string name)
        {
            var control = Get(handle);
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

        public void SetProperty(long handle, string name, object value)
        {
            var control = Get(handle);
            if (control is null)
            {
                return;
            }

            switch (name)
            {
                case "Checked":
                    var checkbox = control as CheckBox;
                    if (checkbox is not null)
                    {
                        Dispatcher.UIThread.Invoke(() => checkbox.IsChecked = (bool)value).ToString();
                    }
                    break;
                case "SelectedItem":
                    if (control is ComboBox dropdown)
                    {
                        Dispatcher.UIThread.Invoke(() => dropdown.SelectedItem = value).ToString();
                    }
                    break;
            }
        }

        public long Label(long handle, string caption, int x, int y, int width, int height)
        {
            var form = Get(handle) as Form;
            if (form is null)
            {
                return 0;
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                return Register(form.AddLabel(x, y, width, height, caption));
            });
        }

        public long Dropdown(long handle, List<string> items, int x, int y, int width, int height)
        {
            var form = Get(handle) as Form;
            if (form is null)
            {
                return 0;
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                return Register(form.AddCombobox(x, y, items));
            });
        }

        public long Checkbox(long handle, string caption, int x, int y)
        {
            var form = Get(handle) as Form;
            if (form is null)
            {
                return 0;
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                return Register(form.AddCheckbox(x, y, caption));
            });
        }

        public long Button(long handle, string caption, Callback onClick, int x, int y, int width, int height)
        {
            var form = Get(handle) as Form;
            if (form is null)
            {
                return 0;
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                return Register(form.AddButton(x, y, width, height, caption, () => onClick()));
            });
        }

        public bool IsChecked(long handle)
        {
            var checkbox = Get(handle) as CheckBox;
            if (checkbox is null)
            {
                return false;
            }

            bool result = Dispatcher.UIThread.Invoke(() => checkbox.IsChecked ?? false);
            return result;
        }
    }
}
