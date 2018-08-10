﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NLog;
using MoreLinq;
using Nebulator.Common;
using Nebulator.Script;



namespace NProcessing
{
    public partial class MainForm : Form
    {
        #region Enums
        /// <summary>Internal status.</summary>
        enum PlayCommand { Start, Stop }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>Fast timer.</summary>
        MmTimerEx _timer = new MmTimerEx();

        /// <summary>Surface child form.</summary>
        Surface _surface = new Surface();

        /// <summary>Current script file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;

        /// <summary>The current script.</summary>
        ScriptCore _script = null;

        /// <summary>Frame rate in fps.</summary>
        int _frameRate = 30;

        /// <summary>Seconds since start pressed.</summary>
        DateTime _startTime = DateTime.Now;

        /// <summary>Script compile errors and warnings.</summary>
        List<ScriptError> _compileResults = new List<ScriptError>();

        /// <summary>Detect changed script files.</summary>
        MultiFileWatcher _watcher = new MultiFileWatcher();

        /// <summary>Files that have been changed externally or have runtime errors - requires a recompile.</summary>
        bool _needCompile = false;

        /// <summary>The temp dir for channeling down runtime errors.</summary>
        string _compileTempDir = "";

        /// <summary>Persisted internal values for current script file.</summary>
        Bag _npVals = new Bag();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Need to load settings before creating controls in MainForm_Load().
            string appDir = Utils.GetAppDataDir();
            DirectoryInfo di = new DirectoryInfo(appDir);
            di.Create();
            UserSettings.Load(appDir);
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object sender, EventArgs e)
        {
            // Hook logging to client window.
            LogClientNotificationTarget.ClientNotification += Log_ClientNotification;
            _logger.Info("hellow world");

            txtView.Font = UserSettings.TheSettings.EditorFont;
            txtView.BackColor = UserSettings.TheSettings.BackColor;
            txtView.WordWrap = UserSettings.TheSettings.WordWrap;

            //btnClear.Image = Utils.ColorizeBitmap(btnClear.Image, UserSettings.TheSettings.IconColor);
            btnClear.Click += (object _, EventArgs __) => { txtView.Clear(); };

            //btnWrap.Image = Utils.ColorizeBitmap(btnWrap.Image, UserSettings.TheSettings.IconColor);
            btnWrap.Checked = UserSettings.TheSettings.WordWrap;
            btnWrap.Click += (object _, EventArgs __) => { UserSettings.TheSettings.WordWrap = btnWrap.Checked; txtView.WordWrap = btnWrap.Checked; };

            // Init main UI from settings
            if (UserSettings.TheSettings.MainFormInfo.Width == 0)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                Location = new Point(UserSettings.TheSettings.MainFormInfo.X, UserSettings.TheSettings.MainFormInfo.Y);
                Size = new Size(UserSettings.TheSettings.MainFormInfo.Width, UserSettings.TheSettings.MainFormInfo.Height);
                WindowState = FormWindowState.Normal;
            }

            _surface.Size = new Size(UserSettings.TheSettings.SurfaceFormInfo.Width, UserSettings.TheSettings.SurfaceFormInfo.Height);
            _surface.Visible = true;
            _surface.TopMost = UserSettings.TheSettings.LockUi;
            _surface.Location = new Point(UserSettings.TheSettings.SurfaceFormInfo.X, UserSettings.TheSettings.SurfaceFormInfo.Y);

            PopulateRecentMenu();

            KeyPreview = true; // for routing kbd strokes properly

            _watcher.FileChangeEvent += Watcher_Changed;

            Text = $"NProcessing {Utils.GetVersionString()} - No file loaded";

            // Catches runtime errors during drawing.
            _surface.RuntimeErrorEvent += (object esender, Surface.RuntimeErrorEventArgs eargs) => { ScriptRuntimeError(eargs); };

            _timer.TimerElapsedEvent += TimerElapsedEvent;
            SetUiTimerPeriod();
            _timer.Start();

            string f = @"C:\Dev\Nebulator\Examples\generative1.neb";
            string s = OpenFile(f);

            if (s != "")
            {
                _logger.Error($"Couldn't open file because {s}");
            }
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                ProcessPlay(PlayCommand.Stop, false);

                if(_script != null)
                {
                    // Save the project.
                    _npVals.Clear();
                    _npVals.Save();
                }

                // Save user settings.
                SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.Error($"Couldn't save the file: {ex.Message}.");
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
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;

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

            if (_fn == Utils.UNKNOWN_STRING)
            {
                _logger.Warn("No script file loaded.");
                ok = false;
            }
            else
            {
                NebCompiler compiler = new NebCompiler();

                // Save internal nebp file vals now as they will be reloaded during compile.
                _npVals.Save();

                // Compile now.
                _script = compiler.Execute(_fn);

                // Update file watcher just in case.
                _watcher.Clear();
                compiler.SourceFiles.ForEach(f => { if (f != "") _watcher.Add(f); });

                // Process errors. Some may be warnings.
                _compileResults = compiler.Errors;
                int errorCount = _compileResults.Count(w => w.ErrorType == ScriptError.ScriptErrorType.Error);

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
                    ProcessPlay(PlayCommand.Stop, false);
                    SetCompileStatus(false);
                }

                _compileResults.ForEach(r =>
                {
                    if (r.ErrorType == ScriptError.ScriptErrorType.Warning)
                    {
                        _logger.Warn(r.ToString());
                    }
                    else
                    {
                        _logger.Error(r.ToString());
                    }
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
                btnCompile.BackColor = UserSettings.TheSettings.IconColor;
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
        void TimerElapsedEvent(object sender, MmTimerEx.TimerEventArgs e)
        {
            // Kick over to main UI thread.
            BeginInvoke((MethodInvoker)delegate ()
            {
                if (_script != null)
                {
                    NextDraw(e);
                }
            });
        }

        /// <summary>
        /// Output next frame.
        /// </summary>
        /// <param name="e">Information about updates required.</param>
        void NextDraw(MmTimerEx.TimerEventArgs e)
        {
            InitRuntime();

            if (e.ElapsedTimers.Contains("UI") && btnPlay.Checked && !_needCompile)
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
            _script.Playing = btnPlay.Checked;
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
            ProcessPlay(PlayCommand.Stop, false);
            SetCompileStatus(false);

            ScriptError err = ScriptCore.ProcessScriptRuntimeError(args, _compileTempDir);

            if (err != null)
            {
                _logger.Error(err.ToString());
            }
        }
        #endregion

        #region File handling
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
        /// Allows the user to select a neb file from file system.
        /// </summary>
        void Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog()
            {
                Filter = "NProcessing files (*.neb)|*.neb",
                Title = "Select a NProcessing file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// Common neb file opener.
        /// </summary>
        /// <param name="fn">The neb file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        public string OpenFile(string fn)
        {
            string ret = "";

            using (new WaitCursor())
            {
                try
                {
                    _logger.Info($"Reading neb file: {fn}");
                    _npVals = Bag.Load(fn.Replace(".neb", ".nebp"));
                    _fn = fn;

                    // This may be coming from the web service...
                    if (InvokeRequired)
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            SetCompileStatus(true);
                            AddToRecentDefs(fn);

                            Compile();
                        });
                    }

                    Text = $"NProcessing {Utils.GetVersionString()} - {fn}";
                }
                catch (Exception ex)
                {
                    ret = $"Couldn't open the neb file: {fn} because: {ex.Message}";
                    _logger.Error(ret);
                }

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

            UserSettings.TheSettings.RecentFiles.ForEach(f =>
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
                UserSettings.TheSettings.RecentFiles.UpdateMru(fn);
                PopulateRecentMenu();
            }
        }

        /// <summary>
        /// One or more neb files have changed so reload/compile.
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
            ProcessPlay(btnPlay.Checked ? PlayCommand.Start : PlayCommand.Stop, true);
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        void Compile_Click(object sender, EventArgs e)
        {
            Compile();
            ProcessPlay(PlayCommand.Stop, true);
        }
        #endregion

        #region Messages and logging
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

                txtView.SelectionBackColor = BackColor;

                //if (msg.Contains(s)) - colorize

                txtView.AppendText(s);
                txtView.ScrollToCaret();
            });
        }
        #endregion

        #region User settings
        /// <summary>
        /// Save user settings that aren't automatic.
        /// </summary>
        void SaveSettings()
        {
            UserSettings.TheSettings.SurfaceFormInfo.FromForm(_surface);

            if (WindowState == FormWindowState.Maximized)
            {
                UserSettings.TheSettings.MainFormInfo.Width = 0; // indicates maximized
                UserSettings.TheSettings.MainFormInfo.Height = 0;
            }
            else
            {
                UserSettings.TheSettings.MainFormInfo.FromForm(this);
            }

            UserSettings.TheSettings.Save();
        }

        /// <summary>
        /// Edit the options in a property grid.
        /// </summary>
        void Settings_Click(object sender, EventArgs e) // TODO
        {
            //using (Form f = new Form()
            //{
            //    Text = "User Settings",
            //    Size = new Size(350, 400),
            //    StartPosition = FormStartPosition.Manual,
            //    Location = new Point(200, 200),
            //    FormBorderStyle = FormBorderStyle.FixedToolWindow,
            //    ShowIcon = false,
            //    ShowInTaskbar = false
            //})
            //{
            //    PropertyGridEx pg = new PropertyGridEx()
            //    {
            //        Dock = DockStyle.Fill,
            //        PropertySort = PropertySort.NoSort,
            //        SelectedObject = UserSettings.TheSettings
            //    };

            //    //// Supply the midi options. There should be a cleaner way than this but the ComponentModel is a hard wrestle.
            //    //ListSelector.Options.Clear();
            //    //ListSelector.Options.Add("MidiIn", _device1.ProtocolInputs);
            //    //ListSelector.Options.Add("MidiOut", _device1.ProtocolOutputs);

            //    // Detect changes of interest.
            //    //bool midi = false;
            //    bool ctrls = false;
            //    pg.PropertyValueChanged += (sdr, args) =>
            //    {
            //        string p = args.ChangedItem.PropertyDescriptor.Name;
            //        //midi |= p.Contains("Midi");
            //        ctrls |= (p.Contains("Font") | p.Contains("Color"));
            //    };

            //    f.Controls.Add(pg);
            //    f.ShowDialog();

            //    //// Figure out what changed - each handled differently.
            //    //if (midi)
            //    //{
            //    //    _device1.Init();
            //    //}

            //    if (ctrls)
            //    {
            //        MessageBox.Show("UI changes require a restart to take effect.");
            //    }

            //    // Always safe to update these.
            //    SetUiTimerPeriod();
            //    _surface.TopMost = UserSettings.TheSettings.LockUi;

            //    SaveSettings();
            //}
        }
        #endregion

        #region Play control
        /// <summary>
        /// Update everything per param.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="userAction">Something the user did.</param>
        /// <returns>Indication of success.</returns>
        bool ProcessPlay(PlayCommand cmd, bool userAction)
        {
            bool ret = true;

            switch (cmd)
            {
                case PlayCommand.Start:
                    bool ok = _needCompile ? Compile() : true;
                    if (ok)
                    {
                        _startTime = DateTime.Now;
                        btnPlay.Checked = true;
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
                ProcessPlay(btnPlay.Checked ? PlayCommand.Stop : PlayCommand.Start, true);
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
            _timer.SetTimer("UI", (int)msecPerFrame);
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object sender, EventArgs e)
        {
            //About dlg = new About(); TODO
            //dlg.ShowDialog();

            //private void About_Load(object sender, EventArgs e)
            //{
            //    BackColor = UserSettings.TheSettings.BackColor;

            //    string s = Markdown.ToHtml(File.ReadAllText(@"Resources\README.md"));
            //    // Insert some style.
            //    s = s.Insert(0, $"<style>body {{ background - color: {UserSettings.TheSettings.BackColor.Name}; }}</style>");
            //    browser.DocumentText = s;
            //}
        }
        #endregion
    }
}