using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Reflection;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using NProcessing.Script;
using Ephemera.NScript;


namespace NProcessing.App
{
    public partial class MainForm : Form
    {
        #region Enums
        /// <summary>Internal control status.</summary>
        enum PlayCommand { Start, Stop }
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Main");

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Surface child form.</summary>
        readonly Surface _surface = new();

        /// <summary>Our compiler.</summary>
        readonly Compiler _compiler;

        /// <summary>Current script file name.</summary>
        string? _scriptFileName;

        /// <summary>The current script.</summary>
        ScriptCore? _script;

        /// <summary>Frame rate in fps.</summary>
        int _frameRate = 30;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        // /// <summary>The temp dir for compile products.</summary>
        // string _compileTempDir = "";

        /// <summary>The user settings.</summary>
        readonly UserSettings _settings = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Get settings.
            string appDir = MiscUtils.GetAppDataDir("NProcessing", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Init logging.
            FileInfo fi = new(Path.Combine(appDir, "log.txt"));
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(fi.FullName, 100000);

            _compiler = new()
            {
                IgnoreWarnings = true,              // to taste
                Namespace = "NProcessing.Script",   // same as ScriptCore.cs
                BaseClassName = "ScriptCore",       // same as ScriptCore.cs
            };

            // Init UI
            toolStrip1.Renderer = new ToolStripCheckBoxRenderer() { SelectedColor = _settings.SelectedColor };

            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            WindowState = FormWindowState.Normal;

            _surface.Visible = true;
            _surface.Location = new Point(Right, Top);
            _surface.TopMost = _settings.LockUi;

            // The rest of the controls.
            textViewer.BackColor = BackColor;
            textViewer.MatchText.Add("ERR", Color.LightPink);
            textViewer.MatchText.Add("WRN", Color.Plum);
            textViewer.Prompt = "> ";

            PopulateRecentMenu();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChange += (_, __) => { this.InvokeIfRequired(_ => { SetCompileStatus(false); }); };

            Text = $"NProcessing {MiscUtils.GetVersionString()} - No file loaded";

            // Slow timer.
            timer1.Interval = 500;
            timer1.Start();
        }

        /// <summary>
        /// Post control creation.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _logger.Info("============================ Starting up ===========================");

            // Fast timer.
            SetUiTimerPeriod();
            _mmTimer.Start();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenScriptFile(args[1]);
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _mmTimer.Stop();
                LogManager.Stop();
                SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Something failed during shut down: {ex.Message}.");
            }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        { 
            if (disposing)
            {
                _mmTimer.Dispose();
                components?.Dispose();
            }

            // Wait a bit in case there are some lingering events.
            System.Threading.Thread.Sleep(100);

            base.Dispose(disposing);
        }
        #endregion

        #region Compile
        /// <summary>
        /// Master compiler function.
        /// </summary>
        bool CompileScript()
        {
            bool ok = true;

            if (_scriptFileName is null)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                // Compile now.
                _compiler.CompileScript(_scriptFileName);

                // What happened?
                if (_compiler.CompiledScript is null)
                {
                    _logger.Error($"Compile failed:");

                    // Log compiler results.
                    LogCompilerResults();

                    return false;
                }

                _script = _compiler.CompiledScript as ScriptCore;

                // Update file watcher just in case.
                _watcher.Clear();
                _compiler.SourceFiles.ForEach(f => { _watcher.Add(f); });

                if (ok)
                {
                    SetCompileStatus(true);

                    // Need exception handling here to protect from user script errors.
                    try
                    {
                        // Init shared vars.
                        InitRuntime();

                        // Setup script. This builds the sequences and sections.
                        _surface.InitSurface(_script); // TODO maybe combine with main draw

                        // Script may have altered shared values.
                        ProcessRuntime();
                    }
                    catch (Exception ex)
                    {
                        ProcessScriptRuntimeError(ex);
                        ok = false;
                    }

                    SetCompileStatus(ok);
                }
                else
                {
                    _logger.Warn("Compile failed.");
                    ok = false;
                    ProcessPlay(PlayCommand.Stop);
                    SetCompileStatus(false);
                }
            }

            return ok;
        }

        /// <summary>
        /// Update system statuses.
        /// </summary>
        /// <param name="compileStatus"></param>
        void SetCompileStatus(bool compileStatus)
        {
            if (compileStatus)
            {
                btnCompile.BackColor = SystemColors.Control;
                _needCompile = false;
            }
            else
            {
                btnCompile.BackColor = Color.Red;
                _needCompile = true;
            }
        }

        /// <summary>Log helper.</summary>
        void LogCompilerResults()
        {
            _compiler.Reports.ForEach(r =>
            {
                var msg = r.SourceFileName is not null ?
                    $"{r.ReportType}: {r.SourceFileName}({r.SourceLineNumber}) [{r.Message}]" :
                    $"{r.ReportType}: [{r.Message}]";
                switch (r.Level)
                {
                    case ReportLevel.Error: _logger.Error(msg); break;
                    case ReportLevel.Warning: _logger.Warn(msg); break;
                    case ReportLevel.Info: _logger.Info(msg); break;
                    default: break;
                }
            });
        }
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // Kick over to main UI thread.
            if (_script is not null && _mmTimer.Running)
            {
                try
                {
                    this.InvokeIfRequired(_ => { NextDraw(); });
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(ex);
                }
            }
        }

        /// <summary>
        /// Output next frame.
        /// </summary>
        void NextDraw()
        {
            InitRuntime();

            if (btnPlay.Checked && !_needCompile)
            {
                try
                {
                    _surface.UpdateSurface();
                }
                catch (Exception ex)
                {
                    ProcessScriptRuntimeError(ex);
                }
            }

            // Process whatever the script did.
            ProcessRuntime();
        }

        /// <summary>
        /// Package up the runtime stuff the script may need. Call this before any script updates.
        /// </summary>
        void InitRuntime()
        {
            if(_script is not null)
            {
                _script.RealTime = (float)(DateTime.Now - _startTime).TotalSeconds;
                _script.FrameRate = _frameRate;
            }
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (_script is not null)
            {
                if (_script.FrameRate != _frameRate)
                {
                    _frameRate = _script.FrameRate;
                    SetUiTimerPeriod();
                }
            }
        }

        /// <summary>
        /// Runtime error.
        /// </summary>
        /// <param name="ex"></param>
        void ProcessScriptRuntimeError(Exception ex)
        {
            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            _compiler.ProcessRuntimeException(ex);
            LogCompilerResults();
        }
        #endregion

        #region File handling
        /// <summary>
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openDlg = new()
            {
                Filter = "NProcessing files (*.np)|*.np",
                Title = "Select a NProcessing file"
            };
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenScriptFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                ToolStripMenuItem item = (ToolStripMenuItem)sender;
                string fn = item.ToString();
                OpenScriptFile(fn);
            }
        }

        /// <summary>
        /// Common np file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        public string OpenScriptFile(string fn)
        {
            string ret = "";

            try
            {
                _logger.Info($"Reading np file: {fn}");
                _scriptFileName = fn;

                AddToRecentDefs(fn);

                bool ok = CompileScript();
                SetCompileStatus(ok);

                Text = $"NProcessing {MiscUtils.GetVersionString()} - {fn}";
            }
            catch (Exception ex)
            {
                ret = $"Couldn't open the np file: {fn} because: {ex.Message}";
                _logger.Error(ret);
                SetCompileStatus(false);
            }

            return ret;
        }

        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateRecentMenu()
        {
            ToolStripItemCollection menuItems = recentToolStripMenuItem.DropDownItems;
            menuItems.Clear();

            _settings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f, null, new EventHandler(Recent_Click));
                menuItems.Add(menuItem);
            });
        }

        /// <summary>
        /// Update the mru with the user selection.
        /// </summary>
        /// <param name="fn">The selected file.</param>
        void AddToRecentDefs(string fn)
        {
            if (File.Exists(fn))
            {
                _settings.UpdateMru(fn);
                PopulateRecentMenu();
            }
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go or stop button.
        /// </summary>
        void Play_Click(object sender, EventArgs e)
        {
            ProcessPlay(btnPlay.Checked ? PlayCommand.Start : PlayCommand.Stop);
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        void Compile_Click(object sender, EventArgs e)
        {
            CompileScript();
            ProcessPlay(PlayCommand.Stop);
        }
        #endregion

        #region User settings
        /// <summary>
        /// Save user settings that aren't automatic.
        /// </summary>
        void SaveSettings()
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);
            _settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 300);

            // Detect changes of interest.
            bool restart = changes.Count > 0;

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            _settings.Save();
        }
        #endregion

        #region Play control
        /// <summary>
        /// Update everything per param.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>Indication of success.</returns>
        bool ProcessPlay(PlayCommand cmd)
        {
            bool ret = true;

            if (_script is not null)
            {
                switch (cmd)
                {
                    case PlayCommand.Start:
                        bool ok = !_needCompile || CompileScript();
                        if (ok)
                        {
                            _startTime = DateTime.Now;
                            btnPlay.Checked = true;
                            _surface.InitSurface(_script);
                        }
                        else
                        {
                            btnPlay.Checked = false;
                            ret = false;
                        }
                        break;

                    case PlayCommand.Stop:
                        btnPlay.Checked = false;
                        break;
                }

                btnPlay.BackColor = btnPlay.Checked ? _settings.SelectedColor : SystemColors.Control;
            }
            else
            {
                btnPlay.Checked = false;
                ret = false;
            }

            return ret;
        }
        #endregion

        #region Keyboard handling
        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                ProcessPlay(btnPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        #endregion

        #region Internal stuff
        /// <summary>
        /// Common func.
        /// </summary>
        void SetUiTimerPeriod()
        {
            // Convert fps to msec per frame.
            double framesPerMsec = (double)_frameRate / 1000;
            double msecPerFrame = 1 / framesPerMsec;
            int period = msecPerFrame > 1.0 ? (int)Math.Round(msecPerFrame) : 1;
            _mmTimer.SetTimer(period, MmTimerCallback);
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            Tools.ShowReadme("NProcessing");
        }

        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            // Usually come from a different thread.
            if (IsHandleCreated)
            {
                this.InvokeIfRequired(_ => { textViewer.AppendLine($"{e.Message}"); });
            }
        }

        /// <summary>
        /// Updates.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer1_Tick(object sender, EventArgs e)
        {
        }
        #endregion
    }
}
