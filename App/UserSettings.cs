using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using System.Text.Json;
using System.Text.Json.Serialization;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfTricks.Slog;
using NBagOfUis;


namespace NProcessing.App
{
    [Serializable]
    public class UserSettings : Settings
    {
        #region Persisted editable properties
        // [DisplayName("Script Path")]
        // [Description("Default location for user scripts.")]
        // [Browsable(true)]
        // [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        // public string ScriptPath { get; set; } = "";

        [DisplayName("Lock UI")]
        [Description("Forces UI to be topmost.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        public bool LockUi { get; set; } = false;

        [DisplayName("Auto Compile")]
        [Description("Compile current file when change detected.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool AutoCompile { get; set; } = true;

        [DisplayName("Ignore Warnings")]
        [Description("Ignore warnings otherwise treat them as errors.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool IgnoreWarnings { get; set; } = true;

        [DisplayName("CPU Meter")]
        [Description("Show a CPU usage meter. Note that this slows start up a bit.")]
        [Category("Functionality")]
        [Browsable(true)]
        public bool CpuMeter { get; set; } = true;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Selected Color")]
        [Description("The color used for selections.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue;
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;
        #endregion
    }
}
