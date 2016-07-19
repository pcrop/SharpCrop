﻿using SharpCrop.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpCrop.Forms
{
    public partial class DrawForm : Form
    {
        private Point mouseDown = Point.Empty;
        private Point mouseMove = Point.Empty;
        private Point mouseUp = Point.Empty;
        private bool isMouseDown = false;

        private CaptureService captureService;
        private UploadService uploadService;
        private ClickForm clickForm;

        /// <summary>
        /// A nonclickable form which background is transparent - so drawing is possible.
        /// </summary>
        public DrawForm(ClickForm parent)
        {
            SuspendLayout();

            Name = "SharpCrop";
            Text = "SharpCrop";
            ClientSize = Screen.PrimaryScreen.Bounds.Size;
            Location = new Point(0, 0);
            
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.White;
            TransparencyKey = Color.White;
            Opacity = 0.75;

            var accessToken = Settings.Default.AccessToken;

            captureService = new CaptureService();
            uploadService = new UploadService(accessToken);
            clickForm = parent;
        }

        /// <summary>
        /// Grab bitmap and upload it to the saved Dropbox account.
        /// </summary>
        /// <param name="r">Bitmap position, size</param>
        private void Upload(Rectangle r)
        {
            // Hide click and draw form
            Hide();
            clickForm.Hide();
            Application.DoEvents();

            // Process upload
            uploadService.UploadBitmap(captureService.GetBitmap(r));
            Application.Exit();
        }

        /// <summary>
        /// Private helper function to construct a rectangle from two points.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        private Rectangle GetRect(Point source, Point dest)
        {
            return new Rectangle(
                Math.Min(source.X, dest.X),
                Math.Min(source.Y, dest.Y),
                Math.Abs(source.X - dest.X),
                Math.Abs(source.Y - dest.Y));
        }

        /// <summary>
        /// Listen for mouse down events and save the coords.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            isMouseDown = true;
            mouseUp = mouseMove = mouseDown = e.Location;
        }

        /// <summary>
        /// Listen for mouse up events.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            isMouseDown = false;
            mouseUp = e.Location;

            var r = GetRect(mouseDown, mouseUp);

            if (r.X >= 0 && r.Y >= 0 && r.Width >= 1 && r.Height >= 1)
            {
                Upload(r);
            }
        }

        /// <summary>
        /// Change the last coord as the mouse moves.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            mouseMove = e.Location;
            Invalidate();
        }

        /// <summary>
        /// Paint the rectangle if the mouse button is down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (isMouseDown)
            {
                e.Graphics.FillRectangle(Brushes.RoyalBlue, GetRect(mouseDown, mouseMove));
            }
        }

        #region Public Events

        /* These methods are needed to make internal functions public to the ClickForm */

        public void CallOnMouseDown(MouseEventArgs e)
        {
            OnMouseDown(e);
        }

        public void CallOnMouseUp(MouseEventArgs e)
        {
            OnMouseUp(e);
        }

        public void CallOnMouseMove(MouseEventArgs e)
        {
            OnMouseMove(e);
        }

        public void CallOnPaint(PaintEventArgs e)
        {
            OnPaint(e);
        }

        #endregion
    }
}