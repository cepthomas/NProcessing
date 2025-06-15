using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using SkiaSharp;
using Ephemera.NBagOfTricks;


namespace NProcessing.Script
{
    public partial class ScriptBase
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Script");

        /// <summary>Script randomizer.</summary>
        Random _rand = new();

        /// <summary>Loop option.</summary>
        internal bool _loop = true;

        /// <summary>Redraw option.</summary>
        internal bool _redraw = false;

        /// <summary>Current working object to draw on.</summary>
        #nullable disable
        internal SKCanvas _canvas;
        #nullable enable
        #endregion

        #region Fields - graphics/processing
        /// <summary>Current font to draw with.</summary>
        readonly SKPaint _textPaint = new()
        {
            TextSize = 12,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Left,
            IsAntialias = true
        };

        /// <summary>Current pen to draw with.</summary>
        readonly SKPaint _pen = new()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            IsStroke = true,
            StrokeWidth = 1,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current brush to draw with.</summary>
        readonly SKPaint _fill = new()
        {
            Color = SKColors.Transparent,
            Style = SKPaintStyle.Fill,
            IsStroke = false,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current drawing points.</summary>
        readonly List<SKPoint> _vertexes = [];

        /// <summary>General purpose stack</summary>
        readonly Stack<SKMatrix> _matrixStack = [];

        /// <summary>Background color.</summary>
        SKColor _bgColor = SKColors.LightGray;

        /// <summary>Smoothing option.</summary>
        bool _smooth = true;
        #endregion

        #region Properties - dynamic things shared between host and script at runtime
        /// <summary>Main -> Script</summary>
        public double RealTime { get; set; } = 0.0;

        /// <summary>Main -> Script -> Main</summary>
        public int FrameRate { get; set; } = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptBase()
        {
            ResetVars();
        }

        /// <summary>
        /// Ugly way to deal with static vars in color class. TODO?
        /// </summary>
        public void ResetVars()
        {
            Script.color.ResetMode();
        }
        #endregion

        #region Private functions
        /// <summary>Handle unimplemented script elements that we can safely ignore but do tell the user.</summary>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        void NotImpl(string name, string desc = "")
        {
            _logger.Warn($"{name} not implemented. {desc}");
        }

        /// <summary>Bounds check a color definition./// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        SKColor SafeColor(double r, double g, double b, double a)
        {
            r = constrain(r, 0, 255);
            g = constrain(g, 0, 255);
            b = constrain(b, 0, 255);
            a = constrain(a, 0, 255);
            return new SKColor((byte)r, (byte)g, (byte)b, (byte)a);
        }
        #endregion
    }
}
