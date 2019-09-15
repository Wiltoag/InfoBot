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
            Queue = new Queue<Func<Task>>();
        }

        #endregion Public Constructors

        #region Private Properties

        private Queue<Func<Task>> Queue { get; set; }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Execute on the main loop the command.
        /// </summary>
        /// <param name="action">Command to execute</param>
        public void Execute(Func<Task> action) => Queue.Enqueue(action);

        public Func<Task> GetNext() => Queue.Count > 0 ? Queue.Dequeue() : null;

        #endregion Public Methods
    }
}