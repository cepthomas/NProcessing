using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Ephemera.NBagOfTricks;


namespace NProcessing.App
{
    public class Compiler : CompilerCore
    {
        /// <inheritdoc />
        public override void PreCompile()
        {
            SystemDlls.Add("System.Drawing");
            LocalDlls = ["NAudio", "SkiaSharp", "Ephemera.NBagOfTricks", "NProcessing.Script"];
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
