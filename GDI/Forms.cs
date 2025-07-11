using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.Reflection;

namespace LuaEmuPlayer.Models
{
    internal class Forms
    {
        public delegate void Callback();

        private static void SafeInvoke(Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private static T SafeInvoke<T>(Control control, Func<T> func)
        {
            if (control.InvokeRequired)
            {
                return (T)control.Invoke(func);
            }
            else
            {
                return func();
            }
        }

        public long Form(int width, int height, string title, Callback onClose, object dispatcher)
        {
            var dispatcherForm = dispatcher as Form;
            return SafeInvoke(dispatcherForm, () =>
            {
                var form = new Form();
                form.Text = title;
                form.Size = new System.Drawing.Size(width + 20, height + 30);
                form.StartPosition = FormStartPosition.CenterScreen;
                form.FormClosed += (_, __) => onClose?.Invoke();

                form.Show();
                return form.Handle.ToInt64();
            });
        }

        public bool Destroy(long? handle)
        {
            if (handle is null)
            {
                return false;
            }

            var form = Control.FromHandle(new IntPtr(handle.Value)) as Form;
            if (form is null)
            {
                return false;
            }

            SafeInvoke(form, () => form.Close());
            return true;
        }

        private Control Get(long handle)
        {
            return Control.FromHandle(new IntPtr(handle));
        }

        public string GetProperty(long handle, string name)
        {
            var control = Get(handle);
            if (control is null)
            {
                return null;
            }

            return SafeInvoke(control, () =>
            {
                var property = control.GetType().GetProperty(name);
                if (property is null)
                {
                    return null;
                }

                var value = property.GetValue(control);
                return value?.ToString();
            });
        }

        public void SetProperty(long handle, string name, object value)
        {
            var control = Get(handle);
            if (control is null)
            {
                return;
            }

            SafeInvoke(control, () =>
            {
                var property = control.GetType().GetProperty(name);
                if (property is null)
                {
                    return;
                }

                var targetType = property.PropertyType;
                var convertedValue = Convert.ChangeType(value, targetType);
                property.SetValue(control, convertedValue);
            });
        }

        public long Label(long handle, string caption, int x, int y, int width, int height)
        {
            var form = Get(handle);
            if (form is null)
            {
                return 0;
            }

            return SafeInvoke(form, () =>
            {
                var label = new Label();
                label.Text = caption;
                label.Location = new System.Drawing.Point(x, y);
                label.Size = new System.Drawing.Size(width, height);

                form.Controls.Add(label);

                return label.Handle.ToInt64();
            });
        }

        public long Dropdown(long handle, List<string> items, int x, int y, int width, int height)
        {
            var form = Get(handle);
            if (form is null)
            {
                return 0;
            }

            return SafeInvoke(form, () =>
            {
                var comboBox = new ComboBox();
                comboBox.Location = new System.Drawing.Point(x, y);
                comboBox.Size = new System.Drawing.Size(width, height);
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                foreach (var item in items)
                {
                    comboBox.Items.Add(item);
                }

                SafeInvoke(form, () => form.Controls.Add(comboBox));

                return comboBox.Handle.ToInt64();
            });
        }

        public long Checkbox(long handle, string caption, int x, int y)
        {
            var form = Get(handle);
            if (form is null)
            {
                return 0;
            }

            return SafeInvoke(form, () =>
            {
                var checkBox = new CheckBox();
                checkBox.Text = caption;
                checkBox.Location = new System.Drawing.Point(x, y);
                checkBox.AutoSize = true;

                SafeInvoke(form, () => form.Controls.Add(checkBox));

                return checkBox.Handle.ToInt64();
            });
        }

        public long Button(long handle, string caption, Callback onClick, int x, int y, int width, int height)
        {
            var form = Get(handle);
            if (form is null)
            {
                return 0;
            }

            return SafeInvoke(form, () =>
            {
                var button = new Button();
                button.Text = caption;
                button.Location = new System.Drawing.Point(x, y);
                button.Size = new System.Drawing.Size(width, height);
                button.Click += (_, __) => onClick?.Invoke();

                SafeInvoke(form, () => form.Controls.Add(button));

                return button.Handle.ToInt64();
            });
        }

        public bool IsChecked(long handle)
        {
            var checkbox = Get(handle) as CheckBox;
            if (checkbox is null)
            {
                return false;
            }

            return SafeInvoke(checkbox, () => checkbox.Checked);
        }
    }
}
