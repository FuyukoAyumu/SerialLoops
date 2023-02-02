﻿using Eto.Drawing;
using SkiaSharp;

namespace SerialLoops.Utility
{
    public class SKGuiImage : Bitmap
    {
        public SKBitmap SkBitmap { get; set; }

        public SKGuiImage(SKBitmap skBitmap) : base(skBitmap.Encode(SKEncodedImageFormat.Png, 1).AsStream())
        {
        }
    }
}