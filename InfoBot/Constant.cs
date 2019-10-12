using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicTable
{
    /// <summary>
    /// Manages a constant value in the expression (any variable acutally)
    /// </summary>
    public class Constant : Equation
    {
        #region Public Properties

        /// <summary>
        /// Name of the constant
        /// </summary>
        public string Name { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// See in Equation
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public override bool Test(Dictionary<string, bool> keys)
        {
            var res = keys[Name];
            return res;
        }

        #endregion Public Methods
    }
}