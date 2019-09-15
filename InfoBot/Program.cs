using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBot
{
    internal class Program
    {
        #region Private Methods

        private static async Task AsyncMain(string[] args)
        {
            string Token;
            Console.Write("Token :");
            Token = Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            Task.Run(() => AsyncMain(args)).GetAwaiter().GetResult();
        }

        #endregion Private Methods
    }
}