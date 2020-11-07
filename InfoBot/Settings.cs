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

        #endregion Public Fields

        #region Public Properties

        public static Settings Default
        {
            get
            {
                var result = new Settings();
                result.oldHash = new int[] { 0, 0, 0, 0, 0, 0 };
                return result;
            }
        }

        #endregion Public Properties
    }
}