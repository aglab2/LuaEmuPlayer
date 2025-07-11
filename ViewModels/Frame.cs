using Avalonia.Media.Imaging;

namespace LuaEmuPlayer.ViewModels
{
    internal class Frame
    {
        WriteableBitmap _bitmap;
        public WriteableBitmap Bitmap { get => _bitmap; }

        public Frame(WriteableBitmap bitmap)
        {
            _bitmap = bitmap;
        }
    }
}
