using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicTable
{
    public abstract class Equation
    {
        #region Public Methods

        /// <summary>
        /// Returns the output of the equation.
        /// </summary>
        /// <param name="keys">The values to give to the constants</param>
        /// <returns>Output value of the equation.</returns>
        abstract public bool Test(Dictionary<string, bool> keys);

        #endregion Public Methods
    }
}