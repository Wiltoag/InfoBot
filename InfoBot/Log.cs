using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infobot
{
    internal class Log : IDisposable
    {
        #region Private Fields

        private StreamWriter logfile;

        private Mutex mutex;

        #endregion Private Fields

        #region Public Constructors

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

        public void Dispose()
        {
            logfile.Close();
        }

        public void Error(object value) => Write(value.ToString(), 2);

        public void Info(object value) => Write(value.ToString(), 0);

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
                _ => default
            };
            var code = info switch
            {
                0 => "INFO",
                1 => "WARN",
                2 => "ERROR",
                _ => default
            };
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write('[');
            Console.ForegroundColor = customColor;
            Console.Write(code);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"] {value}");
            logfile.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [{code}] {value}");
            logfile.Flush();
            mutex.ReleaseMutex();
        }

        #endregion Private Methods
    }
}