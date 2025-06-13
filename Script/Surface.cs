using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SkiaSharp;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace NProcessing.Script
{
    public partial class Surface : Form
    {
        #region Fields
        /// <summary>The current script.</summary>
        ScriptBase? _script = null;

        /// <summary>Rendered bitmap for display when painting.</summary>
        System.Drawing.Bitmap? _bitmap = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Surface()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            UpdateStyles();

            // Intercept all keyboard events.
            KeyPreview = true;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Update per new script object.
        /// If there are user script exceptions they will bubble up to the MainForm.
        /// </summary>
        /// <param name="script"></param>
        public void InitSurface(ScriptBase script)
        {
            if(script is not null)
            {
                _script = script;
                ClientSize = new System.Drawing.Size(_script.width, _script.height);
                _script.focused = Focused;
                _script.frameCount = 0;
                _script.setup();
            }
        }

        /// <summary>
        /// Redraw if it's time and enabled.
        /// </summary>
        public void UpdateSurface()
        {
            if (_script is not null && (_script._loop || _script._redraw))
            {
                // Check for resize or init.
                if(_bitmap is null || _bitmap.Width != _script.width || _bitmap.Height != _script.height)
                {
                    if (_bitmap is not null)
                    {
                        _bitmap.Dispose();
                        _bitmap = null;
                    }

                    // Update client side.
                    _bitmap = new System.Drawing.Bitmap(_script.width, _script.height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    ClientSize = new System.Drawing.Size(_script.width, _script.height);
                }

                Render();
                Invalidate();
            }
        }
        #endregion

        #region Painting
        /// <summary>
        /// Calls the script code that generates the bmp to paint later.
        /// If there are user script exceptions they will bubble up to the MainForm.
        /// </summary>
        void Render()
        {
            if (_script is not null && _bitmap is not null)
            {
                var w = _script.width; // Width;
                var h = _script.height; // Height;

                var data = _bitmap.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, _bitmap.PixelFormat);

                using (SKSurface skSurface = SKSurface.Create(new SKImageInfo(w, h, SKImageInfo.PlatformColorType, SKAlphaType.Premul), data.Scan0, w * 4))
                {
                    // Hand over to the script for drawing on.
                    _script._canvas = skSurface.Canvas;

                    // Some housekeeping.
                    _script.pMouseX = _script.mouseX;
                    _script.pMouseY = _script.mouseY;
                    _script._redraw = false;

                    // Execute the user script code.
                    _script.frameCount++;
                    _script.draw();
                }

                _bitmap.UnlockBits(data);
            }
        }

        /// <summary>
        /// Renders the stored bitmap to the UI.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_bitmap is not null)
            {
                e.Graphics.DrawImage(_bitmap, new System.Drawing.Point(0, 0));
            }
        }
        #endregion

        #region Mouse handling
        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_script is not null)
            {
                ProcessMouseEvent(e);
                _script.mouseIsPressed = true;
                _script.mousePressed();
            }
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_script is not null)
            {
                ProcessMouseEvent(e);
                _script.mouseIsPressed = false;
                _script.mouseReleased();
            }
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_script is not null)
            {
                ProcessMouseEvent(e);
                if (_script.mouseIsPressed)
                {
                    _script.mouseDragged();
                }
                else
                {
                    _script.mouseMoved();
                }
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_script is not null)
            {
                ProcessMouseEvent(e);
                _script.mouseClicked();
            }
            base.OnMouseClick(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_script is not null)
            {
                ProcessMouseEvent(e);
                _script.mouseWheel();
            }
            base.OnMouseWheel(e);
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            if (_script is not null)
            {
                _script.focused = Focused;
            }
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            if (_script is not null)
            {
                _script.focused = Focused;
            }
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Common routine to update mouse stuff.
        /// </summary>
        /// <param name="e"></param>
        void ProcessMouseEvent(MouseEventArgs e)
        {
            if (_script is not null)
            {
                _script.mouseX = e.X;
                _script.mouseY = e.Y;
                _script.mouseWheelValue = e.Delta;

                _script.mouseButton = e.Button switch
                {
                    MouseButtons.Left => ScriptBase.LEFT,
                    MouseButtons.Right => ScriptBase.RIGHT,
                    MouseButtons.Middle => ScriptBase.CENTER,
                    _ => 0,
                };
            }
        }
        #endregion

        #region Keyboard handling
        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_script is not null)
            {
                _script.keyIsPressed = false;

                // Decode character, maybe.
                var v = KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyIsPressed = true;
                    // Notify client.
                    _script.keyPressed();
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_script is not null)
            {
                _script.keyIsPressed = false;

                // Decode character, maybe.
                var v = KeyToChar(e.KeyCode, e.Modifiers);
                ProcessKeys(v);

                if (_script.key != 0)
                {
                    // Valid character.
                    _script.keyIsPressed = false;
                    // Notify client.
                    _script.keyReleased();
                    // Now reset keys.
                    _script.key = (char)0;
                    _script.keyCode = 0;
                }
            }
            base.OnKeyUp(e);
        }

        /// <summary>
        /// Event handler for keys.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (_script is not null)
            {
                _script.key = e.KeyChar;
                _script.keyTyped();
            }
            base.OnKeyPress(e);
        }

        /// <summary>General purpose decoder for keys. Because windows makes it kind of difficult.</summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns>Tuple of Converted char (0 if not convertible) and keyCode(s).</returns>
        (char ch, List<Keys> keyCodes) KeyToChar(Keys key, Keys modifiers)
        {
            char ch = (char)0;
            List<Keys> keyCodes = new();

            bool shift = modifiers.HasFlag(Keys.Shift);
            bool iscap = (Console.CapsLock && !shift) || (!Console.CapsLock && shift);

            // Check modifiers.
            if (modifiers.HasFlag(Keys.Control)) keyCodes.Add(Keys.Control);
            if (modifiers.HasFlag(Keys.Alt)) keyCodes.Add(Keys.Alt);
            if (modifiers.HasFlag(Keys.Shift)) keyCodes.Add(Keys.Shift);

            switch (key)
            {
                case Keys.Enter: ch = '\n'; break;
                case Keys.Tab: ch = '\t'; break;
                case Keys.Space: ch = ' '; break;
                case Keys.Back: ch = (char)8; break;
                case Keys.Escape: ch = (char)27; break;
                case Keys.Delete: ch = (char)127; break;

                case Keys.Left: keyCodes.Add(Keys.Left); break;
                case Keys.Right: keyCodes.Add(Keys.Right); break;
                case Keys.Up: keyCodes.Add(Keys.Up); break;
                case Keys.Down: keyCodes.Add(Keys.Down); break;

                case Keys.D0: ch = shift ? ')' : '0'; break;
                case Keys.D1: ch = shift ? '!' : '1'; break;
                case Keys.D2: ch = shift ? '@' : '2'; break;
                case Keys.D3: ch = shift ? '#' : '3'; break;
                case Keys.D4: ch = shift ? '$' : '4'; break;
                case Keys.D5: ch = shift ? '%' : '5'; break;
                case Keys.D6: ch = shift ? '^' : '6'; break;
                case Keys.D7: ch = shift ? '&' : '7'; break;
                case Keys.D8: ch = shift ? '*' : '8'; break;
                case Keys.D9: ch = shift ? '(' : '9'; break;

                case Keys.Oemplus: ch = shift ? '+' : '='; break;
                case Keys.OemMinus: ch = shift ? '_' : '-'; break;
                case Keys.OemQuestion: ch = shift ? '?' : '/'; break;
                case Keys.Oemcomma: ch = shift ? '<' : ','; break;
                case Keys.OemPeriod: ch = shift ? '>' : '.'; break;
                case Keys.OemQuotes: ch = shift ? '\"' : '\''; break;
                case Keys.OemSemicolon: ch = shift ? ':' : ';'; break;
                case Keys.OemPipe: ch = shift ? '|' : '\\'; break;
                case Keys.OemCloseBrackets: ch = shift ? '}' : ']'; break;
                case Keys.OemOpenBrackets: ch = shift ? '{' : '['; break;
                case Keys.Oemtilde: ch = shift ? '~' : '`'; break;

                case Keys.NumPad0: ch = '0'; break;
                case Keys.NumPad1: ch = '1'; break;
                case Keys.NumPad2: ch = '2'; break;
                case Keys.NumPad3: ch = '3'; break;
                case Keys.NumPad4: ch = '4'; break;
                case Keys.NumPad5: ch = '5'; break;
                case Keys.NumPad6: ch = '6'; break;
                case Keys.NumPad7: ch = '7'; break;
                case Keys.NumPad8: ch = '8'; break;
                case Keys.NumPad9: ch = '9'; break;
                case Keys.Subtract: ch = '-'; break;
                case Keys.Add: ch = '+'; break;
                case Keys.Decimal: ch = '.'; break;
                case Keys.Divide: ch = '/'; break;
                case Keys.Multiply: ch = '*'; break;

                default:
                    if (key >= Keys.A && key <= Keys.Z)
                    {
                        // UC is 65-90  LC is 97-122
                        ch = iscap ? (char)(int)key : (char)(int)(key + 32);
                    }
                    break;
            }

            return (ch, keyCodes);
        }


        /// <summary>
        /// Convert generic utility output to flavor that Processing understands.
        /// </summary>
        /// <param name="keys"></param>
        void ProcessKeys((char ch, List<Keys> keyCodes) keys)
        {
            if (_script is null)
            {
                return; // early return!
            }
             
            _script.keyCode = 0;
            _script.key = keys.ch;

            // Check modifiers.
            if (keys.keyCodes.Contains(Keys.Control))
            {
                _script.keyCode |= ScriptBase.CTRL;
            }

            if (keys.keyCodes.Contains(Keys.Alt))
            {
                _script.keyCode |= ScriptBase.ALT;
            }

            if (keys.keyCodes.Contains(Keys.Shift))
            {
                _script.keyCode |= ScriptBase.SHIFT;
            }

            if (keys.keyCodes.Contains(Keys.Left))
            {
                _script.keyCode |= ScriptBase.LEFT;
                _script.key = (char)ScriptBase.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Right))
            {
                _script.keyCode |= ScriptBase.RIGHT;
                _script.key = (char)ScriptBase.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Up))
            {
                _script.keyCode |= ScriptBase.UP;
                _script.key = (char)ScriptBase.CODED;
            }

            if (keys.keyCodes.Contains(Keys.Down))
            {
                _script.keyCode |= ScriptBase.DOWN;
                _script.key = (char)ScriptBase.CODED;
            }
        }
        #endregion
    }
}
