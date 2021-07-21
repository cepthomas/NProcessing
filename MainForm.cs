using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using NLog;
using NBagOfTricks;
using NBagOfTricks.UI;
using NProcessing.Script;


namespace NProcessing
{
    public partial class MainForm : Form
    {
        #region Enums
        /// <summary>Internal control status.</summary>
        enum PlayCommand { Start, Stop }
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>Fast timer.</summary>
        MmTimerEx _mmTimer = new MmTimerEx();

        /// <summary>Surface child form.</summary>
        readonly Surface _surface = new Surface();

        /// <summary>Current script file name.</summary>
        string _fn = "";

        /// <summary>The current script.</summary>
        NpScript _script = null;

        /// <summary>Frame rate in fps.</summary>
        int _frameRate = 30;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Script compile errors and warnings.</summary>
        List<ScriptError> _compileResults = new List<ScriptError>();

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for channeling down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>The user settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Midi input device.</summary>
        NpMidiInput _midiIn = null;

        /// <summary>Midi output device.</summary>
        NpMidiOutput _midiOut = null;

        /// <summary>Midi event queue.</summary>
        readonly ConcurrentQueue<PMidiEvent> _pmidiEvents = new ConcurrentQueue<PMidiEvent>();

        /// <summary>Optional midi piano.</summary>
        Form _piano = null;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = MiscUtils.GetAppDataDir("NProcessing", "Ephemera");
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            _settings = UserSettings.Load(appDir);
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object sender, EventArgs e)
        {
            bool ok = true;

            txtView.Font = _settings.EditorFont;
            txtView.BackColor = _settings.BackColor;

            btnClear.Click += (object _, EventArgs __) => { txtView.Clear(); };
            btnWrap.Click += (object _, EventArgs __) => { txtView.WordWrap = btnWrap.Checked; };

            InitLogging();

            ///// Init UI //////
            Location = new Point(_settings.MainFormInfo.X, _settings.MainFormInfo.Y);
            Size = new Size(_settings.MainFormInfo.Width, _settings.MainFormInfo.Height);
            WindowState = FormWindowState.Normal;

            _surface.Visible = true;
            _surface.Location = new Point(Right, Top);
            _surface.TopMost = _settings.LockUi;

            ///// CPU meter /////
            CpuMeter cpuMeter = new CpuMeter()
            {
                Width = 50,
                Height = toolStrip1.Height,
                DrawColor = Color.Red
            };
            // This took way too long to find out:
            //https://stackoverflow.com/questions/12823400/statusstrip-hosting-a-usercontrol-fails-to-call-usercontrols-onpaint-event
            cpuMeter.MinimumSize = cpuMeter.Size;
            toolStrip1.Items.Add(new ToolStripControlHost(cpuMeter));

            ok = InitMidi();

            PopulateRecentMenu();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChangeEvent += Watcher_Changed;

            Text = $"NProcessing {MiscUtils.GetVersionString()} - No file loaded";

            // Catches runtime errors during drawing.
            _surface.RuntimeErrorEvent += (object esender, Surface.RuntimeErrorEventArgs eargs) => { ScriptRuntimeError(eargs); };

            // Fast timer.
            SetUiTimerPeriod();
            _mmTimer.Start();

            // Slow timer.
            timer1.Interval = 500;
            timer1.Start();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (ok && args.Count() > 1)
            {
                OpenFile(args[1]);
            }
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ProcessPlay(PlayCommand.Stop);

                // Save user settings.
                SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Something failed during shut down: {ex.Message}.");
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmTimer?.Stop();
                _mmTimer?.Dispose();
                _mmTimer = null;

                _midiIn?.Dispose();
                _midiIn = null;

                _midiOut?.Dispose();
                _midiOut = null;

                _piano?.Dispose();
                _piano = null;

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Compile
        /// <summary>
        /// Master compiler function.
        /// </summary>
        bool Compile()
        {
            bool ok = true;

            if (_fn == "")
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                _script?.ResetVars();

                Compiler compiler = new Compiler();

                // Compile now.
                _script = compiler.Execute(_fn);

                // Update file watcher just in case.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                // Process errors. Some may be warnings.
                _compileResults = compiler.Errors;
                int errorCount = _compileResults.Count(w => w.ErrorType == ScriptErrorType.Error);

                if (errorCount == 0 && _script != null)
                {
                    SetCompileStatus(true);
                    _compileTempDir = compiler.TempDir;

                    try
                    {
                        // Init surface area.
                        InitRuntime();
                        _surface.InitSurface(_script);
                        ProcessRuntime();
                    }
                    catch (Exception ex)
                    {
                        ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
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

                _compileResults.ForEach(r =>
                {
                    _logger.Log(new LogEventInfo()
                    {
                        Level = r.ErrorType == ScriptErrorType.Warning ? LogLevel.Warn : LogLevel.Error,
                        Message = r.ToString()
                    });
                });
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
        #endregion

        #region Realtime handling
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                if (_script != null)
                {
                    // Process any events.
                    while(_pmidiEvents.TryDequeue(out PMidiEvent mevt))
                    {
                        _script.midiEvent(mevt);
                    }

                    // Update the view.
                    NextDraw();
                }
            });
        }

        /// <summary>
        /// Output next frame.
        /// </summary>
        /// <param name="e">Information about updates required.</param>
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
                    ScriptRuntimeError(new Surface.RuntimeErrorEventArgs() { Exception = ex });
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
            _script.RealTime = (float)(DateTime.Now - _startTime).TotalSeconds;
            _script.FrameRate = _frameRate;
        }

        /// <summary>
        /// Process whatever the script may have done.
        /// </summary>
        void ProcessRuntime()
        {
            if (_script.FrameRate != _frameRate)
            {
                _frameRate = _script.FrameRate;
                SetUiTimerPeriod();
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="args"></param>
        void ScriptRuntimeError(Surface.RuntimeErrorEventArgs args)
        {
            ProcessPlay(PlayCommand.Stop);
            SetCompileStatus(false);

            ScriptError err = ProcessScriptRuntimeError(args, _compileTempDir);

            if (err != null)
            {
                _logger.Error(err.ToString());
            }
        }

        /// <summary>
        /// Runtime error. Look for ones generated by our script - normal occurrence which the user should know about.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="compileDir"></param>
        ScriptError ProcessScriptRuntimeError(Surface.RuntimeErrorEventArgs args, string compileDir)
        {
            ScriptError err = null;

            // Locate the offending frame.
            string srcFile = "";
            int srcLine = -1;
            StackTrace st = new StackTrace(args.Exception, true);
            StackFrame sf = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame stf = st.GetFrame(i);
                if (stf.GetFileName() != null && stf.GetFileName().ToUpper().Contains(compileDir.ToUpper()))
                {
                    sf = stf;
                    break;
                }
            }

            if (sf != null)
            {
                // Dig out generated file parts.
                string genFile = sf.GetFileName();
                int genLine = sf.GetFileLineNumber() - 1;

                // Open the generated file and dig out the source file and line.
                string[] genLines = File.ReadAllLines(genFile);

                srcFile = genLines[0].Trim().Replace("//", "");

                int ind = genLines[genLine].LastIndexOf("//");
                if (ind != -1)
                {
                    string sl = genLines[genLine].Substring(ind + 2);
                    int.TryParse(sl, out srcLine);
                }

                err = new ScriptError()
                {
                    ErrorType = ScriptErrorType.Runtime,
                    SourceFile = srcFile,
                    LineNumber = srcLine,
                    Message = args.Exception.Message
                };
            }
            else // unknown?
            {
                err = new ScriptError()
                {
                    ErrorType = ScriptErrorType.Runtime,
                    SourceFile = "",
                    LineNumber = -1,
                    Message = args.Exception.Message
                };
            }

            return err;
        }
        #endregion

        #region Midi
        /// <summary>
        /// Init midi stuff.
        /// </summary>
        bool InitMidi()
        {
            bool valid = true;

            if (_settings.MidiIn != "")
            {
                _midiIn = new NpMidiInput();
                if (_midiIn.Init(_settings.MidiIn))
                {
                    _midiIn.InputEvent += MidiIn_InputEvent;
                }
                else
                {
                    _logger.Error(_midiIn.ErrorInfo);
                    _midiIn = null;
                    valid = false;
                }
            }

            if (_settings.MidiOut != "")
            {
                _midiOut = new NpMidiOutput();
                if (_midiOut.Init(_settings.MidiOut))
                {
                }
                else
                {
                    _logger.Error(_midiOut.ErrorInfo);
                    _midiOut = null;
                    valid = false;
                }
            }

            if (_settings.Vkey)
            {
                CreatePiano();
            }

            return valid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MidiIn_InputEvent(object sender, NpMidiEventArgs e)
        {
            PMidiEvent mevt = new PMidiEvent(e.channel, e.note, e.velocity, e.controllerId, e.controllerValue);
            _pmidiEvents.Enqueue(mevt);

            if(_midiOut != null) // pass-thru?
            {
                _midiOut.Send(e);
            }
        }

        /// <summary>
        /// Make a floating midi piano.
        /// </summary>
        void CreatePiano()
        {
            _piano = new Form()
            {
                Text = "Virtual Keyboard",
                Size = new Size(864, 100),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(Left, Bottom + 20),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            VirtualKeyboard vkey = new VirtualKeyboard()
            {
                Dock = DockStyle.Fill,
                ShowNoteNames = true,
            };

            // Set the icon.
            Bitmap bm = new Bitmap(Properties.Resources.glyphicons_327_piano);
            _piano.Icon = Icon.FromHandle(bm.GetHicon());

            vkey.KeyboardEvent += (_, e) =>
            {
                NpMidiEventArgs mevt = new NpMidiEventArgs()
                {
                    channel = e.ChannelNumber,
                    note = e.NoteId,
                    velocity = e.Velocity
                };
                MidiIn_InputEvent(vkey, mevt);
            };

            _piano.Controls.Add(vkey);
            _piano.TopMost = true;
            _piano.Show();
        }
        #endregion

        #region Messages and logging
        /// <summary>
        /// Init all logging functions.
        /// </summary>
        void InitLogging()
        { 
            string appDir = MiscUtils.GetAppDataDir("NProcessing", "Ephemera");

            FileInfo fi = new FileInfo(Path.Combine(appDir, "log.txt"));
            if(fi.Exists && fi.Length > 100000)
            {
                File.Copy(fi.FullName, fi.FullName.Replace("log.", "log2."), true);
                File.Delete(fi.FullName);
            }

            // Hook to client window.
            LogClientNotificationTarget.ClientNotification += Log_ClientNotification;
        }

        /// <summary>
        /// A message from the logger to display to the user.
        /// </summary>
        /// <param name="msg">The message.</param>
        void Log_ClientNotification(string msg)
        {
            BeginInvoke((MethodInvoker)delegate ()
            {
                string s = $"{msg}{Environment.NewLine}";

                if (txtView.TextLength > 5000)
                {
                    txtView.Select(0, 1000);
                    txtView.SelectedText = "";
                }

                txtView.SelectionBackColor = _settings.BackColor;

                txtView.AppendText(s);
                txtView.ScrollToCaret();
            });
        }

        /// <summary>
        /// Show the log file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogShow_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "Log Viewer",
                Size = new Size(900, 600),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                RichTextBox tv = new RichTextBox()
                {
                    Dock = DockStyle.Fill,
                    Font = _settings.EditorFont
                };
                f.Controls.Add(tv);

                string appDir = MiscUtils.GetAppDataDir("NProcessing", "Ephemera");
                string logFilename = Path.Combine(appDir, "log.txt");
                File.ReadAllLines(logFilename).ForEach(l => tv.AppendText(l + Environment.NewLine));

                f.ShowDialog();
            }
        }
        #endregion

        #region File handling
        /// <summary>
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "NProcessing files (*.np)|*.np",
                Title = "Select a NProcessing file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string fn = sender.ToString();
            OpenFile(fn);
        }

        /// <summary>
        /// Common np file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        public string OpenFile(string fn)
        {
            string ret = "";

            try
            {
                _logger.Info($"Reading np file: {fn}");
                _fn = fn;

                AddToRecentDefs(fn);

                bool ok = Compile();
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
                ToolStripMenuItem menuItem = new ToolStripMenuItem(f, null, new EventHandler(Recent_Click));
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
                // First check if it's already in there.
                for (int i = 0; i < _settings.RecentFiles.Count; i++)
                {
                    if (fn == _settings.RecentFiles[i])
                    {
                        // Remove from current location so we can stuff it back in later.
                        _settings.RecentFiles.Remove(_settings.RecentFiles[i]);
                    }
                }

                // Insert at the front and trim the tail.
                _settings.RecentFiles.Insert(0, fn);
                while (_settings.RecentFiles.Count > 20)
                {
                    _settings.RecentFiles.RemoveAt(_settings.RecentFiles.Count - 1);
                }

                PopulateRecentMenu();
            }
        }

        /// <summary>
        /// One or more np files have changed so reload/compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                SetCompileStatus(false);
            });
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
            Compile();
            ProcessPlay(PlayCommand.Stop);
        }
        #endregion

        #region User settings
        /// <summary>
        /// Save user settings that aren't automatic.
        /// </summary>
        void SaveSettings()
        {
            _settings.MainFormInfo.FromForm(this);

            _settings.Save();
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e)
        {
            using (Form f = new Form()
            {
                Text = "User Settings",
                Size = new Size(350, 400),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            })
            {
                PropertyGrid pg = new PropertyGrid()
                {
                    Dock = DockStyle.Fill,
                    PropertySort = PropertySort.NoSort,
                    SelectedObject = _settings
                };

                // Detect changes of interest.
                bool ctrls = false;
                pg.PropertyValueChanged += (sdr, args) =>
                {
                    string p = args.ChangedItem.PropertyDescriptor.Name;
                    ctrls |= p.Contains("Font") | p.Contains("Color") | p.Contains("Midi");
                };

                f.Controls.Add(pg);
                f.ShowDialog();

                if (ctrls)
                {
                    MessageBox.Show("These changes require a restart to take effect.");
                }

                // Always safe to update these.
                SetUiTimerPeriod();
                _surface.TopMost = _settings.LockUi;

                SaveSettings();
            }
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

            if(_script != null)
            {
                switch (cmd)
                {
                    case PlayCommand.Start:
                        bool ok = !_needCompile || Compile();
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                ProcessPlay(btnPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
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
        void About_Click(object sender, EventArgs e)
        {
            // Make some markdown.
            List<string> mdText = new List<string>
            {
                // Device info.
                "# Your Devices",
                "## Midi Input"
            };

            if (NAudio.Midi.MidiIn.NumberOfDevices > 0)
            {
                for (int device = 0; device < NAudio.Midi.MidiIn.NumberOfDevices; device++)
                {
                    mdText.Add($"- {NAudio.Midi.MidiIn.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                mdText.Add($"- None");
            }
            mdText.Add("## Midi Output");
            if (NAudio.Midi.MidiOut.NumberOfDevices > 0)
            {
                for (int device = 0; device < NAudio.Midi.MidiOut.NumberOfDevices; device++)
                {
                    mdText.Add($"- {NAudio.Midi.MidiOut.DeviceInfo(device).ProductName}");
                }
            }
            else
            {
                mdText.Add($"- None");
            }

            // Main help file.
            mdText.AddRange(File.ReadAllLines(@"README.md"));

            Tools.MarkdownToHtml(mdText, "lightcyan", "helvetica", true);
        }

        /// <summary>
        /// Update cpu display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer1_Tick(object sender, EventArgs e)
        {
        }
        #endregion
    }
}
