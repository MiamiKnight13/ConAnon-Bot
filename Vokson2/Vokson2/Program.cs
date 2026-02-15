using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Vokson2
{
    internal class Program
    {
        static readonly string TOKEN = Environment.GetEnvironmentVariable("TG_VOKSON_2");
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Host bot = new Host(TOKEN);
            bot.Start();
            Console.ReadLine();
        }
    }

    class Host
    {
        long SupportId = 1369750317;
        TelegramBotClient bot;
        public Dictionary<long, UserState> _states = new();

        public Host(string token)
        {
            bot = new TelegramBotClient(token);
        }

        public void Start()
        {
            bot.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("bot has been started");
        }

        public async Task Spam(UserState state)
        {
            Console.WriteLine($"Spamming from: @{state.UserName}\nId To Spam: {state.IdToSpam}\nUsername To Spam: @{state.UsernameToSpam}\nSpam text: {state.TextToSpam}");
            while(state.isSpamming)
            {
                await bot.SendMessage(state.IdToSpam, state.TextToSpam);
                await Task.Delay(400);
            }
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine(exception.Message);
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message;
            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;

            if(!_states.TryGetValue(chatId, out var state))
            {
                state = new UserState();
                _states[chatId] = state;
                state.UserName =  message?.From?.Username ?? update.CallbackQuery?.From.Username ?? "nousername";

                Console.WriteLine($"new user: @{state.UserName}");
            }

            if(message != null && state.inDialog)
            {
                await DialogHandler(chatId, message, state);
            }
            else if (update.CallbackQuery != null)
            {
                await CallBackQueryHandler(chatId, update.CallbackQuery, state);
            }
            else if (message.Text != null)
            {
                await TextMessageHandler(chatId, message, state);
            }
        }

        private async Task TextMessageHandler(long chatId, Message message, UserState state)
        {
            var text = message.Text;

            if(text != null && state.isWritingSupport)
            {
                await bot.SendMessage(SupportId, $"new support message from @{message.From?.Username}:\n{text}");
                await bot.SendMessage(chatId, $"You have just sent '{text}' to support");
                state.isWritingSupport = false;
            }

            if(text == "/start")
            {
                var row1 = new[]
                {
                    InlineKeyboardButton.WithCallbackData("Main menu", "user_mainmenu"),
                    InlineKeyboardButton.WithCallbackData("List of users", "user_listofusers")
                };
                var row2 = new[]
                {
                    InlineKeyboardButton.WithCallbackData("Support", "user_support")
                };
                var row3 = new[]
                {
                    InlineKeyboardButton.WithUrl("My Git Hub", "https://github.com/MiamiKnight13")
                };

                var keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });

                await bot.SendMessage(chatId, "*Hi there*! 🛠📚", replyMarkup: keyboard, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

            }
        }
        private async Task CallBackQueryHandler(long chatId, CallbackQuery callbackQuery, UserState state)
        {
            var data = callbackQuery.Data;

            if(data != null && data.StartsWith("user_"))
            {
                if (data == "user_mainmenu")
                {
                    var row1 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon conversation🔎", "user_anoncon"),
                };
                    var row2 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon message📩", "user_anonmes")
                };
                    var row3 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon spam📈", "user_anonspam")
                };

                    var keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });

                    await bot.SendMessage(chatId, "Main menu⚙", replyMarkup: keyboard);
                }
                else if (data == "user_listofusers")
                {
                    var str = "List of bot users📃";
                    foreach (var st in _states)
                    {
                        str += $"\n - @{st.Value.UserName}";
                    }
                    await bot.SendMessage(chatId, str);
                }
                else if(data == "user_support")
                {
                    await bot.SendMessage(chatId, "Your next message will be sent to support✅");
                    state.isWritingSupport = true;
                }
                else if(data == "user_anoncon")
                {

                }
                else if(data == "user_anonmes")
                {
                    await bot.SendMessage(chatId, "send the Username");
                    state.inDialog = true;
                    state.Step = 1;
                }
                else if(data == "user_anonspam")
                {
                    await bot.SendMessage(chatId, "send the Username");
                    state.inDialog = true;
                    state.Step = 3;
                }
            }
        }
        private async Task DialogHandler(long chatId, Message message, UserState state)
        {
            var text = message.Text;

            if(text == "/exit")
            {
                if(state.inDialog)
                {
                    state.inDialog = false;
                    state.Step = 0;
                    state.IdToMessage = 0;
                    state.UsernameToMessage = null;
                    state.isFirst = false;

                    var row1 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon conversation🔎", "user_anoncon"),
                };
                    var row2 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon message📩", "user_anonmes")
                };
                    var row3 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon spam📈", "user_anonspam")
                };

                    var keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });
                    await bot.SendMessage(chatId, "You have ended the conversation", replyMarkup: keyboard);
                }
                else
                {
                    await bot.SendMessage(chatId, "There are no pending conversations now");
                }
            }
            else if(text == "/stop")
            {
                var row1 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon conversation🔎", "user_anoncon"),
                };
                var row2 = new[]
                {
                    InlineKeyboardButton.WithCallbackData("Anon message📩", "user_anonmes")
                };
                var row3 = new[]
                {
                    InlineKeyboardButton.WithCallbackData("Anon spam📈", "user_anonspam")
                };

                var keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });
                state.isSpamming = false;
                await bot.SendMessage(chatId, "You have just stopped the spamming", replyMarkup: keyboard);
            }

                bool sent = false;

            if (state.Step == 1) //is waiting for Username to get ID 
            {
                if(text != null)
                {
                    state.UsernameToMessage = text;
                }

                foreach(var st in _states)
                {
                    if(state.UsernameToMessage == st.Value.UserName)
                    {
                        state.IdToMessage = st.Key;
                        sent = true;
                        break;
                    }
                }

                if (sent == false)
                {
                    await bot.SendMessage(chatId, "Username not found. Please make sure the bot was launched by the person you want to message.");
                    state.inDialog = false;
                    state.Step = 0;
                    state.IdToMessage = 0;
                    return;
                }

                await bot.SendMessage(chatId, "set the text you want to send");
                state.Step++; // = 2
            }

            else if(state.Step == 2) //is waiting for text to send
            {
                if(text != null)
                state.TextToSend = text;

                if(state.isFirst)
                {
                    await bot.SendMessage(state.IdToMessage, "*👀Someone is writing you a message...*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    state.isFirst = false;
                }

                await bot.SendMessage(state.IdToMessage, state.TextToSend);
                await bot.SendMessage(chatId, $"You have just sent '{state.TextToSend}' to @{state.UsernameToMessage}\nThe conversation continues, /exit to end");
            }

            else if(state.Step == 3) // is waiting for Username to get ID
            {
                if (text != null)
                {
                    state.UsernameToSpam = text;
                }

                foreach (var st in _states)
                {
                    if (state.UsernameToSpam == st.Value.UserName)
                    {
                        state.IdToSpam = st.Key;
                        sent = true;
                        break;
                    }
                }

                if (sent == false)
                {
                    await bot.SendMessage(chatId, "Username not found. Please make sure the bot was launched by the person you want to message.");
                    state.inDialog = false;
                    state.Step = 0;
                    state.IdToMessage = 0;
                    return;
                }

                await bot.SendMessage(chatId, "set the text you want to send");
                state.Step++; // = 4
            }

            else if(state.Step == 4)
            {
                if(text != null)
                {
                    state.TextToSpam = text;
                    state.isSpamming = true;
                }

                _ = Spam(state);

                await bot.SendMessage(chatId, $"You are now spamming @{state.UsernameToSpam} with '{state.TextToSpam}'\n/stop to stop");
                state.inDialog = false;
                state.Step = 0;
            }
        }
    }

    class UserState
    {
        public bool isAdmin { get; set; }
        public string? UserName { get; set; }


        public bool isWritingSupport { get; set; }
        public bool inDialog { get; set; }
        public int Step { get; set; } // 1 - is waiting for the Username to send anon message;
                                      // 2 - is waiting for the text to send;
                                      // 3 - is waiting for the Username to spam;
                                      // 4 - is waiting for the text to spam;

        public long IdToMessage { get; set; }
        public string? UsernameToMessage { get; set; }
        public string? TextToSend { get; set; }
        public bool isFirst { get; set; } = true;


        public long IdToSpam { get; set; }
        public string? UsernameToSpam { get; set; }
        public string? TextToSpam { get; set; }
        public bool isSpamming { get; set; }
    }
}
