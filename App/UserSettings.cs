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


namespace NProcessing.App
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
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

        [DisplayName("Lock UI")]
        [Description("Forces UI to be topmost.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        public bool LockUi { get; set; } = false;

        [DisplayName("Midi Input")]
        [Description("Valid device if handling midi input.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiInDevice { get; set; } = "";

        [DisplayName("Virtual Keyboard")]
        [Description("Show or hide the virtual keyboard.")]
        [Category("Devices")]
        [Browsable(true)]
        public bool Vkey { get; set; } = false;

        [DisplayName("Work Path")]
        [Description("Where you keep your neb files.")]
        [Category("Functionality")]
        [Browsable(true)]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string WorkPath { get; set; } = "";

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
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public bool Valid { get; set; } = false;

        [Browsable(false)]
        public bool WordWrap { get; set; } = false;

        [Browsable(false)]
        [JsonConverter(typeof(JsonRectangleConverter))]
        public Rectangle FormGeometry { get; set; } = new Rectangle(50, 50, 600, 400);

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "";
        #endregion

        #region Persistence
        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings settings = new();
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                var jobj = JsonSerializer.Deserialize<UserSettings>(json);
                if (jobj is not null)
                {
                    settings = jobj;
                    settings._fn = fn;
                    settings.Valid = true;
                }
            }
            else
            {
                // Doesn't exist, create a new one.
                settings = new UserSettings()
                {
                    _fn = fn,
                    Valid = true
                };
            }

            settings._fn = fn;

            return settings;
        }

        /// <summary>Save object to file.</summary>
        public void Save()
        {
            if(Valid)
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(_fn, json);
            }
        }
        #endregion
    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "MidiInDevice":
                    rec = new List<string>();
                    for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                    }
                    break;

                case "MidiOutDevice":
                    rec = new List<string>();
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;
            }

            return new StandardValuesCollection(rec);
        }
    }
}
