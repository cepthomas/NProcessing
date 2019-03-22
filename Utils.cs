using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using System.ComponentModel;


namespace NProcessing
{
    /// <summary>
    /// Static utility functions.
    /// </summary>
    public static class Utils
    {
        #region Constants
        /// <summary>General purpose marker.</summary>
        public const string UNKNOWN_STRING = "???";

        ///// <summary>General UI.</summary>
        //public const int BORDER_WIDTH = 1;
        #endregion

        #region System utils
        /// <summary>
        /// Returns a string with the application version information.
        /// </summary>
        public static string GetVersionString()
        {
            Version ver = typeof(Utils).Assembly.GetName().Version;
            return $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }

        /// <summary>
        /// Get the user app dir.
        /// </summary>
        /// <returns></returns>
        public static string GetAppDataDir()
        {
            string localdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localdir, "NProcessing");
        }

        /// <summary>
        /// Update the MRU.
        /// </summary>
        /// <param name="mruList">The MRU.</param>
        /// <param name="newVal">New value(s) to perhaps insert.</param>
        public static void UpdateMru(this List<string> mruList, string newVal)
        {
            // First check if it's already in there.
            for (int i = 0; i < mruList.Count; i++)
            {
                if (newVal == mruList[i])
                {
                    // Remove from current location so we can stuff it back in later.
                    mruList.Remove(mruList[i]);
                }
            }

            // Insert at the front and trim the tail.
            mruList.Insert(0, newVal);
            while (mruList.Count > 20)
            {
                mruList.RemoveAt(mruList.Count - 1);
            }
        }
        #endregion
    }
}
