using System;
using System.Collections.Generic;
using System.Text;

namespace Infobot
{
    public class Settings
    {
        #region Public Fields

        public static Settings CurrentSettings;
        public int[] oldHash;
        public string[] timeTableUrls;

        #endregion Public Fields

        #region Public Properties

        public static Settings Default
        {
            get
            {
                var result = new Settings();
                result.oldHash = new int[] { 0, 0, 0, 0, 0, 0 };
                result.timeTableUrls = new string[] { "", "", "", "", "", "" };
                return result;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void CheckIntegrity()
        {
            if (oldHash == null)
            {
                Program.Logger.Warning("Missing Settings.oldHash");
                oldHash = new int[] { 0, 0, 0, 0, 0, 0 };
            }
            if (timeTableUrls == null)
            {
                Program.Logger.Warning("Missing Settings.timeTableUrls");
                timeTableUrls = new string[] { "", "", "", "", "", "" };
            }
        }

        #endregion Public Methods
    }
}