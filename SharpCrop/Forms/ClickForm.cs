﻿using SharpCrop.Provider;
using SharpCrop.Provider.Utils;
using SharpCrop.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace SharpCrop.Forms
{
    public partial class ClickForm : Form
    {
        private bool configShown = false;
        private DrawForm drawForm;
        private IProvider provider;

        /// <summary>
        /// A clickable form which is totally transparent - so no drawing is possible here.
        /// </summary>
        public ClickForm(IProvider provider)
        {
            this.provider = provider;

            InitializeComponent();

            ClientSize = Screen.PrimaryScreen.Bounds.Size;
            Location = new Point(0, 0);
            Opacity = 0.005;

            drawForm = new DrawForm(this);
            drawForm.Show();
        }

        /// <summary>
        /// Grab bitmap and upload it to the saved Dropbox account.
        /// </summary>
        /// <param name="r">Bitmap position, size</param>
        public void Upload(Rectangle r)
        {
            if (configShown)
            {
                return;
            }

            Hide();
            drawForm.Hide();
            Application.DoEvents();

#if __MonoCS__
			Thread.Sleep(500);
#endif

            var name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "." + ConfigHelper.Memory.FormatExt;
            var bitmap = CaptureHelper.GetBitmap(r);

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ConfigHelper.Memory.FormatType);

                var url = provider.Upload(name, stream);

                if (ConfigHelper.Memory.Copy)
                {
#if __MonoCS__
                    var form = new CopyForm(url);
                    form.FormClosed += (object sender, FormClosedEventArgs e) => Application.Exit();
                    form.Show();
#else
                    Clipboard.SetText(url);
#endif
                }
            }

            ToastFactory.CreateToast("Uploaded successfully!", 3000, () => 
            {
#if !__MonoCS__
                Application.Exit();
#endif
            });
        }

        /// <summary>
        /// Show ConfigForm and hide ClickForm and DrawForm.
        /// </summary>
        private void ShowConfig()
        {
            configShown = true;

            drawForm.Hide();
            Hide();

            var form = new ConfigForm();

            form.Show();
            form.FormClosed += (object sender, FormClosedEventArgs ev) =>
            {
                configShown = false;

                drawForm.Reset();
                drawForm.Show();
                Show();
            };
        }

        /// <summary>
        /// Listen for key presses.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Application.Exit();
                    break;
                case Keys.F1:
                    ShowConfig();
                    break;
            }
        }

        /// <summary>
        /// Hide Form from the Alt + Tab application switcher menu.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var Params = base.CreateParams;
                Params.ExStyle |= 0x80;
                return Params;
            }
        }

        #region Mouse Events

        /* These mouse events are binded to DrawForm */

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            drawForm.CallOnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            drawForm.CallOnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            drawForm.CallOnMouseMove(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            drawForm.CallOnPaint(e);
        }

        #endregion
    }
}