using LuaEmuPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace LuaEmuPlayer.Models
{
    internal class GUI : IDisposable
    {
        Bitmap _backBuffer;
        Bitmap _frontBuffer;
        Graphics _graphics;
        int _width = 800;
        int _height = 450;

        readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();
        readonly Dictionary<string, FontFamily> _fontFamilies = new Dictionary<string, FontFamily>();
        readonly Dictionary<Tuple<string, int>, Font> _fonts = new Dictionary<Tuple<string, int>, Font>();

        public int Width { get => _width; }
        public int Height { get => _height; }

        public GUI()
        {
            _backBuffer = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
            _frontBuffer = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
        }

        public Frame SwapBuffers()
        {
            if (!StopRender())
            {
                return null;
            }

            Bitmap present = _backBuffer;
            _backBuffer = _frontBuffer;
            _frontBuffer = present;
            return new Frame(present);
        }

        void StartRender()
        {
            if (_graphics is null)
            {
                _graphics = Graphics.FromImage(_backBuffer);
                _graphics.Clear(Color.Black);
                _graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                _graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            }
        }

        bool StopRender()
        {
            if (_graphics is null)
            {
                return false;
            }

            _graphics.Dispose();
            _graphics = null;
            return true;
        }

        Image LoadImage(string path)
        {
            if (_images.TryGetValue(path, out Image image))
            {
                return image;
            }
            else
            {
                var imageNew = Image.FromFile(path);
                _images[path] = imageNew;
                return imageNew;
            }
        }

        FontFamily LoadFontFamily(string name)
        {
            if (_fontFamilies.TryGetValue(name, out FontFamily ff))
            {
                return ff;
            }
            else
            {
                var ffNew = new FontFamily(name);
                _fontFamilies[name] = ffNew;
                return ffNew;
            }
        }

        Font LoadFont(string name, int size)
        {
            var key = new Tuple<string, int>(name, size);
            if (_fonts.TryGetValue(key, out Font font))
            {
                return font;
            }
            else
            {
                var ff = LoadFontFamily(name);
                var fontNew = new Font(ff, size * 0.75f);
                _fonts[key] = fontNew;
                return fontNew;
            }
        }

        public int DrawImage(string path, int x, int y, int width, int height)
        {
            StartRender();
            var image = LoadImage(path);
            _graphics.DrawImage(image, new Rectangle(x, y, width, height));
            return 0;
        }

        StringAlignment? ToStringAlignment(string name)
        {
            if (name is null)
            {
                return null;
            }

            if (name == "center")
            {
                return StringAlignment.Center;
            }
            if (name == "right")
            {
                return StringAlignment.Far;
            }
            if (name == "left")
            {
                return StringAlignment.Near;
            }

            return null;
        }

        Color? ToColor(string name)
        {
            if (name is null)
            {
                return null;
            }

            try
            {
                return ColorTranslator.FromHtml(name);
            }
            catch
            {
                return null;
            }
        }

        public int DrawString(int x, int y, string message, string foreColor, string backColor, int? fontSizez, string fontFamily, string fontStyle, string horizAlign, string vertAlign)
        {
            StartRender();
            int fontSize = fontSizez ?? 16;
            var font = LoadFont(fontFamily, fontSize);

            var color = ToColor(foreColor) ?? Color.White;
            var align = ToStringAlignment(horizAlign) ?? StringAlignment.Near;

            using (var brush = new SolidBrush(color))
            {
                var stringFormat = new StringFormat()
                {
                    Alignment = align,
                    LineAlignment = StringAlignment.Near
                };

                _graphics.DrawString(message, font, brush, x, y, stringFormat);

                stringFormat.Dispose();
            }
            return 0;
        }

        public void DrawBox(int x, int y, int x2, int y2, string line, string background)
        {
            var w = x2 - x;
            var h = y2 - y;
            StartRender();
            var lineColor = ToColor(line) ?? Color.White;
            using (var pen = new Pen(lineColor, 1))
            {
                _graphics.DrawRectangle(pen, x, y, w, h);
            }

            var backgroundColor = ToColor(background) ?? Color.Black;
            using (var brush = new SolidBrush(backgroundColor))
            {
                _graphics.FillRectangle(brush, x + 1, y + 1, w - 2, h - 2);
            }
        }

    public class Residue
        {
            public Bitmap back;
            public Bitmap front;

            public void Dispose()
            {
                back.Dispose();
                front.Dispose();
            }
        }

        public Residue CheckHeight(int h, int w)
        {
            if (h == Height && w == Width)
                return null;

            var residue = new Residue() { back = _backBuffer, front = _frontBuffer };

            _backBuffer = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            _frontBuffer = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            _width = w;
            _height = h;

            return residue;
        }

        public void Dispose()
        {
            _graphics.Dispose();
            _backBuffer.Dispose();
            _frontBuffer.Dispose();

            foreach (var image in _images.Values)
                image.Dispose();
            foreach (var font in _fonts.Values)
                font.Dispose();
            foreach (var fontFamily in _fontFamilies.Values)
                fontFamily.Dispose();

            _images.Clear();
            _fonts.Clear();
            _fontFamilies.Clear();
        }
    }
}
