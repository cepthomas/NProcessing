using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Newtonsoft.Json;
using NAudio.Midi;


namespace NProcessing
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Editor Font")]
        [Description("The font to use for editors etc.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        public Font EditorFont { get; set; } = new Font("Consolas", 9);

        [DisplayName("Selected Color")]
        [Description("The color used for selections.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Lock UI")]
        [Description("Forces UI to always topmost.")]
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

        [DisplayName("CPU Meter")]
        [Description("Show a CPU usage meter. Note that this slows start up a bit.")]
        [Category("Devices")]
        [Browsable(true)]
        public bool CpuMeter { get; set; } = true;

        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo MainFormInfo { get; set; } = new FormInfo();

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings settings = null;

            string fn = Path.Combine(appDir, "settings.json");

            if(File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                settings = JsonConvert.DeserializeObject<UserSettings>(json);

                // Clean up any bad file names.
                settings.RecentFiles.RemoveAll(f => !File.Exists(f));
            }
            else
            {
                settings = new UserSettings(); // default
            }

            settings._fn = fn;

            return settings;
        }
        #endregion
    }

    /// <summary>General purpose container for persistence.</summary>
    [Serializable]
    public class FormInfo
    {
        public int X { get; set; } = 50;
        public int Y { get; set; } = 50;
        public int Width { get; set; } = 1000;
        public int Height { get; set; } = 700;

        public void FromForm(Form f)
        {
            X = f.Location.X;
            Y = f.Location.Y;
            Width = f.Width;
            Height = f.Height;
        }
    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> rec = null;

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
