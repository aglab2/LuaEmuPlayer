using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LuaEmuPlayer.Models
{
    public class Form : Window
    {
        static long _nextId = 1;

        public delegate void OnClose();
        public delegate void OnClick();

        readonly Canvas _canvas = new Canvas();

        public Form(int width, int height, string title, OnClose onClose)
        {
            Title = title;
            Width = width;
            Height = height;
            Closed += (_, _) => onClose();
            Tag = _nextId++;

            Content = _canvas;
        }

        Control Add(Control obj, int x, int y)
        {
            Canvas.SetLeft(obj, x);
            Canvas.SetTop(obj, y);
            obj.Tag = _nextId++;
            _canvas.Children.Add(obj);
            return obj;
        }

        public Control AddButton(int x, int y, int width, int height, string label, OnClick onClick)
        {
            var button = new Button
            {
                Content = label,
                Width = width,
                Height = height
            };

            button.Click += (_, __) => { onClick(); };
            return Add(button, x, y);
        }

        public Control AddCheckbox(int x, int y, string label)
        {
            var checkbox = new CheckBox
            {
                Content = label
            };
            return Add(checkbox, x, y);
        }

        public Control AddLabel(int x, int y, int width, int height, string label)
        {
            var textBlock = new TextBlock
            {
                Text = label,
                Width = width,
                Height = height,
            };
            return Add(textBlock, x, y);
        }

        public Control AddCombobox(int x, int y, List<string> items)
        {
            var comboBox = new ComboBox();
            comboBox.ItemsSource = items;
            return Add(comboBox, x, y);
        }
    }
}
