using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicTable
{
    /// <summary>
    /// Represent a string tree, according to parentheses
    /// </summary>
    public class UnparsedEq
    {
        #region Public Fields

        /// <summary>
        /// string of the equation (sub tree are written like this : "%####") to be linked with the subEqs
        /// </summary>
        public string str;

        /// <summary>
        /// sub trees
        /// </summary>
        public List<UnparsedEq> subEqs;

        #endregion Public Fields
    }
}