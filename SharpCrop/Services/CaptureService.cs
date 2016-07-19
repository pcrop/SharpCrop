﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SharpCrop.Services
{
    class CaptureService
    {
        /// <summary>
        /// Get a Bitmap from screen in the size of the given rectangle.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public Bitmap GetBitmap(Rectangle r)
        {
            var s = GetScaling();
            var rs = new Rectangle((int)((float)r.X * s), (int)((float)r.Y * s), (int)((float)r.Width * s), (int)((float)r.Height * s));

            var bitmap = new Bitmap(rs.Width, rs.Height, PixelFormat.Format32bppArgb);
            var gfx = Graphics.FromImage(bitmap);
            
            gfx.CopyFromScreen(rs.X, rs.Y, 0, 0, new Size(rs.Width, rs.Height), CopyPixelOperation.SourceCopy);

            return bitmap;
        }
        
        /// <summary>
        /// Get the percentage of scalling.
        /// </summary>
        /// <returns></returns>
        public float GetScaling()
        {
            Graphics gfx = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = gfx.GetHdc();

            int logicalHeight = GetDeviceCaps(desktop, 10);
            int physicalHeight = GetDeviceCaps(desktop, 117);

            return (float)physicalHeight / (float)logicalHeight;
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    }
}
