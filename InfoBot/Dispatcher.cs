using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;

namespace InfoBot
{
    internal class Dispatcher
    {
        #region Public Constructors

        public Dispatcher()
        {
            Queue = new Queue<Action>();
        }

        #endregion Public Constructors

        #region Private Properties

        private Queue<Action> Queue { get; set; }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Execute on the main loop the command.
        /// </summary>
        /// <param name="action">Command to execute</param>
        public void Execute(Action action) => Queue.Enqueue(action);

        public Action GetNext() => Queue.Count > 0 ? Queue.Dequeue() : null;

        #endregion Public Methods
    }
}