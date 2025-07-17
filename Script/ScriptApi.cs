using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using SkiaSharp;
using Ephemera.NBagOfTricks;


// Processing emulation script stuff. TODOX make this self-documenting and gen ScriptApi.md.
// The properties and functions are organized similarly to the API specified in https://processing.org/reference/.

// The following sections list the supported elements in roughly the same structure as the [Processing API](https://processing.org/reference/).
// Refer to that document for API specifics.  

// There are lots of unimplemented functions and properties, including some of the overloaded flavors. If it's not implemented,
// you get either a compiler error or a runtime warning.  

// Note also that a lot of these have not been properly tested. Eventually there may be a real unit test project.



namespace NProcessing.Script
{
    public partial class ScriptCore
    {
        #region Definitions - same values as Processing
        //---- Math
        public const double QUARTER_PI = (Math.PI / 4.0);
        public const double HALF_PI = (Math.PI / 2.0);
        public const double PI = (Math.PI);
        public const double TWO_PI = (Math.PI * 2.0);
        public const double TAU = (Math.PI * 2.0);
        //---- Mouse buttons, keyboard arrows
        public const int LEFT = 37;
        public const int UP = 38;
        public const int RIGHT = 39;
        public const int DOWN = 40;
        public const int CENTER = 3;
        //---- Keys
        public const int BACKSPACE = 8;
        public const int TAB = 9;
        public const int ENTER = 6;
        public const int RETURN = 13;
        public const int ESC = 27;
        public const int DELETE = 127;
        //---- keyCodes
        public const int CODED = 0xFF;
        public const int ALT = 8;
        public const int CTRL = 2;
        public const int SHIFT = 1;
        //---- Arc styles
        public const int OPEN = 1;
        public const int CHORD = 2;
        public const int PIE = 3;
        //---- Alignment
        public const int BASELINE = 0;
        public const int TOP = 101;
        public const int BOTTOM = 102;
        //---- Drawing defs
        public const int CORNER = 0;
        public const int CORNERS = 1;
        public const int RADIUS = 2;
        public const int SQUARE = 1;
        public const int ROUND = 2;
        public const int PROJECT = 4;
        public const int MITER = 8;
        public const int BEVEL = 32;
        //---- Color mode
        public const int RGB = 1;
        //public const int ARGB = 2;
        public const int HSB = 3;
        //public const int ALPHA = 4;
        //---- Cursor types
        public const int ARROW = 0;
        public const int CROSS = 1;
        public const int TEXT = 2;
        public const int WAIT = 3;
        public const int HAND = 12;
        public const int MOVE = 13;
        //---- Misc items
        public const int CLOSE = 2;
        #endregion

        #region Structure
        //---- Function overrides
        public virtual void setup() { }
        public virtual void draw() { }

        //---- Script functions
        protected void loop() { _loop = true; }
        protected void noLoop() { _loop = false; }
        protected void redraw() { _redraw = true; }
        #endregion

        #region Environment 
        //---- Script properties
        public bool focused { get; internal set; }
        public int frameCount { get; internal set; } = 1;
        public int frameRate() { return FrameRate; }
        public void frameRate(int num) { FrameRate = num; }
        public int height { get; internal set; } = 300;
        public void noSmooth() { _smooth = false; }
        public void size(int w, int h) { height = h; width = w; }
        public void smooth() { _smooth = true; }
        public void smooth(int level) { _smooth = level > 0; }
        public int width { get; internal set; } = 300;
        #endregion

        #region Data
        public int @int(double val) { return (int)val; }
        public int @int(string val) { return int.Parse(val); }
        public string str(object value) { return value.ToString() ?? "null"; }

        #region Data - String Functions
        public string join(string[] list, char separator) { return string.Join(separator.ToString(), list); }
        public string[] split(string value, char delim) { return value.SplitByToken(delim.ToString()).ToArray(); }
        public string[] split(string value, string delim) { return value.SplitByToken(delim).ToArray(); }
        public string[] splitTokens(string value, string delim) { return value.SplitByTokens(delim).ToArray(); }
        public string trim(string str) { return str.Trim(); }
        public string[] trim(string[] array) { return array.Select(i => i.Trim()).ToArray(); }
        #endregion

        #region Data - Array Functions
        #endregion
        #endregion

        #region Shape 

        #region Shape - 2D Primitives
        public void arc(double x1, double y1, double x2, double y2, double angle1, double angle2, int style)
        {
            x1 -= width / 2;
            y1 -= height / 2;
            angle1 = MathUtils.RadiansToDegrees(angle1);
            angle2 = MathUtils.RadiansToDegrees(angle2);
            SKPath path = new();
            SKRect rect = new((float)x1, (float)y1, (float)x2, (float)y2);
            path.AddArc(rect, (float)angle1, (float)angle2);

            //https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/arcs

            switch (style)
            {
                case OPEN: // default is OPEN stroke with a PIE fill.
                    if (_fill.Color != SKColors.Transparent)
                    {
                        _canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        _canvas.DrawPath(path, _pen);
                    }
                    break;

                case CHORD:
                    path.Close();
                    if (_fill.Color != SKColors.Transparent)
                    {
                        _canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        _canvas.DrawPath(path, _pen);
                    }
                    break;

                case PIE:
                    path.MoveTo(rect.MidX, rect.MidY);
                    if (_fill.Color != SKColors.Transparent)
                    {
                        _canvas.DrawPath(path, _fill);
                    }
                    if (_pen.StrokeWidth != 0)
                    {
                        _canvas.DrawPath(path, _pen);
                    }
                    break;
            }
        }

        public void arc(double x1, double y1, double x2, double y2, double angle1, double angle2)
        {
            arc(x1, y1, x2, y2, angle1, angle2 - angle1, OPEN);
        }

        public void ellipse(double x1, double y1, double w, double h)
        {
            if (_fill.Color != SKColors.Transparent)
            {
                _canvas.DrawOval((float)x1, (float)y1, (float)w / 2, (float)h / 2, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                _canvas.DrawOval((float)x1, (float)y1, (float)w / 2, (float)h / 2, _pen);
            }
        }

        public void line(double x1, double y1, double x2, double y2)
        {
            _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, _pen);
        }

        public void point(double x, double y)
        {
            _canvas.DrawPoint((float)x, (float)y, _pen);
        }

        public void quad(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            SKPoint[] points = new SKPoint[4] { new SKPoint((float)x1, (float)y1), new SKPoint((float)x2, (float)y2), new SKPoint((float)x3, (float)y3), new SKPoint((float)x4, (float)y4) };

            if (_fill.Color != SKColors.Transparent)
            {
                _canvas.DrawPoints(SKPointMode.Polygon, points, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                _canvas.DrawPoints(SKPointMode.Polygon, points, _pen);
            }
        }

        public void rect(double x1, double y1, double w, double h)
        {
            if (_fill.Color != SKColors.Transparent)
            {
                _canvas.DrawRect((float)x1, (float)y1, (float)w, (float)h, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                _canvas.DrawRect((float)x1, (float)y1, (float)w, (float)h, _pen);
            }
        }

        public void triangle(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            SKPoint[] points = new SKPoint[3] { new SKPoint((float)x1, (float)y1), new SKPoint((float)x2, (float)y2), new SKPoint((float)x3, (float)y3) };

            if (_fill.Color != SKColors.Transparent)
            {
                _canvas.DrawPoints(SKPointMode.Lines, points, _fill);
            }

            if (_pen.StrokeWidth != 0)
            {
                _canvas.DrawPoints(SKPointMode.Lines, points, _pen);
            }
        }
        #endregion

        #region Shape - Curves
        public void bezier(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            // Draw path with cubic Bezier curve.
            using SKPath path = new();
            path.MoveTo((float)x1, (float)y1);
            path.CubicTo((float)x2, (float)y2, (float)x3, (float)y3, (float)x4, (float)y4);
            _canvas.DrawPath(path, _pen);
        }
        #endregion

        #region Shape - 3D Primitives
        #endregion

        #region Shape - Attributes
        public void strokeCap(int style)
        {
            switch (style)
            {
                case PROJECT: _pen.StrokeCap = SKStrokeCap.Square; break;
                case ROUND: _pen.StrokeCap = SKStrokeCap.Round; break;
                case SQUARE: _pen.StrokeCap = SKStrokeCap.Butt; break;
            }
        }

        public void strokeJoin(int style)
        {
            switch (style)
            {
                case BEVEL: _pen.StrokeJoin = SKStrokeJoin.Bevel; break;
                case MITER: _pen.StrokeJoin = SKStrokeJoin.Miter; break;
                case ROUND: _pen.StrokeJoin = SKStrokeJoin.Round; break;
            }
        }

        public void strokeWeight(int w) { _pen.StrokeWidth = w; }
        #endregion

        #region Shape - Vertex
        public void beginShape() { _vertexes.Clear(); }
        public void endShape(int mode = -1)
        {
            SKPoint[] points = _vertexes.ToArray();

            if (mode == -1) // Not closed - draw lines.
            {
                _canvas.DrawPoints(SKPointMode.Lines, points, _pen);
            }
            else if (mode == CLOSE)
            {
                using var path = new SKPath();
                for (int i = 0; i < _vertexes.Count; i++)
                {
                    if (i == 0)
                    {
                        path.MoveTo(_vertexes[i]);
                    }
                    else
                    {
                        path.LineTo(_vertexes[i]);
                    }
                }
                path.Close();

                if (_fill.Color != SKColors.Transparent)
                {
                    _canvas.DrawPath(path, _fill);
                }

                if (_pen.StrokeWidth != 0)
                {
                    _canvas.DrawPath(path, _pen);
                }
            }
            else
            {
                NotImpl(nameof(endShape));
            }
        }
        public void vertex(double x, double y) { _vertexes.Add(new SKPoint((float)x, (float)y)); } // Just x/y.
        #endregion

        #region Shape - Loading & Displaying
        #endregion
        #endregion

        #region Input
        #region Input - Mouse
        //---- Script properties
        public bool mouseIsPressed { get; internal set; } = false;
        public int mouseButton { get; internal set; } = LEFT;
        public int mouseWheelValue { get; internal set; } = 0;
        public int mouseX { get; internal set; } = 0;
        public int mouseY { get; internal set; } = 0;
        public int pMouseX { get; internal set; } = 0;
        public int pMouseY { get; internal set; } = 0;
        //---- Function overrides
        public virtual void mouseClicked() { }
        public virtual void mouseDragged() { }
        public virtual void mouseMoved() { }
        public virtual void mousePressed() { }
        public virtual void mouseReleased() { }
        public virtual void mouseWheel() { }
        #endregion

        #region Input - Keyboard
        //---- Script properties
        public char key { get; internal set; } = ' ';
        public int keyCode { get; internal set; } = 0;
        public bool keyIsPressed { get; internal set; } = false;
        //---- Function overrides
        public virtual void keyPressed() { }
        public virtual void keyReleased() { }
        public virtual void keyTyped() { }
        #endregion

        #region Input - Files
        public string[] loadStrings(string filename) { return File.ReadAllLines(filename); }
        #endregion

        #region Input - Time & Date
        public int day() { return DateTime.Now.Day; }
        public int hour() { return DateTime.Now.Hour; }
        public int minute() { return DateTime.Now.Minute; }
        public int millis() { return (int)(RealTime * 1000); }
        public int month() { return DateTime.Now.Month; }
        public int second() { return DateTime.Now.Second; }
        public int year() { return DateTime.Now.Year; }
        #endregion
        #endregion

        #region Output
        #region Output - Text Area
        public void print(params object[] vars)
        {
            _logger.Info(string.Join(" ", vars));
        }
        public void printArray(Array what)
        {
            for (int i = 0; i < what.Length; i++)
            {
                _logger.Info($"array[{i}]: {what.GetValue(i)}");
            }
        }
        #endregion

        #region Output - Image
        #endregion

        #region Output - Files
        #endregion

        #region Output - PrintWriter
        #endregion
        #endregion

        #region Transform 
        public void popMatrix() { _canvas.SetMatrix(_matrixStack.Pop()); }
        public void pushMatrix() { _matrixStack.Push(_canvas.TotalMatrix); }
        public void rotate(double angle) { _canvas.RotateRadians((float)angle); }
        public void scale(double sc) { _canvas.Scale((float)sc); }
        public void scale(double scx, double scy) { _canvas.Scale((float)scx, (float)scy); }
        public void translate(double dx, double dy) { _canvas.Translate((float)dx, (float)dy); }
        #endregion

        #region Lights & Camera
        #region Lights & Camera - Lights
        #endregion

        #region Lights & Camera - Camera
        #endregion

        #region Lights & Camera - Coordinates
        #endregion

        #region Lights & Camera - Material Properties
        #endregion
        #endregion

        #region Color
        #region Color - Setting
        public void background(double gray) { _bgColor = SafeColor(gray, gray, gray, 255); _canvas.Clear(_bgColor); }
        public void background(double gray, double alpha) { _bgColor = SafeColor(gray, gray, gray, alpha); _canvas.Clear(_bgColor); }
        public void background(double v1, double v2, double v3) { color c = new(v1, v2, v3, 255); _bgColor = c.NativeColor; _canvas.Clear(_bgColor); }
        public void background(double v1, double v2, double v3, double alpha) { color c = new(v1, v2, v3, alpha); _bgColor = c.NativeColor; _canvas.Clear(_bgColor); }
        public void background(PImage img) { _canvas.DrawBitmap(img.bmp, new SKRect(0, 0, width, height)); }
        public void colorMode(int mode, double max = 255) { Script.color.SetMode(mode, max, max, max, max); }
        public void colorMode(int mode, int max1, int max2, int max3, int maxA = 255) { Script.color.SetMode(mode, max1, max2, max3, maxA); }
        public void fill(color clr) { _fill.Color = SafeColor(clr.R, clr.G, clr.B, clr.A); }
        public void fill(double gray) { _fill.Color = SafeColor(gray, gray, gray, 255); }
        public void fill(double gray, double alpha) { _fill.Color = SafeColor(gray, gray, gray, alpha); }
        public void fill(double v1, double v2, double v3) { color c = new(v1, v2, v3, 255); _fill.Color = c.NativeColor; }
        public void fill(double v1, double v2, double v3, double alpha) { color c = new(v1, v2, v3, alpha); _fill.Color = c.NativeColor; }
        public void noFill() { _fill.Color = SKColors.Transparent; }
        public void noStroke() { _pen.StrokeWidth = 0; }
        public void stroke(color clr) { _pen.Color = clr.NativeColor; }
        public void stroke(double gray) { _pen.Color = SafeColor(gray, gray, gray, 255); }
        public void stroke(double gray, double alpha) { _pen.Color = SafeColor(gray, gray, gray, alpha); }
        public void stroke(double v1, double v2, double v3) { color c = new(v1, v2, v3, 255); _pen.Color = c.NativeColor; }
        public void stroke(double v1, double v2, double v3, double alpha) { color c = new(v1, v2, v3, alpha); _pen.Color = c.NativeColor; }
        #endregion

        #region Color - Creating & Reading
        public color color(double v1, double v2, double v3, double a = 255) { return new color(v1, v2, v3, a); }
        public color color(double gray, double a = 255) { return new color(gray, a); }
        public double alpha(color color) { return color.A; }
        public double blue(color color) { return color.B; }
        public double brightness(color color) { return color.Brightness; }
        public double green(color color) { return color.G; }
        public double hue(color color) { return color.Hue; }
        public double red(color color) { return color.R; }
        public double saturation(color color) { return color.Saturation; }

        public color lerpColor(color c1, color c2, double amt)
        {
            amt = constrain(amt, 0, 1);
            double r = lerp(c1.R, c2.R, amt);
            double b = lerp(c1.B, c2.B, amt);
            double g = lerp(c1.G, c2.G, amt);
            double a = lerp(c1.A, c2.A, amt);
            return new color(r, g, b, a);
        }
        #endregion
        #endregion

        #region Image 

        #region Image - Loading & Displaying
        public void image(PImage img, double x, double y)
        {
            _canvas.DrawBitmap(img.bmp, (float)x, (float)y); // unscaled
        }

        public void image(PImage img, double x1, double y1, double x2, double y2)
        {
            _canvas.DrawBitmap(img.bmp, new SKRect((float)x1, (float)y1, (float)x2, (float)y2)); // scaled
        }

        public PImage loadImage(string filename)
        {
            return new PImage(filename);
        }
        #endregion

        #region Image - Textures
        #endregion

        #region Image - Pixels
        #endregion
        #endregion

        #region Rendering 

        #region Rendering - Shaders
        #endregion
        #endregion

        #region Typography
        #region Typography - Loading & Displaying
        public PFont createFont(string name, int size)
        {
            return new PFont(name, size);
        }

        public void text(string s, double x, double y)
        {
            _canvas.DrawText(s, (float)x, (float)y, _textPaint);
        }

        public void textFont(PFont font)
        {
            _textPaint.TextSize = (float)font.size;
            _textPaint.Typeface = SKTypeface.FromFamilyName(font.name);
        }
        #endregion

        #region Typography - Attributes
        public void textAlign(int alignX)
        {
            switch (alignX)
            {
                case LEFT: _textPaint.TextAlign = SKTextAlign.Left; break;
                case CENTER: _textPaint.TextAlign = SKTextAlign.Center; break;
                case RIGHT: _textPaint.TextAlign = SKTextAlign.Right; break;
            }
            NotImpl(nameof(textAlign));
        }
        public void textSize(int pts) { _textPaint.TextSize = pts; }
        double textWidth(string s) { return _textPaint.MeasureText(s); }
        double textWidth(char ch) { return textWidth(ch.ToString()); }
        #endregion

        #region Typography - Metrics
        #endregion
        #endregion

        #region Math
        #region Math - Calculation
        public int abs(int val) { return Math.Abs(val); }
        public double abs(double val) { return Math.Abs(val); }
        public int ceil(double val) { return (int)Math.Ceiling(val); }
        public double constrain(double val, double min, double max) { return MathUtils.Constrain(val, min, max); }
        public int constrain(int val, int min, int max) { return MathUtils.Constrain(val, min, max); }
        public double dist(double x1, double y1, double x2, double y2) { return Math.Sqrt(sq(x1 - x2) + sq(y1 - y2)); }
        public double dist(double x1, double y1, double z1, double x2, double y2, double z2) { return Math.Sqrt(sq(x1 - x2) + sq(y1 - y2) + sq(z1 - z2)); }
        public double exp(double exponent) { return Math.Exp(exponent); }
        public int floor(double val) { return (int)Math.Floor(val); }
        public double lerp(double start, double stop, double amt) { return start + (stop - start) * amt; }
        public double log(double val) { return Math.Log(val); }
        public double mag(double x, double y) { return Math.Sqrt(sq(x) + sq(y)); }
        public double mag(double x, double y, double z) { return Math.Sqrt(sq(x) + sq(y) + sq(z)); }
        public double map(double val, double start1, double stop1, double start2, double stop2) { return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1); }
        public double max(double val1, double val2) { return Math.Max(val1, val2); }
        public double max(double[] vals) { return vals.Max(); }
        public int max(int val1, int val2) { return Math.Max(val1, val2); }
        public int max(int[] vals) { return vals.Max(); }
        public double min(double val1, double val2) { return Math.Min(val1, val2); }
        public double min(double[] vals) { return vals.Min(); }
        public int min(int val1, int val2) { return Math.Min(val1, val2); }
        public int min(int[] vals) { return vals.Min(); }
        public double norm(double val, double start, double stop) { return (val - start) / (stop - start); }
        public double pow(double val, double exponent) { return Math.Pow(val, exponent); }
        public int round(double val) { return (int)Math.Round(val); }
        public double sq(double val) { return Math.Pow(val, 2); }
        public double sqrt(double val) { return Math.Sqrt(val); }
        public int truncate(double val) { return (int)val; }
        #endregion

        #region Math - Trigonometry
        public double acos(double val) { return Math.Acos(val); }
        public double asin(double val) { return Math.Asin(val); }
        public double atan(double val) { return Math.Atan(val); }
        public double atan2(double y, double x) { return Math.Atan2(y, x); }
        public double cos(double angle) { return Math.Cos(angle); }
        public double degrees(double rad) { return 360.0f * rad / (2.0f * PI); }
        public double radians(double degrees) { return 2.0f * PI * degrees / 360.0f; }
        public double sin(double angle) { return Math.Sin(angle); }
        public double tan(double angle) { return Math.Tan(angle); }
        #endregion

        #region Math - Random
        public double random(double max) { return _rand.NextDouble() * max; }
        public double random(double min, double max) { return min + _rand.NextDouble() * (max - min); }
        public int random(int max) { return _rand.Next(max); }
        public int random(int min, int max) { return _rand.Next(min, max); }
        public double randomGaussian()
        {
            double mean = 0;
            double sigma = 1;
            var u1 = _rand.NextDouble();
            var u2 = _rand.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var randNormal = mean + sigma * randStdNormal;
            return randNormal;
        }
        public void randomSeed(int seed) { _rand = new Random(seed); }
        #endregion
        #endregion
    }
}
