# InfoBot
Bot for a custom Discord server

To add a new command, simply create a new class which implements [ICommand](https://github.com/WildGoat07/InfoBot/blob/master/InfoBot/ICommand.cs).

* `Admin` defines the command as admin-only or if everyone can call it.
* `Detail` is an array of (title, description) data used by the help panel. Some kind of large description. can be `null`.
* `Key` is the name of the command, aka the input used to call it.
* `Summary` is a small description of the command.
* `Handle(MessageCreateEventArgs, IEnumerable<string>)` is an asynchronous method called when someone call this command. The first parameter is the triggered event when someone creates a message. The second parameter is an array of the given arguments.

To add a setup (called once at the beginning, used for example to setup timers), create a new class which implements [ISetup](https://github.com/WildGoat07/InfoBot/blob/master/InfoBot/ISetup.cs).

* `Setup()` is the method which is called at the start of the bot.
* `Connected()` is the method which is called once the bot is ready (connected).

To ease the sarting of the bot, create a `token.txt` file next to the `.exe` which contains the token of the bot.
