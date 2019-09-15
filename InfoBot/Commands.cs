using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBot
{
    partial class Program
    {
        #region Private Methods

        private static void InitCommands()
        {
            Discord.MessageCreated += async (arg) =>
            {
                if (arg.Message.Content.Substring(0, 4) == ">ib ")
                {
                    var input = arg.Message.Content.Substring(4, arg.Message.Content.Length - 4);
                    ParseInput(input, out string command, out string[] args);
                    switch (command)
                    {
                        case "help":
                            await arg.Channel.SendMessageAsync(
@"Help pannel :");
                            break;

                        default:
                            await arg.Channel.SendMessageAsync("Unknown command, type \">ib help\"");
                            break;
                    }
                }
            };
        }

        #endregion Private Methods
    }
}