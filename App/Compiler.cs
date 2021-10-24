using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using NBagOfTricks;
using ScriptCompiler;


namespace NProcessing.App
{
    public class Compiler : ScriptCompilerCore
    {
        /// <inheritdoc />
        public override void PreExecute()
        {
            LocalDlls = new() { "NAudio", "NLog", "NBagOfTricks", "NProcessing.Script" };
            Usings = new() { "System.Collections.Generic" };
        }

        // /// <inheritdoc />
        // public override void PostExecute()
        // {
        //     // Nothing to do.
        // }

        // /// <inheritdoc />
        // public override bool PreprocessFile(string sline, FileContext pcont)
        // {
        //     // Nothing to do.
        // }
    }
}
