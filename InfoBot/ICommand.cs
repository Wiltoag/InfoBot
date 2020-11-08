using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        #region Public Classes

        public class Comparer : IEqualityComparer<ICommand>
        {
            #region Public Methods

            public bool Equals([AllowNull] ICommand x, [AllowNull] ICommand y)
                => x.Key.Equals(y.Key);

            public int GetHashCode([DisallowNull] ICommand obj)
                => obj.Key.GetHashCode();

            #endregion Public Methods
        }

        #endregion Public Classes
    }
}