using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.ScriptCompiler;


namespace NProcessing.App
{
    public class Compiler : ScriptCompilerCore
    {
        /// <inheritdoc />
        public override void PreCompile()
        {
            SystemDlls.Add("System.Drawing");
            LocalDlls = new() { "NAudio", "SkiaSharp", "Ephemera.NBagOfTricks", "NProcessing.Script" };
        }

        /// <inheritdoc />
        public override void PostCompile()
        {
            // Nothing to do
        }

        /// <inheritdoc />
        public override bool PreprocessLine(string sline, FileContext pcont)
        {
            // Nothing to do
            return false;
        }
    }
}
