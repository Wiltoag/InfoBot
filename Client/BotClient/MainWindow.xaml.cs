using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BotClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static WebClient Client;
        private static DiscordChannel currentChannel;
        private static DiscordClient Discord;
        private static Dictionary<ulong, ImageSource> imgCache;
        private static ulong lastMessageId;

        public MainWindow()
        {
            InitializeComponent();
            Task.Run(Init).Wait();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (inputField.Text.Length > 0)
            {
                Discord.SendMessageAsync(currentChannel, inputField.Text);
                inputField.Text = "";
            }
        }

        private ImageSource FindCustomEmoji(ulong emoji)
        {
            if (imgCache.ContainsKey(emoji))
                return imgCache[emoji];
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri($"https://cdn.discordapp.com/emojis/{emoji.ToString(CultureInfo.InvariantCulture)}.png", UriKind.Absolute);
                bitmap.EndInit();
                imgCache.Add(emoji, bitmap);
                return bitmap;
            }
        }

        private ImageSource FindUserAvatar(DiscordUser user)
        {
            if (imgCache.ContainsKey(user.Id))
                return imgCache[user.Id];
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(user.GetAvatarUrl(ImageFormat.Png, 64), UriKind.Absolute);
                bitmap.EndInit();
                imgCache.Add(user.Id, bitmap);
                return bitmap;
            }
        }

        private string GetMemberName(DiscordMember user) =>
                    user.Nickname == null || user.Nickname.Length == 0 ?
            user.Username : user.Nickname;

        private UIElement GetMessage(DiscordMessage message)
        {
            bool bold = false;
            bool italic = false;
            bool underline = false;
            bool strike = false;
            var result = new Grid();
            result.Children.Add(new Image
            {
                Source = FindUserAvatar(message.Author),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 48,
                Height = 48
            });
            result.Children.Add(new Label
            {
                Content = GetMemberName(message.Channel.Guild.GetMemberAsync(message.Author.Id).Result),
                Margin = new Thickness(55, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            });
            var content = new TextBlock
            {
                Margin = new Thickness(0, 55, 0, 25),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var currentLine = "";
            Inline format(string text)
            {
                Inline res = new Run { Text = text };
                if (underline)
                    res.TextDecorations.Add(TextDecorations.Underline);
                if (bold)
                    res = new Bold(res);
                if (strike)
                    res.TextDecorations.Add(TextDecorations.Strikethrough);
                if (italic)
                    res = new Italic(res);
                return res;
            }
            for (int i = 0; i < message.Content.Length; i++)
            {
                char c = message.Content[i];
                if (message.Content.Length > i + 2 && message.Content.Substring(i, 2) == "**")
                {
                    content.Inlines.Add(format(currentLine));
                    currentLine = "";

                    bold = !bold;
                    i++;
                }
                else if (message.Content.Length > i + 2 && message.Content.Substring(i, 2) == "__")
                {
                    content.Inlines.Add(format(currentLine));
                    currentLine = "";

                    underline = !underline;
                    i++;
                }
                else if (message.Content.Length > i + 2 && message.Content.Substring(i, 2) == "~~")
                {
                    content.Inlines.Add(format(currentLine));
                    currentLine = "";

                    strike = !strike;
                    i++;
                }
                else if (c == '*' || c == '_')
                {
                    content.Inlines.Add(format(currentLine));
                    currentLine = "";
                    italic = !italic;
                }
                else if (c == '<')
                {
                    content.Inlines.Add(format(currentLine));
                    currentLine = "";
                    if (message.Content.Length > i + 3 && message.Content[i..(i + 3)] == "<@!")
                    {
                        var id = "";
                        i += 3;
                        while (message.Content[i] != '>')
                            id += message.Content[i++];
                        content.Inlines.Add(new Run
                        {
                            Text = $"@{GetMemberName(currentChannel.Guild.GetMemberAsync(ulong.Parse(id)).Result)}",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 20, 100, 255))
                        });
                    }
                    else if (message.Content.Length > i + 3 && message.Content.Substring(i, 3) == "<@&")
                    {
                        var id = "";
                        i += 3;
                        while (message.Content[i] != '>')
                            id += message.Content[i++];
                        var role = currentChannel.Guild.GetRole(ulong.Parse(id));
                        content.Inlines.Add(new Run
                        {
                            Text = $"@{role.Name}",
                            Foreground = new SolidColorBrush(Color.FromArgb(255, role.Color.R, role.Color.G, role.Color.B))
                        });
                    }
                    else if (message.Content.Length > i + 2 && message.Content.Substring(i, 2) == "<:")
                    {
                        var emojicode = "";
                        i++;
                        while (message.Content[i] != '>')
                            emojicode += message.Content[i++];
                        var id = emojicode.Split(':').Last();
                        content.Inlines.Add(new InlineUIContainer
                        {
                            Child = new Image
                            {
                                Width = 32,
                                Height = 32,
                                Source = FindCustomEmoji(ulong.Parse(id))
                            }
                        });
                    }
                    else
                        currentLine += '<';
                }
                else
                    currentLine += c;
            }
            content.Inlines.Add(format(currentLine));
            result.Children.Add(content);
            return result;
        }

        private async Task Init()
        {
            imgCache = new Dictionary<ulong, ImageSource>();
            currentChannel = null;
            lastMessageId = 0;
            Client = new WebClient();
            string token;
            try
            {
                using (var sr = new StreamReader("token.txt"))
                    token = sr.ReadToEnd();
                Discord = new DiscordClient(new DiscordConfiguration() { Token = token, TokenType = TokenType.Bot });
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
            bool updatingScroll = false;
            messageScroller.ScrollChanged += async (sender, e) =>
            {
                if (messageScroller.VerticalOffset == 0 && lastMessageId != 0 && !updatingScroll)
                {
                    updatingScroll = true;
                    var messages = await currentChannel.GetMessagesAsync(10, lastMessageId);
                    if (messages.Count > 0)
                    {
                        lastMessageId = messages.Last().Id;
                        messagePanel.Dispatcher.Invoke(() =>
                        {
                            double offset = messageScroller.ScrollableHeight;
                            foreach (var mess in messages)
                                messagePanel.Children.Insert(0, GetMessage(mess));
                            messagePanel.UpdateLayout();
                            messageScroller.ScrollToVerticalOffset(messageScroller.ScrollableHeight - offset);
                            messagePanel.UpdateLayout();
                        });
                    }
                    updatingScroll = false;
                }
            };
            Discord.GuildAvailable += e =>
            {
                serverPanel.Dispatcher.Invoke(() =>
                {
                    var serverButton = new Button
                    {
                        Width = 40,
                        Height = 40
                    };
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(e.Guild.IconUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    var serverImage = new Image
                    {
                        Source = bitmap
                    };
                    serverButton.Content = serverImage;
                    serverPanel.Children.Add(serverButton);
                    if (currentChannel == null)
                    {
                        currentChannel = e.Guild.GetDefaultChannel();
                        UpdateChannels();
                        _ = UpdateMessages();
                    }
                    serverButton.Click += (sender, e2) =>
                    {
                        currentChannel = e.Guild.GetDefaultChannel();
                        UpdateChannels();
                        _ = UpdateMessages();
                    };
                    Console.WriteLine($"Guild {e.Guild} available");
                });
                return Task.CompletedTask;
            };
            Discord.Ready += e =>
            {
                Console.WriteLine("client ready");
                return Task.CompletedTask;
            };
            Discord.MessageCreated += e =>
            {
                if (e.Channel == currentChannel)
                    messagePanel.Dispatcher.Invoke(() =>
                    {
                        messagePanel.Children.Add(GetMessage(e.Message));
                        if (messageScroller.VerticalOffset == messageScroller.ScrollableHeight)
                            messageScroller.ScrollToVerticalOffset(double.MaxValue);
                    });
                return Task.CompletedTask;
            };

            await Discord.ConnectAsync();
        }

        private void UpdateChannels()
        {
            void addChannel(StackPanel panel, DiscordChannel channel)
            {
                if (channel.IsCategory)
                {
                    var group = new GroupBox
                    {
                        Header = channel.Name,
                        Margin = new Thickness(5)
                    };
                    var stackPanel = new StackPanel();
                    group.Content = stackPanel;
                    panel.Children.Add(group);
                    var channels = new List<DiscordChannel>(channel.Children.Where(c => c.IsCategory || c.Parent == null));
                    channels.Sort((left, right) => left.Position.CompareTo(right.Position));
                    foreach (var sub in channel.Children)
                        addChannel(stackPanel, sub);
                }
                else
                {
                    var selectChannel = new Button();
                    selectChannel.Click += async (sender, e) =>
                    {
                        currentChannel = channel;
                        await UpdateMessages();
                    };
                    if (channel.Type != ChannelType.Text)
                        selectChannel.IsEnabled = false;
                    selectChannel.Content = channel.Name;
                    panel.Children.Add(selectChannel);
                }
            }
            channelPanel.Dispatcher.Invoke(() =>
            {
                channelPanel.Children.Clear();
                var channels = new List<DiscordChannel>(currentChannel.Guild.Channels.Where(c => c.IsCategory || c.Parent == null));
                channels.Sort((left, right) => left.Position.CompareTo(right.Position));
                foreach (var channel in channels)
                    addChannel(channelPanel, channel);
            });
        }

        private async Task UpdateMessages()
        {
            var messages = new List<DiscordMessage>(await currentChannel.GetMessagesAsync(9, currentChannel.LastMessageId));
            try
            {
                messages.Insert(0, await currentChannel.GetMessageAsync(currentChannel.LastMessageId));
            }
            catch (DSharpPlus.Exceptions.NotFoundException) { }
            lastMessageId = messages.Last().Id;
            messagePanel.Dispatcher.Invoke(() =>
            {
                Title = currentChannel.Guild.Name + " - " + currentChannel.Name;
                messagePanel.Children.Clear();
                foreach (var mess in messages)
                    messagePanel.Children.Insert(0, GetMessage(mess));
                messageScroller.ScrollToVerticalOffset(double.MaxValue);
            });
        }
    }
}