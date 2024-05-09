using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using SkiaSharp;
using Ephemera.NBagOfTricks;


// Processing API stuff.

namespace NProcessing.Script
{
    /// <summary>
    /// Map Processing color to native. Processing uses a 32 bit value as color - this uses a class.
    /// Note that .NET calls it HSV but is actually HSL so you shouldn't use the Color.GetXXX() functions.
    /// https://blogs.msdn.microsoft.com/cjacks/2006/04/12/converting-from-hsb-to-rgb-in-net/
    /// </summary>
    public class color
    {
        #region Storage for some globals
        /// <summary>Color mode: RGB or HSB, corresponding to Red/Green/Blue and Hue/Saturation/Brightness.</summary>
        static int _mode = ScriptBase.RGB;

        /// <summary>Range for the red or hue depending on the current color mode.</summary>
        static double _max1 = 255;

        /// <summary>Range for the green or saturation depending on the current color mode.</summary>
        static double _max2 = 255;

        /// <summary>Range for the blue or brightness depending on the current color mode.</summary>
        static double _max3 = 255;

        /// <summary>Range for the alpha channel.</summary>
        static double _maxA = 255;

        public static void SetMode(int mode, double max1, double max2, double max3, double maxA)
        {
            _mode = mode;
            _max1 = max1;
            _max2 = max2;
            _max3 = max3;
            _maxA = maxA;
        }

        public static void ResetMode()
        {
            _mode = ScriptBase.RGB;
            _max1 = 255;
            _max2 = 255;
            _max3 = 255;
            _maxA = 255;
        }
        #endregion

        #region Properties
        public double Hue { get; private set; } = 0;
        public double Saturation { get; private set; } = 0;
        public double Brightness { get; private set; } = 0;

        public int R { get; private set; } = 0;
        public int G { get; private set; } = 0;
        public int B { get; private set; } = 0;
        public int A { get; private set; } = 0;
        #endregion

        public SKColor NativeColor
        {
            get { return new SKColor((byte)R, (byte)G, (byte)B, (byte)A); }
            set { FromNative(value); }
        }

        public color(double v1, double v2, double v3, double a = 255)
        {
            if (_mode == ScriptBase.HSB)
            {
                FromHSB(v1, v2, v3, a);
            }
            else // RGB
            {
                FromRGB((int)v1, (int)v2, (int)v3, (int)a);
            }
        }

        public color(double gray, double a = 255)
        {
            FromRGB((int)gray, (int)gray, (int)gray, (int)a);
        }

        public color(SKColor native)
        {
            NativeColor = native;
        }

        #region Converters
        void FromHSB(double h, double s, double b, double a)
        {
            // Normalize input values.
            Hue = MathUtils.Map(h, 0, _max1, 0, 1.0);
            Saturation = MathUtils.Map(s, 0, _max2, 0, 1.0);
            Brightness = MathUtils.Map(b, 0, _max3, 0, 1.0);
            A = (int)MathUtils.Map(a, 0, _maxA, 0, 255);

            // Convert them.
            R = G = B = (int)(Brightness * 255.0 + 0.5);

            if (Saturation != 0)
            {
                double hv = (Hue - Math.Floor(Hue)) * 6.0;
                double f = hv - Math.Floor(hv);
                double p = Brightness * (1.0 - Saturation);
                double q = Brightness * (1.0 - Saturation * f);
                double t = Brightness * (1.0 - (Saturation * (1.0 - f)));

                switch ((int)hv)
                {
                    case 0:
                        G = (int)(t * 255.0 + 0.5);
                        B = (int)(p * 255.0 + 0.5);
                        break;
                    case 1:
                        R = (int)(q * 255.0 + 0.5);
                        B = (int)(p * 255.0 + 0.5);
                        break;
                    case 2:
                        R = (int)(p * 255.0 + 0.5);
                        B = (int)(t * 255.0 + 0.5);
                        break;
                    case 3:
                        R = (int)(p * 255.0 + 0.5);
                        G = (int)(q * 255.0 + 0.5);
                        break;
                    case 4:
                        R = (int)(t * 255.0 + 0.5);
                        G = (int)(p * 255.0 + 0.5);
                        break;
                    case 5:
                        G = (int)(p * 255.0 + 0.5);
                        B = (int)(q * 255.0 + 0.5);
                        break;
                }
            }
        }

        void FromRGB(int r, int g, int b, int a)
        {
            // Normalize input values.
            R = MathUtils.Map(r, 0, (int)_max1, 0, 255);
            G = MathUtils.Map(g, 0, (int)_max2, 0, 255);
            B = MathUtils.Map(b, 0, (int)_max3, 0, 255);
            A = (int)MathUtils.Map(a, 0, _maxA, 0, 255);

            // Calc corresponding values.
            double cmax = (R > G) ? R : G;
            if (B > cmax)
            {
                cmax = B;
            }

            double cmin = (R < G) ? R : G;
            if (B < cmin)
            {
                cmin = B;
            }

            Brightness = cmax / 255.0;
            Saturation = cmax != 0 ? (cmax - cmin) / cmax : 0;
            if (Saturation == 0)
            {
                Hue = 0;
            }
            else
            {
                double redc = (cmax - r) / (cmax - cmin);
                double greenc = (cmax - G) / (cmax - cmin);
                double bluec = (cmax - B) / (cmax - cmin);

                if (R == cmax)
                {
                    Hue = bluec - greenc;
                }
                else if (G == cmax)
                {
                    Hue = 2.0f + redc - bluec;
                }
                else
                {
                    Hue = 4.0f + greenc - redc;
                }

                Hue /= 6.0;
                if (Hue < 0)
                {
                    Hue += 1.0;
                }
            }
        }

        void FromARGB(int argb)
        {
            int b = argb >> 00 & 0xFF;
            int g = argb >> 16 & 0xFF;
            int r = argb >> 32 & 0xFF;
            int a = argb >> 48 & 0xFF;

            FromRGB(r, g, b, a);
        }

        void FromNative(SKColor col)
        {
            FromRGB(col.Red, col.Green, col.Blue, col.Alpha);
        }
        #endregion
    }

    /// <summary>
    /// Map Processing PImage class to native.
    /// </summary>
    public class PImage
    {
        // Added native:
        public SKBitmap bmp { get; private set; } = new SKBitmap();
        public color[]? pixels { get; private set; }
        public int width { get { return bmp.Width; } }
        public int height { get { return bmp.Height; } }
        public PImage(string fname) { bmp = SKBitmap.Decode(fname); }
        public PImage(SKBitmap bm) { bmp = bm; }

        public color get(int x, int y)
        {
            return new color(bmp.GetPixel(x, y));
        }

        public PImage get(int x, int y, int width, int height)
        {
            SKBitmap dest = new();
            bmp.ExtractSubset(dest, new SKRectI(x, y, width, height));
            return new PImage(dest);
        }

        public void set(int x, int y, color color)
        {
            bmp.SetPixel(x, y, color.NativeColor);
        }

        //public void set(int x, int y, PImage img) { NotImpl(nameof(save)); }
        //public bool save(string filename) { NotImpl(nameof(save)); }
        //public void loadPixels() { NotImpl(nameof(loadPixels)); }
        //public void updatePixels() { NotImpl(nameof(updatePixels)); }
        //public void updatePixels(int x, int y, int w, int h) { NotImpl(nameof(updatePixels)); }

        public void resize(int width, int height)
        {
            if (width == 0) // proportional
            {
                width = this.width * height / this.height;
            }
            else if (height == 0) // proportional
            {
                height = this.height * width / this.width;
            }

            var scaled = bmp.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            bmp = scaled;
        }
    }

    /// <summary>
    /// Map Processing PFont class to native.
    /// </summary>
    public class PFont
    {
        public string name { get; set; } = "Arial";
        public double size { get; set; } = 12;

        public PFont(string name, int size)
        {
            this.name = name;
            this.size = size;
        }
    }

    /// <summary>
    /// Custom midi data class.
    /// </summary>
    public class PMidiEvent
    {
        const int PITCH_CONTROL = 1000; // same as main app

        public int channel { get; set; } = -1;
        public int note { get; set; } = -1;
        public int velocity { get; set; } = -1;
        public int controller { get; set; } = -1;
        public int value { get; set; } = -1;

        public bool isNoteOn { get { return channel > 0 && note > 0 && velocity > 0; } }
        public bool isNoteOff { get { return channel > 0 && note > 0 && velocity == 0; } }
        public bool isController { get { return channel > 0 && controller > 0 && value > 0; } }
        public bool isPitch { get { return channel > 0 && controller == PITCH_CONTROL && value > 0; } }

        public PMidiEvent(int channel, int note, int velocity, int controller, int value)
        {
            this.channel = channel;
            this.note = note;
            this.velocity = velocity;
            this.controller = controller;
            this.value = value;
        }

        public override string ToString()
        {
            return $"PMidiEvent: channel:{channel} note:{note} velocity:{velocity} controller:{controller} value:{value}";
        }
    }
}
