using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicTable
{
    /// <summary>
    /// The... not operator....
    /// </summary>
    public class Not : Equation
    {
        #region Public Properties

        /// <summary>
        /// Sub equation to apply the not operator
        /// </summary>
        public Equation InternalEquation { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// See in Equation
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public override bool Test(Dictionary<string, bool> keys)
        {
            var res = !InternalEquation.Test(keys);
            return res;
        }

        #endregion Public Methods
    }
}