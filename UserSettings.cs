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


namespace NProcessing
{
    /// <summary>
    /// General purpose container for persistence.
    /// </summary>
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

    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Editor Font"), Description("The font to use for editors etc."), Browsable(true)]
        public Font EditorFont { get; set; } = new Font("Consolas", 9);

        [DisplayName("Selected Color"), Description("The color used for selections."), Browsable(true)]
        public Color SelectedColor { get; set; } = Color.Violet;

        [DisplayName("Background Color"), Description("The color used for overall background."), Browsable(true)]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Lock UI"), Description("Forces UI to always topmost."), Browsable(true)]
        public bool LockUi { get; set; } = false;

        [DisplayName("Midi Input"), Description("Valid device if handling midi input."), Browsable(true)]
        public string MidiIn { get; set; } = "";

        [DisplayName("Midi Output"), Description("Valid device if passing midi input through."), Browsable(true)]
        public string MidiOut { get; set; } = "";

        [DisplayName("Virtual Keyboard"), Description("Show or hide the virtual keyboard."), Browsable(true)]
        public bool Vkey { get; set; } = false;
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
}
