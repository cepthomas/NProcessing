﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using SkiaSharp;
using NLog;
using NBagOfTricks;


namespace NProcessing.Script
{
    public partial class NpScript : IDisposable
    {
        #region Fields - internal
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Resource clean up.</summary>
        bool _disposed = false;

        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Loop option.</summary>
        internal bool _loop = true;

        /// <summary>Redraw option.</summary>
        internal bool _redraw = false;

        /// <summary>Current working object to draw on.</summary>
        internal SKCanvas _canvas = null;
        #endregion

        #region Fields - graphics/processing
        /// <summary>Current font to draw with.</summary>
        SKPaint _textPaint = new SKPaint()
        {
            TextSize = 12,
            Color = SKColors.Black,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Left,
            IsAntialias = true
        };

        /// <summary>Current pen to draw with.</summary>
        SKPaint _pen = new SKPaint()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            IsStroke = true,
            StrokeWidth = 1,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current brush to draw with.</summary>
        SKPaint _fill = new SKPaint()
        {
            Color = SKColors.Transparent,
            Style = SKPaintStyle.Fill,
            IsStroke = false,
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };

        /// <summary>Current drawing points.</summary>
        List<SKPoint> _vertexes = new List<SKPoint>();

        /// <summary>General purpose stack</summary>
        Stack<SKMatrix> _matrixStack = new Stack<SKMatrix>();

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
        public NpScript()
        {
            ResetVars();
        }

        /// <summary>
        /// Ugly way to deal with static vars in color class. TODO fix this.
        /// </summary>
        public void ResetVars()
        {
            Script.color.ResetMode();
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
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
