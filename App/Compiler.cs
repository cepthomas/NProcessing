using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;


namespace NProcessing.App
{
    public class Compiler : CompilerCore
    {
        /// <see cref="CompilerCore"/>
        protected override void PreCompile()
        {

            // App references.
            SystemDlls =
            [
                "System",
                "System.Private.CoreLib",
                "System.Runtime",
                "System.Collections",
                "System.Drawing"
            ];

            LocalDlls =
            [
                "SkiaSharp",
                "Ephemera.NBagOfTricks",
                "NProcessing.Script"
            ];

            Usings =
            [
                "System.Collections.Generic",
                "System.Text",
            ];

        }

        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
            // Nothing to do
        }

        /// <see cref="CompilerCore"/>
        protected override bool PreprocessLine(string sline, int lineNum, ScriptFile pcont)
        {
            // Nothing to do
            return false;
        }
    }
}
