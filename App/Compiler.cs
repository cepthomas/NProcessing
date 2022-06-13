using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NBagOfTricks;
using NBagOfTricks.ScriptCompiler;


namespace NProcessing.App
{
    public class Compiler : ScriptCompilerCore
    {
        /// <inheritdoc />
        public override void PreExecute()
        {
            SystemDlls.Add("System.Drawing");
            LocalDlls = new() { "NAudio", "SkiaSharp", "NBagOfTricks", "NProcessing.Script" };
        }

        // Nothing to do:
        // public override void PostExecute()
        // public override bool PreprocessFile(string sline, FileContext pcont)
    }
}
