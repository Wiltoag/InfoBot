using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Infobot
{
    /// <summary>
    /// Base class for every command
    /// </summary>
    public interface ICommand
    {
        #region Public Properties

        /// <summary>
        /// True if the command is admin-only
        /// </summary>
        bool Admin { get; }

        /// <summary>
        /// A list of (title, description) for the details of the command
        /// </summary>
        IEnumerable<(string, string)> Detail { get; }

        /// <summary>
        /// The name of the command. It is the string used to call the command
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Short description of the command
        /// </summary>
        string Summary { get; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Async method run when the command is called
        /// </summary>
        /// <param name="ev">Original event triggered</param>
        /// <param name="args">arguments of the command</param>
        /// <returns>Task handle of the asynchronous method</returns>
        Task Handle(MessageCreateEventArgs ev, IEnumerable<string> args);

        #endregion Public Methods

        #region Internal Classes

        internal class Comparer : IEqualityComparer<ICommand>
        {
            #region Public Methods

            public bool Equals([AllowNull] ICommand x, [AllowNull] ICommand y)
                => x.Key.Equals(y.Key);

            public int GetHashCode([DisallowNull] ICommand obj)
                => obj.Key.GetHashCode();

            #endregion Public Methods
        }

        #endregion Internal Classes
    }
}