using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaEmuPlayer.ViewModels
{
    internal class Frame
    {
        Bitmap _bitmap;
        public Bitmap Bitmap
        {
            get => _bitmap;
        }

        public Frame(Bitmap bitmap)
        {
            _bitmap = bitmap;
        }
    }
}
