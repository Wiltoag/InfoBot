using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;

namespace InfoBot
{
    internal partial class Program
    {
        #region Private Properties

        private static DiscordClient Discord { get; set; }
        private static Dispatcher Dispatcher { get; set; }

        #endregion Private Properties

        #region Private Methods

        private static async Task AsyncMain(string[] args)
        {
            string token;
            Console.Write("Token :");
            token = Console.ReadLine();
            Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
            Console.WriteLine("Connecting...");
            if (!ExecuteAsyncMethod(() => Discord.ConnectAsync()))
                Environment.Exit(0);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connected");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Initalization...");
            ///////////////////////////////////////

            Dispatcher = new Dispatcher();
            var consoleThread = new Thread(ConsoleManager);
            InitCommands();

            ///////////////////////////////////////
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Initialized");
            Console.ForegroundColor = ConsoleColor.Gray;
            consoleThread.Start();

            while (true)
            {
                Dispatcher.GetNext()?.Invoke();
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
            }
        }

        private static void ConsoleManager()
        {
            while (true)
            {
                Console.Write('>');
                var input = Console.ReadLine();
                ParseInput(input, out string command, out string[] args);
                switch (command)
                {
                    case "help":
                        Console.WriteLine("quit : close the bot.");
                        Console.WriteLine("reconnect : reconnect the bot.");
                        Console.WriteLine("help : display this info.");
                        break;

                    case "quit":
                        Environment.Exit(0);
                        break;

                    case "reconnect":
                        Console.WriteLine("Connecting...");
                        if (ExecuteAsyncMethod(() => Discord.ReconnectAsync()))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Connected");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Command not recognized, type \"help\"");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }
            }
        }

        private static bool ExecuteAsyncMethod(Func<Task> func)
        {
            try
            {
                if (!func().Wait(TimeSpan.FromSeconds(10)))
                    throw new Exception("10 sec timeout passed, command canceled");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fatal error : " + e.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
            return true;
        }

        private static void Main(string[] args)
        {
            Task.Run(() => AsyncMain(args)).GetAwaiter().GetResult();
        }

        private static void ParseInput(string input, out string command, out string[] args)
        {
            command = "";
            var listArgs = new List<string>();
            int index = 0;
            while (index < input.Length)
            {
                var currChar = input[index];
                if (currChar == ' ')
                    break;
                else
                    command += currChar;
                index++;
            }
            index++;
            var currArg = "";
            bool ignoreSpaces = false;
            while (index < input.Length)
            {
                var currChar = input[index];
                if (currChar == '\\' && input.Length > index + 1)
                {
                    currChar = input[++index];
                    currArg += currChar;
                }
                else if (currChar == ' ' && !ignoreSpaces)
                {
                    if (currArg.Length > 0)
                        listArgs.Add(currArg);
                    currArg = "";
                }
                else if (currChar == '"')
                    ignoreSpaces = !ignoreSpaces;
                else
                    currArg += currChar;
                index++;
            }
            if (currArg.Length > 0)
                listArgs.Add(currArg);
            args = listArgs.ToArray();
        }

        #endregion Private Methods
    }
}