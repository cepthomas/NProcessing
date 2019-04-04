using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.ComponentModel;


namespace NProcessing.Script
{
    /// <summary>
    /// Static utility functions.
    /// </summary>
    public static class Utils
    {
        #region System utils
        /// <summary>General purpose decoder for keys. Because windows makes it kind of difficult.</summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns>Tuple of Converted char (0 if not convertible) and keyCode(s).</returns>
        // public static (char ch, List<Keys> keyCodes) KeyToChar(Keys key, Keys modifiers)
        // {
        //     char ch = (char)0;
        //     List<Keys> keyCodes = new List<Keys>();

        //     bool shift = modifiers.HasFlag(Keys.Shift);
        //     bool iscap = (Console.CapsLock && !shift) || (!Console.CapsLock && shift);

        //     // Check modifiers.
        //     if (modifiers.HasFlag(Keys.Control)) keyCodes.Add(Keys.Control);
        //     if (modifiers.HasFlag(Keys.Alt)) keyCodes.Add(Keys.Alt);
        //     if (modifiers.HasFlag(Keys.Shift)) keyCodes.Add(Keys.Shift);

        //     switch (key)
        //     {
        //         case Keys.Enter: ch = '\n'; break;
        //         case Keys.Tab: ch = '\t'; break;
        //         case Keys.Space: ch = ' '; break;
        //         case Keys.Back: ch = (char)8; break;
        //         case Keys.Escape: ch = (char)27; break;
        //         case Keys.Delete: ch = (char)127; break;

        //         case Keys.Left: keyCodes.Add(Keys.Left); break;
        //         case Keys.Right: keyCodes.Add(Keys.Right); break;
        //         case Keys.Up: keyCodes.Add(Keys.Up); break;
        //         case Keys.Down: keyCodes.Add(Keys.Down); break;

        //         case Keys.D0: ch = shift ? ')' : '0'; break;
        //         case Keys.D1: ch = shift ? '!' : '1'; break;
        //         case Keys.D2: ch = shift ? '@' : '2'; break;
        //         case Keys.D3: ch = shift ? '#' : '3'; break;
        //         case Keys.D4: ch = shift ? '$' : '4'; break;
        //         case Keys.D5: ch = shift ? '%' : '5'; break;
        //         case Keys.D6: ch = shift ? '^' : '6'; break;
        //         case Keys.D7: ch = shift ? '&' : '7'; break;
        //         case Keys.D8: ch = shift ? '*' : '8'; break;
        //         case Keys.D9: ch = shift ? '(' : '9'; break;

        //         case Keys.Oemplus: ch = shift ? '+' : '='; break;
        //         case Keys.OemMinus: ch = shift ? '_' : '-'; break;
        //         case Keys.OemQuestion: ch = shift ? '?' : '/'; break;
        //         case Keys.Oemcomma: ch = shift ? '<' : ','; break;
        //         case Keys.OemPeriod: ch = shift ? '>' : '.'; break;
        //         case Keys.OemQuotes: ch = shift ? '\"' : '\''; break;
        //         case Keys.OemSemicolon: ch = shift ? ':' : ';'; break;
        //         case Keys.OemPipe: ch = shift ? '|' : '\\'; break;
        //         case Keys.OemCloseBrackets: ch = shift ? '}' : ']'; break;
        //         case Keys.OemOpenBrackets: ch = shift ? '{' : '['; break;
        //         case Keys.Oemtilde: ch = shift ? '~' : '`'; break;

        //         case Keys.NumPad0: ch = '0'; break;
        //         case Keys.NumPad1: ch = '1'; break;
        //         case Keys.NumPad2: ch = '2'; break;
        //         case Keys.NumPad3: ch = '3'; break;
        //         case Keys.NumPad4: ch = '4'; break;
        //         case Keys.NumPad5: ch = '5'; break;
        //         case Keys.NumPad6: ch = '6'; break;
        //         case Keys.NumPad7: ch = '7'; break;
        //         case Keys.NumPad8: ch = '8'; break;
        //         case Keys.NumPad9: ch = '9'; break;
        //         case Keys.Subtract: ch = '-'; break;
        //         case Keys.Add: ch = '+'; break;
        //         case Keys.Decimal: ch = '.'; break;
        //         case Keys.Divide: ch = '/'; break;
        //         case Keys.Multiply: ch = '*'; break;

        //         default:
        //             if (key >= Keys.A && key <= Keys.Z)
        //             {
        //                 // UC is 65-90  LC is 97-122
        //                 ch = iscap ? ch = (char)(int)key : (char)(int)(key + 32);
        //             }
        //             break;
        //     }

        //     return (ch, keyCodes);
        // }

        /// <summary>Rudimentary C# source code formatter to make generated files somewhat readable.</summary>
        /// <param name="src">Lines to prettify.</param>
        /// <returns>Formatted lines.</returns>
        // public static List<string> FormatSourceCode(List<string> src)
        // {
        //     List<string> fmt = new List<string>();
        //     int indent = 0;

        //     src.ForEach(s =>
        //     {
        //         if (s.StartsWith("{"))
        //         {
        //             fmt.Add(new string(' ', indent * 4) + s);
        //             indent++;
        //         }
        //         else if (s.StartsWith("}") && indent > 0)
        //         {
        //             indent--;
        //             fmt.Add(new string(' ', indent * 4) + s);
        //         }
        //         else
        //         {
        //             fmt.Add(new string(' ', indent * 4) + s);
        //         }
        //     });

        //     return fmt;
        // }
        #endregion

        #region Math helpers
        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        // public static double DegreesToRadians(double angle)
        // {
        //     return Math.PI * angle / 180.0;
        // }

        /// <summary>Conversion.</summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        // public static double RadiansToDegrees(double angle)
        // {
        //     return angle * 180.0 / Math.PI;
        // }

        /// <summary>
        /// Remap a value to new coordinates.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="start1"></param>
        /// <param name="stop1"></param>
        /// <param name="start2"></param>
        /// <param name="stop2"></param>
        /// <returns></returns>
        // public static double Map(double val, double start1, double stop1, double start2, double stop2)
        // {
        //     return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        // }

        /// <summary>
        /// Remap a value to new coordinates.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="start1"></param>
        /// <param name="stop1"></param>
        /// <param name="start2"></param>
        /// <param name="stop2"></param>
        /// <returns></returns>
        // public static int Map(int val, int start1, int stop1, int start2, int stop2)
        // {
        //     return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1);
        // }

        /// <summary>
        /// Bounds limits a value.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        // public static double Constrain(double val, double min, double max)
        // {
        //     val = Math.Max(val, min);
        //     val = Math.Min(val, max);
        //     return val;
        // }

        /// <summary>
        /// Bounds limits a value.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        // public static int Constrain(int val, int min, int max)
        // {
        //     val = Math.Max(val, min);
        //     val = Math.Min(val, max);
        //     return val;
        // }
        #endregion

        #region Extensions
        /// <summary>
        /// Splits a tokenized (delimited) string into its parts and optionally trims whitespace.
        /// </summary>
        /// <param name="text">The string to split up.</param>
        /// <param name="tokens">The char token(s) to split by.</param>
        /// <param name="trim">Remove whitespace at each end.</param>
        /// <returns>Return the parts as a list.</returns>
        // public static List<string> SplitByTokens(this string text, string tokens, bool trim = true)
        // {
        //     var ret = new List<string>();
        //     var list = text.Split(tokens.ToCharArray(), trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        //     list.ForEach(s => ret.Add(trim ? s.Trim() : s));
        //     return ret;
        // }

        /// <summary>
        /// Splits a tokenized (delimited) string into its parts and optionally trims whitespace.
        /// </summary>
        /// <param name="text">The string to split up.</param>
        /// <param name="splitby">The string to split by.</param>
        /// <param name="trim">Remove whitespace at each end.</param>
        /// <returns>Return the parts as a list.</returns>
        // public static List<string> SplitByToken(this string text, string splitby, bool trim = true)
        // {
        //     var ret = new List<string>();
        //     var list = text.Split(new string[] { splitby }, trim ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        //     list.ForEach(s => ret.Add(trim ? s.Trim() : s));
        //     return ret;
        // }
        #endregion

        // #region Extensions borrowed from MoreLinq
        // /// <summary>
        // /// Immediately executes the given action on each element in the source sequence.
        // /// </summary>
        // /// <typeparam name="T">The type of the elements in the sequence</typeparam>
        // /// <param name="source">The sequence of elements</param>
        // /// <param name="action">The action to execute on each element</param>
        // public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        // {
        //     if (source == null) throw new ArgumentNullException(nameof(source));
        //     if (action == null) throw new ArgumentNullException(nameof(action));

        //     foreach (var element in source)
        //         action(element);
        // }

        // /// <summary>
        // /// Immediately executes the given action on each element in the source sequence.
        // /// Each element's index is used in the logic of the action.
        // /// </summary>
        // /// <typeparam name="T">The type of the elements in the sequence</typeparam>
        // /// <param name="source">The sequence of elements</param>
        // /// <param name="action">The action to execute on each element; the second parameter
        // /// of the action represents the index of the source element.</param>
        // public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        // {
        //     if (source == null) throw new ArgumentNullException(nameof(source));
        //     if (action == null) throw new ArgumentNullException(nameof(action));

        //     var index = 0;
        //     foreach (var element in source)
        //         action(element, index++);
        // }
        // #endregion
    }
}
