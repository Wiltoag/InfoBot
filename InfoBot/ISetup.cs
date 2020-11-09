using System;
using System.Collections.Generic;
using System.Text;

namespace Infobot
{
    /// <summary>
    /// Base class for setup addons
    /// </summary>
    public interface ISetup
    {
        #region Public Methods

        /// <summary>
        /// Called when the bot is ready
        /// </summary>
        void Connected();

        /// <summary>
        /// Called when the bot is starting
        /// </summary>
        void Setup();

        #endregion Public Methods
    }
}