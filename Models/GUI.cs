using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using LuaEmuPlayer.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace LuaEmuPlayer.Models
{
    internal class GUI
    {
        WriteableBitmap _back = new(new Avalonia.PixelSize(800, 450), new Avalonia.Vector(96, 96));
        WriteableBitmap _front = new(new Avalonia.PixelSize(800, 450), new Avalonia.Vector(96, 96));
        SKImageInfo _skiaImageInfo = new(800, 450);

        ILockedFramebuffer _framebuffer;
        SKBitmap _skiaBitMap;
        SKCanvas _skiaCanvas;

        Dictionary<string, SKImage> _skiaImages = new();
        Dictionary<string, SKTypeface> _skiaTypeFaces = new();
        Dictionary<Tuple<string, int>, SKFont> _skiaFonts = new();

        public int Width { get => _skiaImageInfo.Width; }
        public int Height { get => _skiaImageInfo.Height; }

        public Frame SwapBuffers()
        {
            if (!StopRender())
            {
                return null;
            }

            WriteableBitmap present = _back;
            _back = _front;
            _front = present;
            return new Frame(present);
        }

        void StartRender()
        {
            if (_framebuffer is null)
            {
                _framebuffer = _back.Lock();
                _skiaBitMap = new SKBitmap(_skiaImageInfo);
                _skiaBitMap.SetPixels(_framebuffer.Address);
                _skiaCanvas = new SKCanvas(_skiaBitMap);
                _skiaCanvas.Clear();
            }
        }

        bool StopRender()
        {
            if (_framebuffer is null)
            {
                return false;
            }

            _skiaCanvas.Dispose();
            _skiaBitMap.Dispose();
            _framebuffer.Dispose();
            _framebuffer = null;
            return true;
        }

        SKImage LoadImage(string path)
        {
            if (_skiaImages.TryGetValue(path, out SKImage image))
            {
                return image;
            }
            else
            {
                var imageNew = SKImage.FromEncodedData(path);
                _skiaImages[path] = imageNew;
                return imageNew;
            }
        }

        SKTypeface LoadTypeface(string name)
        {
            if (_skiaTypeFaces.TryGetValue(name, out SKTypeface tf))
            {
                return tf;
            }
            else
            {
                var tfNew = SKTypeface.FromFamilyName(name);
                _skiaTypeFaces[name] = tfNew;
                return tfNew;
            }
        }

        SKFont LoadFont(string name, int size)
        {
            var key = new Tuple<string, int>(name, size);
            if (_skiaFonts.TryGetValue(key, out SKFont font))
            {
                return font;
            }
            else
            {
                var tf = LoadTypeface(name);
                var fontNew = tf.ToFont(size);
                _skiaFonts[key] = fontNew;
                return fontNew;
            }
        }

        public int DrawImage(string path, int x, int y, int width, int height)
        {
            StartRender();
            _skiaCanvas.DrawImage(LoadImage(path), new SKRect(x, y, x + width, y + height));
            return 0;
        }

        SKTextAlign? ToSKTextAlign(string name)
        {
            if (name is null)
            {
                return null;
            }

            if (name == "center")
            {
                return SKTextAlign.Center;
            }
            if (name == "right")
            {
                return SKTextAlign.Right;
            }
            if (name == "left")
            {
                return SKTextAlign.Left;
            }

            return null;
        }

        SKColor? ToSKColor(string name)
        {
            if (name is null)
            {
                return null;
            }

            if (name == "red")
            {
                return new SKColor(0xff, 0x00, 0x00);
            }
            if (name == "lightblue")
            {
                return new SKColor(0xFF, 0x33, 0xAA);
            }
            if (name == "lightgreen")
            {
                return new SKColor(0x90, 0xee, 0x90);
            }
            if (name == "orange")
            {
                return new SKColor(0x11, 0xFF, 0x44);
            }
            if (name == "yellow")
            {
                return new SKColor(0x11, 0xFF, 0xFF);
            }
            if (name == "gray")
            {
                return new SKColor(0xa0, 0xa0, 0xa0);
            }
            if (name == "black")
            {
                return new SKColor(0x00, 0x00, 0x00);
            }
            if (name == "white")
            {
                return new SKColor(0xff, 0xff, 0xff);
            }
            if (name[0] == '#')
            {
                if (name.Length == 7)
                {
                    return new SKColor(
                        Convert.ToByte(name.Substring(1, 2), 16),
                        Convert.ToByte(name.Substring(3, 2), 16),
                        Convert.ToByte(name.Substring(5, 2), 16));
                }
            }

            return null;
        }

        public int DrawString(int x, int y, string message, string foreColor, string backColor, int? fontSizez, string fontFamily, string fontStyle, string horizAlign, string vertAlign)
        {
            StartRender();
            int fontSize = fontSizez ?? 16;
            var font = LoadFont(fontFamily, fontSize);
            using (var paint = new SKPaint())
            {
                var align = ToSKTextAlign(horizAlign);
                if (align.HasValue)
                {
                    paint.TextAlign = align.Value;
                }

                var color = ToSKColor(foreColor);
                if (color.HasValue)
                {
                    paint.Color = color.Value;
                }
                else
                {
                    paint.Color = new SKColor(0xff, 0xff, 0xff);
                }

                _skiaCanvas.DrawText(message, x, y + fontSize / 1.3f, font, paint);
            }
            return 0;
        }

        public void DrawBox(int x, int y, int x2, int y2, string line, string background)
        {
            var w = x2 - x;
            var h = y2 - y;
            StartRender();
            var lineColor = ToSKColor(line) ?? new SKColor(0xff, 0xff, 0xff);
            _skiaCanvas.DrawRect(new SKRect(x, y, x + w, y + h), new SKPaint { Color = lineColor, Style = SKPaintStyle.Stroke });
            var backgroundColor = ToSKColor(background) ?? new SKColor(0, 0, 0);
            _skiaCanvas.DrawRect(new SKRect(x + 1, y + 1, x + w - 1, y + h - 1), new SKPaint { Color = backgroundColor, Style = SKPaintStyle.Fill });
        }

        public class Residue
        {
            public WriteableBitmap back;
            public WriteableBitmap front;
            
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

            var residue = new Residue() { back = _back, front = _front };
            _back = new(new Avalonia.PixelSize(w, h), new Avalonia.Vector(96, 96));
            _front = new(new Avalonia.PixelSize(w, h), new Avalonia.Vector(96, 96));
            _skiaImageInfo = new(w, h);

            return residue;
        }
    }
}
