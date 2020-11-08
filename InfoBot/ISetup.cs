using System;
using System.Collections.Generic;
using System.Text;

namespace Infobot
{
    internal interface ISetup
    {
        #region Public Methods

        void Connected();

        void Setup();

        #endregion Public Methods
    }
}