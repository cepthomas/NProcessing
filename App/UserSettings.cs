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
using NBagOfUis;


namespace NProcessing.App
{
    [Serializable]
    public class UserSettings : Settings
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
