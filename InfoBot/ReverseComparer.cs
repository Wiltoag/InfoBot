using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Infobot
{
    public class ReverseComparer<T> : IComparer<T>
    {
        #region Public Fields

        public static readonly ReverseComparer<T> Default = new ReverseComparer<T>(Comparer<T>.Default);

        #endregion Public Fields

        #region Private Fields

        private readonly IComparer<T> comparer = Default;

        #endregion Private Fields

        #region Private Constructors

        private ReverseComparer(IComparer<T> comparer)
        {
            this.comparer = comparer;
        }

        #endregion Private Constructors

        #region Public Methods

        public static ReverseComparer<T> Reverse(IComparer<T> comparer)
        {
            return new ReverseComparer<T>(comparer);
        }

        public int Compare([AllowNull] T x, [AllowNull] T y)
        {
            return comparer.Compare(y, x);
        }

        #endregion Public Methods
    }
}