using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    /// <summary>
    /// RButton with a gear icon in the top-right corner
    /// </summary>
    public class RButtonWithGear : RButton
    {
        private bool _showGear = true;
        private Rectangle _gearRect;
        private const int GEAR_SIZE = 20;
        private const int GEAR_MARGIN = 5;

        public bool ShowGear
        {
            get { return _showGear; }
            set
            {
                if (_showGear != value)
                {
                    _showGear = value;
                    Invalidate();
                }
            }
        }

        public event EventHandler? GearClick;

        public RButtonWithGear()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (!_showGear) return;

            // Calculate gear icon position (bottom-right corner)
            _gearRect = new Rectangle(
                Width - GEAR_SIZE - GEAR_MARGIN,
                Height - GEAR_SIZE - GEAR_MARGIN,
                GEAR_SIZE,
                GEAR_SIZE
            );

            // Draw semi-transparent white background circle for better visibility
            using (var bgBrush = new SolidBrush(Color.FromArgb(180, Color.White)))
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pevent.Graphics.FillEllipse(bgBrush, _gearRect);
            }

            // Draw gear icon with color adjustment (make it darker for contrast)
            try
            {
                if (Properties.Resources.icons8_settings_32 != null)
                {
                    pevent.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // Create a color matrix to make the icon darker/more visible
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                    {
                        new float[] {0.3f, 0, 0, 0, 0},
                        new float[] {0, 0.3f, 0, 0, 0},
                        new float[] {0, 0, 0.3f, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                    });

                    var imageAttributes = new System.Drawing.Imaging.ImageAttributes();
                    imageAttributes.SetColorMatrix(colorMatrix);

                    pevent.Graphics.DrawImage(
                        Properties.Resources.icons8_settings_32,
                        _gearRect,
                        0, 0,
                        Properties.Resources.icons8_settings_32.Width,
                        Properties.Resources.icons8_settings_32.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes
                    );
                }
            }
            catch
            {
                // Fallback: draw a simple dark circle if icon is not available
                using (var brush = new SolidBrush(Color.FromArgb(200, Color.DarkGray)))
                {
                    pevent.Graphics.FillEllipse(brush, _gearRect);
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_showGear && _gearRect.Contains(e.Location))
            {
                // Click on gear icon
                GearClick?.Invoke(this, EventArgs.Empty);
                return; // Don't trigger normal button click
            }

            // Normal button click
            base.OnMouseClick(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_showGear && _gearRect.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }
    }
}

