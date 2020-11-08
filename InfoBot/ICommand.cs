using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    internal interface ICommand
    {
        #region Public Properties

        bool Admin { get; }
        IEnumerable<(string, string)> Detail { get; }
        string Key { get; }
        string Summary { get; }

        #endregion Public Properties

        #region Public Methods

        Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args);

        #endregion Public Methods
    }
}