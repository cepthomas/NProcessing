using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using NLog;
using NBagOfTricks;
using NProcessing.Script;


namespace NProcessing.App
{
    /// <summary>
    /// Parses/compiles *.np file(s).
    /// </summary>
    public class Compiler
    {
        /// <summary>
        /// Parser helper class.
        /// </summary>
        class FileContext
        {
            /// <summary>Current source file.</summary>
            public string SourceFile { get; set; } = "???";

            /// <summary>Current source line.</summary>
            public int LineNumber { get; set; } = 1;

            /// <summary>Current parse state.</summary>
            public string State { get; set; } = "idle";

            /// <summary>Accumulated script code lines.</summary>
            public List<string> CodeLines { get; set; } = new List<string>();
        }

        #region Properties
        /// <summary>Accumulated errors.</summary>
        public List<CompileResult> Errors { get; } = new List<CompileResult>();

        /// <summary>All active source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return _filesToCompile.Values.Select(f => f.SourceFile).ToList(); } }

        /// <summary>Specifies the temp dir used so client can track down runtime errors.</summary>
        public string TempDir { get; set; } = "";

        /// <summary>Do not include some neb only components.</summary>
        public bool Min { get; set; } = true;
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Starting directory.</summary>
        string _baseDir = "???";

        /// <summary>Script info.</summary>
        string _scriptName = "???";

        /// <summary>Accumulated lines to go in the constructor.</summary>
        List<string> _initLines = new List<string>();

        /// <summary>Products of file process. Key is generated file name.</summary>
        Dictionary<string, FileContext> _filesToCompile = new Dictionary<string, FileContext>();
        #endregion

        #region Public functions
        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="npfn">Fully qualified path to topmost file.</param>
        /// <returns>The newly minted script object or null if failed.</returns>
        public ScriptBase Execute(string npfn)
        {
            ScriptBase script = null;

            // Reset everything.
            _filesToCompile.Clear();
            _initLines.Clear();

            Errors.Clear();

            if (File.Exists(npfn))
            {
                _logger.Info($"Compiling {npfn}.");

                ///// Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(npfn);
                StringBuilder sb = new StringBuilder();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                _baseDir = Path.GetDirectoryName(npfn);

                ///// Compile.
                DateTime startTime = DateTime.Now; // for metrics
                Parse(npfn);
                script = Compile();
                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {npfn}.");
            }

            return Errors.Count == 0 ? script : null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Top level parser.
        /// </summary>
        /// <param name="npfn">Topmost file in collection.</param>
        void Parse(string npfn)
        {
            // Start parsing from the main file. ParseOneFile is a recursive function.
            FileContext pcont = new FileContext()
            {
                SourceFile = npfn,
                LineNumber = 1
            };
            ParseOneFile(pcont);

            // Add the generated internal code files.
            _filesToCompile.Add($"{_scriptName}_wrapper.cs", new FileContext()
            {
                SourceFile = "",
                CodeLines = GenMainFileContents()
            });
        }

        /// <summary>
        /// Parse one file. This is recursive to support nested #import.
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool ParseOneFile(FileContext pcont)
        {
            bool valid = false;

            // Try fully qualified.
            if (File.Exists(pcont.SourceFile))
            {
                // OK - leave as is.
                valid = true;
            }
            else // Try relative.
            {
                string fn = Path.Combine(_baseDir, pcont.SourceFile);
                if (File.Exists(fn))
                {
                    pcont.SourceFile = fn;
                    valid = true;
                }
            }

            if (valid)
            {
                string genFn = $"{_scriptName}_src{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                List<string> sourceLines = new List<string>(File.ReadAllLines(pcont.SourceFile));

                // Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                {
                    string s = sourceLines[pcont.LineNumber - 1];

                    // Remove any comments.
                    int pos = s.IndexOf("//");
                    string cline = pos >= 0 ? s.Substring(0, pos) : s;

                    // Test for nested files
                    // #import "path\name.np"
                    // #import "include path\split file name.np"
                    if(s.StartsWith("#import"))
                    {
                        List<string> parts = s.SplitByTokens("\";");
                        string fn = parts.Last();

                        // Recursive call to parse this file
                        FileContext subcont = new FileContext()
                        {
                            SourceFile = fn,
                            LineNumber = 1
                        };

                        if (!ParseOneFile(subcont))
                        {
                            Errors.Add(new CompileResult()
                            {
                                ResultType = CompileResultType.Error,
                                Message = $"Invalid #import: {fn}",
                                SourceFile = pcont.SourceFile,
                                LineNumber = pcont.LineNumber
                            });
                        }
                    }
                    else
                    {
                        if (cline != "")
                        {
                            // Store the whole line with line number tacked on. This is easier than trying to maintain a bunch of source<>compiled mappings.
                            pcont.CodeLines.Add($"        {cline} //{pcont.LineNumber}");
                        }
                    }
                }

                // Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }


        /// <summary>
        /// Run the Compiler.
        /// </summary>
        /// <param name="nebfn">Fully qualified path to main file.</param>
        public void Execute(string nebfn)
        {
            // Reset everything.
            Script = new();
            Channels.Clear();
            Results.Clear();
            _filesToCompile.Clear();
            _initLines.Clear();

            if (nebfn != Definitions.UNKNOWN_STRING && File.Exists(nebfn))
            {
                _logger.Info($"Compiling {nebfn}.");

                ///// Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(nebfn);
                StringBuilder sb = new();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                var dir = Path.GetDirectoryName(nebfn);

                ///// Compile.
                DateTime startTime = DateTime.Now; // for metrics

                ///// Process the source files into something that can be compiled. ParseOneFile is a recursive function.
                FileContext pcont = new()
                {
                    SourceFile = nebfn,
                    LineNumber = 1
                };
                PreprocessFile(pcont);

                ///// Check for changed channel descriptors.
                if (string.Join("", _channelDescriptors).GetHashCode() != chdesc)
                {
                    Channels = ProcessChannelDescs();
                }

                ///// Compile the processed files.
                Script = CompileNative(dir!);

                _logger.Info($"Compile took {(DateTime.Now - startTime).Milliseconds} msec.");
            }
            else
            {
                _logger.Error($"Invalid file {nebfn}.");
            }

            int errorCount = Results.Where(r => r.ResultType == CompileResultType.Error || r.ResultType == CompileResultType.Fatal).Count();
            Script.Valid = errorCount == 0;
        }




        /// <summary>
        /// The actual compiler driver.
        /// </summary>
        /// <returns>Compiled script</returns>
        ScriptBase CompileNative(string baseDir)
        {
            ScriptBase script = new();

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                ///// Assemble constituents.
                List<SyntaxTree> trees = new();

                // Write the generated source files to temp build area.
                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, ci.CodeLines);

                    // Build a syntax tree.
                    string code = File.ReadAllText(fullpath);
                    CSharpParseOptions popts = new();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, genFn);
                    trees.Add(tree);
                }

                //3. We now build up a list of references needed to compile the code.
                var references = new List<MetadataReference>();
                // System stuff location.
                var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);
                // Project refs like nuget.
                var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // System dlls.
                foreach (var dll in new[] { "System", "System.Core", "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Drawing", "System.Linq" })
                {
                    references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll")));
                }

                // Local dlls.
                foreach (var dll in new[] { "NAudio", "NLog", "NBagOfTricks", "NebOsc", "Nebulator.Common", "Nebulator.Script" })
                {
                    references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll")));
                }

                ///// Emit to stream
                var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);
                var ms = new MemoryStream();
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    //Load into currently running assembly. Normally we'd probably want to do this in an AppDomain
                    var assy = Assembly.Load(ms.ToArray());
                    foreach (Type t in assy.GetTypes())
                    {
                        if (t.BaseType != null && t.BaseType.Name == "ScriptBase")
                        {
                            // We have a good script file. Create the executable object.
                            object? o = Activator.CreateInstance(t);
                            if (o is not null)
                            {
                                script = (ScriptBase)o;
                            }
                        }
                    }
                }

                ///// Results.
                // List<string> diags = new();
                // result.Diagnostics.ForEach(d => diags.Add(d.ToString()));
                // File.WriteAllLines(@"C:\Dev\repos\Nebulator\Examples\temp\compiler.txt", diags);
                foreach (var diag in result.Diagnostics)
                {
                    CompileResult se = new();
                    se.Message = diag.GetMessage();
                    bool keep = true;

                    switch (diag.Severity)
                    {
                        case DiagnosticSeverity.Error:
                            se.ResultType = CompileResultType.Error;
                            break;

                        case DiagnosticSeverity.Warning:
                            if (UserSettings.TheSettings.IgnoreWarnings)
                            {
                                keep = false;
                            }
                            else
                            {
                                se.ResultType = CompileResultType.Warning;
                            }
                            break;

                        case DiagnosticSeverity.Info:
                            se.ResultType = CompileResultType.Info;
                            break;

                        case DiagnosticSeverity.Hidden:
                            if (UserSettings.TheSettings.IgnoreWarnings)
                            {
                                keep = false;
                            }
                            else
                            {
                                //?? se.ResultType = CompileResultType.Warning;
                            }
                            break;
                    }

                    var genFileName = diag.Location.SourceTree!.FilePath;
                    var genLineNum = diag.Location.GetLineSpan().StartLinePosition.Line; // 0-based

                    // Get the original info.
                    if (_filesToCompile.TryGetValue(Path.GetFileName(genFileName), out var context))
                    {
                        se.SourceFile = context.SourceFile;
                        // Dig out the original line number.
                        string origLine = context.CodeLines[genLineNum];
                        int ind = origLine.LastIndexOf("//");
                        if (ind != -1)
                        {
                            se.LineNumber = int.TryParse(origLine[(ind + 2)..], out int origLineNum) ? origLineNum : -1; // 1-based
                        }
                    }
                    else
                    {
                        // Presumably internal generated file - should never have errors.
                        se.SourceFile = "NoSourceFile";
                    }

                    if (keep)
                    {
                        Results.Add(se);
                    }
                }
            }
            catch (Exception ex)
            {
                Results.Add(new CompileResult()
                {
                    ResultType = CompileResultType.Fatal,
                    Message = "Exception: " + ex.Message,
                });
            }

            return script;
        }

        /// <summary>
        /// Parse one file. Recursive to support nested include(fn).
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool PreprocessFile(FileContext pcont)
        {
            bool valid = File.Exists(pcont.SourceFile);

            if (valid)
            {
                string genFn = $"{_scriptName}_src{_filesToCompile.Count}.cs".ToLower();
                _filesToCompile.Add(genFn, pcont);

                ///// Preamble.
                pcont.CodeLines.AddRange(GenTopOfFile(pcont.SourceFile));

                ///// The content.
                List<string> sourceLines = new(File.ReadAllLines(pcont.SourceFile));

                for (pcont.LineNumber = 1; pcont.LineNumber <= sourceLines.Count; pcont.LineNumber++)
                {
                    string s = sourceLines[pcont.LineNumber - 1];

                    // Remove any comments. Single line type only.
                    int pos = s.IndexOf("//");
                    string cline = pos >= 0 ? s.Left(pos) : s;

                    // Test for preprocessor directives.
                    string strim = s.Trim();

                    //Include("path\name.neb");
                    if (strim.StartsWith("Include"))
                    {
                        // Exclude from output file.
                        List<string> parts = strim.SplitByTokens("\"");
                        if (parts.Count == 3)
                        {
                            string fn = Path.Combine(UserSettings.TheSettings.WorkPath, parts[1]);

                            // Recursive call to parse this file
                            FileContext subcont = new()
                            {
                                SourceFile = fn,
                                LineNumber = 1
                            };
                            valid = PreprocessFile(subcont);
                        }

                        if (!valid)
                        {
                            Results.Add(new CompileResult()
                            {
                                ResultType = CompileResultType.Error,
                                Message = $"Invalid Include: {strim}",
                                SourceFile = pcont.SourceFile,
                                LineNumber = pcont.LineNumber
                            });
                        }
                    }
                    else if (strim.StartsWith("Channel"))
                    {
                        // Exclude from output file.
                        _channelDescriptors.Add(strim);
                    }
                    else // plain line
                    {
                        if (cline.Trim() != "")
                        {
                            // Store the whole line with line number tacked on and some indentation.
                            pcont.CodeLines.Add($"        {cline} //{pcont.LineNumber}");
                        }
                    }
                }

                ///// Postamble.
                pcont.CodeLines.AddRange(GenBottomOfFile());
            }

            return valid;
        }



        /// <summary>
        /// Top level compiler.
        /// </summary>
        /// <returns>Compiled script</returns>
        ScriptBase Compile_not()
        {
            ScriptBase script = null;

            try // many ways to go wrong...
            {
                // Set the compiler parameters.
                CompilerParameters cp = new CompilerParameters()
                {
                    GenerateExecutable = false,
                    //OutputAssembly = _scriptName, -- don't do this!
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = false,
                    IncludeDebugInformation = true
                };

                // The usual suspects.
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Core.dll");
                cp.ReferencedAssemblies.Add("System.Drawing.dll");
                cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                cp.ReferencedAssemblies.Add("System.Data.dll");
                cp.ReferencedAssemblies.Add("SkiaSharp.dll");
                cp.ReferencedAssemblies.Add("NBagOfTricks.dll");
                cp.ReferencedAssemblies.Add("NProcessing.Script.dll");

                // Add the generated source files.
                List<string> paths = new List<string>();

                // Create output area.
                TempDir = Path.Combine(_baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                foreach (string genFn in _filesToCompile.Keys)
                {
                    FileContext ci = _filesToCompile[genFn];
                    string fullpath = Path.Combine(TempDir, genFn);
                    File.Delete(fullpath);
                    File.WriteAllLines(fullpath, ci.CodeLines);
                    paths.Add(fullpath);
                }

                // Make it compile.
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, paths.ToArray());

                if (cr.Errors.Count == 0)
                {
                    Assembly assy = cr.CompiledAssembly;

                    // Bind to the script interface.
                    foreach (Type t in assy.GetTypes())
                    {
                        if (t.BaseType is not null && t.BaseType.Name == "ScriptBase")
                        {
                            // We have a good script file. Create the executable object.
                            Object o = Activator.CreateInstance(t);
                            script = o as ScriptBase;
                        }
                    }

                    if (script is null)
                    {
                        throw new Exception("Could not instantiate script");
                    }
                }
                else
                {
                    foreach (CompilerError err in cr.Errors)
                    {
                        // The line should end with source line number: "//1234"
                        int origLineNum = 0; // defaults
                        string origFileName = "???";

                        // Dig out the offending source code information.
                        string fpath = Path.GetFileName(err.FileName.ToLower());
                        if (_filesToCompile.ContainsKey(fpath))
                        {
                            FileContext ci = _filesToCompile[fpath];
                            origFileName = ci.SourceFile;
                            string origLine = ci.CodeLines[err.Line - 1];
                            int ind = origLine.LastIndexOf("//");

                            if (origFileName == "" || ind == -1)
                            {
                                // Must be an internal error. Do the best we can.
                                Errors.Add(new CompileResult()
                                {
                                    ResultType = err.IsWarning ? CompileResultType.Warning : CompileResultType.Error,
                                    SourceFile = err.FileName,
                                    LineNumber = err.Line,
                                    Message = $"InternalError: {err.ErrorText} in: {origLine}"
                                });
                            }
                            else
                            {
                                if(int.TryParse(origLine.Substring(ind + 2), out origLineNum))
                                {
                                    Errors.Add(new CompileResult()
                                    {
                                        ResultType = err.IsWarning ? CompileResultType.Warning : CompileResultType.Error,
                                        SourceFile = origFileName,
                                        LineNumber = origLineNum,
                                        Message = err.ErrorText
                                    });
                                }
                            }
                        }
                        else
                        {
                            Errors.Add(new CompileResult()
                            {
                                ResultType = err.IsWarning ? CompileResultType.Warning : CompileResultType.Error,
                                SourceFile = "NoSourceFile",
                                LineNumber = -1,
                                Message = err.ErrorText
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new CompileResult()
                {
                    ResultType = CompileResultType.Error,
                    Message = "Exception: " + ex.Message,
                    SourceFile = "",
                    LineNumber = 0
                });
            }

            return script;
        }

        /// <summary>
        /// Create the file containing all the glue.
        /// </summary>
        /// <returns></returns>
        List<string> GenMainFileContents()
        {
            // Create the main/generated file. Indicated by empty source file name.
            List<string> codeLines = GenTopOfFile("");

            // Collected init stuff goes in a constructor.
            // Reference to current script so nested classes have access to it. Processing uses java which would not require this minor hack.
            codeLines.Add($"        protected static ScriptBase s;");
            codeLines.Add($"        public {_scriptName}() : base()");
            codeLines.Add( "        {");
            codeLines.Add( "            s = this;");
            _initLines.ForEach(l => codeLines.Add("            " + l));
            codeLines.Add( "        }");

            // Bottom stuff.
            codeLines.AddRange(GenBottomOfFile());

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file top stuff.
        /// </summary>
        /// <param name="fn">Source file name. Empty means it's an internal file.</param>
        /// <returns></returns>
        List<string> GenTopOfFile(string fn)
        {
            // Create the common contents.
            List<string> codeLines = new List<string>
            {
                $"//{fn}",
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Text;",
                "using System.Linq;",
                "using System.Drawing;",
                "using System.Windows.Forms;",
                "using SkiaSharp;",
                "using NBagOfTricks;",
                "using NProcessing;",
                "using NProcessing.Script;",
                "namespace NProcessing.UserScript",
                "{",
               $"    public partial class {_scriptName} : ScriptBase",
                "    {"
            };

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file bottom stuff.
        /// </summary>
        /// <returns></returns>
        List<string> GenBottomOfFile()
        {
            // Create the common contents.
            List<string> codeLines = new List<string>
            {
                "    }",
                "}"
            };

            return codeLines;
        }
        #endregion
    }

    /// <summary>General script error.</summary>
    public enum CompileResultType { None, Warning, Error, Runtime }

    /// <summary>General script error container.</summary>
    public class CompileResult
    {
        /// <summary>Where it came from.</summary>
        public CompileResultType ResultType { get; set; } = CompileResultType.None;

        /// <summary>Original source file.</summary>
        public string SourceFile { get; set; } = "???";

        /// <summary>Original source line number.</summary>
        public int LineNumber { get; set; } = 0;

        /// <summary>Content.</summary>
        public string Message { get; set; } = "???";

        /// <summary>Readable.</summary>
        public override string ToString() => $"{ResultType} {SourceFile}({LineNumber}): {Message}";
    }
}
