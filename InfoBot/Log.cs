using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Infobot
{
    /// <summary>
    /// Class used to log to the console and log files
    /// </summary>
    public class Log : IDisposable
    {
        #region Private Fields

        private StreamWriter logfile;

        private Mutex mutex;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public Log()
        {
            mutex = new Mutex(false);
            Directory.CreateDirectory("logs");
            logfile = new StreamWriter(new MultiStream(
                new FileStream("latest.log", FileMode.Create, FileAccess.Write, FileShare.Read),
                new FileStream(Path.Combine("logs", $"{DateTime.Now:yyyyMMddHHmmss}.log"), FileMode.Create, FileAccess.Write, FileShare.Read)));
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Special log type used to debug stuff
        /// </summary>
        /// <param name="value">object to print</param>
        public void Debug(object value) => Write(value.ToString(), 3);

        /// <summary>
        /// Dispose this instance of Log, closing the files
        /// </summary>
        public void Dispose()
        {
            logfile.Close();
        }

        /// <summary>
        /// Error log type. Used when something wrong happened
        /// </summary>
        /// <param name="value">object to print</param>
        public void Error(object value) => Write(value.ToString(), 2);

        /// <summary>
        /// Info log type. Used to display
        /// </summary>
        /// <param name="value"></param>
        public void Info(object value) => Write(value.ToString(), 0);

        /// <summary>
        /// Warning log type. Used when something requires attention
        /// </summary>
        /// <param name="value"></param>
        public void Warning(object value) => Write(value.ToString(), 1);

        #endregion Public Methods

        #region Private Methods

        private void Write(string value, int info)
        {
            mutex.WaitOne();
            var customColor = info switch
            {
                0 => ConsoleColor.Blue,
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.Red,
                3 => ConsoleColor.Green,
                _ => default
            };
            var code = info switch
            {
                0 => "INFO",
                1 => "WARN",
                2 => "ERROR",
                3 => "DEBUG",
                _ => default
            };
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write('[');
            Console.ForegroundColor = customColor;
            Console.Write(code);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            bool quoted = false;
            foreach (var str in value.Split('\'', StringSplitOptions.None))
            {
                if (quoted)
                    Console.Write($"'{str}'");
                else
                    Console.Write(str);
                quoted = !quoted;
                Console.ForegroundColor = Console.ForegroundColor switch
                {
                    ConsoleColor.White => ConsoleColor.Gray,
                    ConsoleColor.Gray => ConsoleColor.White,
                    _ => default
                };
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            logfile.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [{code}] {value}");
            logfile.Flush();
            mutex.ReleaseMutex();
        }

        #endregion Private Methods
    }
}