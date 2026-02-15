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

        private Queue<long> _waitingForCompanion = new Queue<long>(); 
        private object _queueLock = new object(); 

        InlineKeyboardButton[] row1 = new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Anon conversation🔎", "user_anoncon"),
                };
        InlineKeyboardButton[] row2 = new[]
        {
                    InlineKeyboardButton.WithCallbackData("Anon message📩", "user_anonmes")
                };
        InlineKeyboardButton[] row3 = new[]
        {
                    InlineKeyboardButton.WithCallbackData("Anon spam📈", "user_anonspam")
                };

        InlineKeyboardMarkup keyboard;

        public Host(string token)
        {
            bot = new TelegramBotClient(token);
            keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });
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
                state.UserName = message?.From?.Username ?? update.CallbackQuery?.From.Username ?? "nousername";
                state.Id = chatId;

                Console.WriteLine($"new user: @{state.UserName}");
            }

            if(message != null && state.inDialog)
            {
                await DialogHandler(chatId, message, state);
                return;
            }
            else if (update.CallbackQuery != null)
            {
                await CallBackQueryHandler(chatId, update.CallbackQuery, state);
                return;
            }
            else if (message.Text != null)
            {
                await TextMessageHandler(chatId, message, state);
                return;
            }
        }

        private async Task TextMessageHandler(long chatId, Message message, UserState state)
        {
            var text = message.Text;

            if (text == "/leave")
            {
                await LeaveConversationAsync(chatId, state);
                return;
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

                var _keyboard = new InlineKeyboardMarkup(new[] { row1, row2, row3 });

                await bot.SendMessage(chatId, "*Hi there*! 🛠📚", replyMarkup: _keyboard, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;

            }
            else if (text == "/stop")
            {
                state.isSpamming = false;
                state.IdToSpam = default;
                state.UsernameToSpam = default;
                await bot.SendMessage(chatId, "You have just stopped the spamming", replyMarkup: keyboard);
                return;
            }
            else if (text == "/exit")
            {
                if (state.inDialog)
                {
                    state.inDialog = false;
                    state.Step = 0;
                    state.IdToMessage = 0;
                    state.UsernameToMessage = null;
                    state.isFirst = false;

                    await bot.SendMessage(chatId, "You have ended the conversation", replyMarkup: keyboard);
                    return;
                }
                else
                {
                    await bot.SendMessage(chatId, "There are no pending conversations right now");
                    return;
                }
            }

            if (text != null && state.isWritingSupport)
            {
                await bot.SendMessage(SupportId, $"new support message from *@{message.From?.Username}*:\n*{text}*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                await bot.SendMessage(chatId, $"You have just sent *'{text}'* to support", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                state.isWritingSupport = false;
                return;
            }

            if (state.isChating && text != null)
            {
                await bot.SendMessage(state.CompanionId, text);
                return;
            }
        }
        private async Task CallBackQueryHandler(long chatId, CallbackQuery callbackQuery, UserState state)
        {
            var data = callbackQuery.Data;

            if(data != null && data.StartsWith("user_"))
            {
                if (data == "user_mainmenu")
                {
                    await bot.SendMessage(chatId, "Main menu⚙", replyMarkup: keyboard);
                }
                else if (data == "user_listofusers")
                {
                    var str = "*List of bot users📃*";
                    foreach (var st in _states)
                    {
                        str += $"\n - @{st.Value.UserName}";
                    }
                    await bot.SendMessage(chatId, str, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
                else if(data == "user_support")
                {
                    await bot.SendMessage(chatId, "Your next message will be sent to support✅");
                    state.isWritingSupport = true;
                }
                else if (data == "user_anoncon")
                {
                    if (state.isLookingCon)
                    {
                        await bot.SendMessage(chatId, "We are already looking for a companion!");
                        return;
                    }
                    if (state.isChating)
                    {
                        await bot.SendMessage(chatId, "You are already in a conversation");
                        return;
                    }

                    await bot.SendChatAction(chatId, Telegram.Bot.Types.Enums.ChatAction.Typing); 

                    await FindCompanion(chatId, state);
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
            else if (data == "leave_chat_button")
            {
                await LeaveConversationAsync(chatId, state);
            }
        }

        private async Task FindCompanion(long searchingChatId, UserState searchingState)
        {
            long? foundCompanionChatId = null; 
            UserState foundCompanionState = null;

            bool foundPair = false; 

            lock (_queueLock)
            {
                if (_waitingForCompanion.Count > 0)
                {
                    foundCompanionChatId = _waitingForCompanion.Dequeue();
                                                                         
                    _states.TryGetValue(foundCompanionChatId.Value, out foundCompanionState);
                    foundPair = true;
                }
                else
                {
                    _waitingForCompanion.Enqueue(searchingChatId);
                    searchingState.isLookingCon = true; 
                    Console.WriteLine($"User {searchingChatId} ({searchingState.UserName}) is now waiting for a companion.");
                }
            } 

            if (foundPair)
            {
                if (foundCompanionState == null)
                {
                    Console.WriteLine($"Ошибка: UserState для {foundCompanionChatId} не найден, возвращаем {searchingChatId} в очередь.");
                    lock (_queueLock) { _waitingForCompanion.Enqueue(searchingChatId); }
                    await bot.SendMessage(searchingChatId, "Error. Try again");
                    return;
                }

                searchingState.isLookingCon = false;
                searchingState.isChating = true;
                searchingState.CompanionId = foundCompanionChatId.Value;

                foundCompanionState.isLookingCon = false;
                foundCompanionState.isChating = true;
                foundCompanionState.CompanionId = searchingChatId;

                Console.WriteLine($"Found companion! {searchingChatId} ({searchingState.UserName}) <-> {foundCompanionChatId} ({foundCompanionState.UserName})");

                await bot.SendMessage(searchingChatId,
                    $"Companion found!\n" +
                    "/leave to leave",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Leave", "leave_chat_button")));

                await bot.SendMessage(foundCompanionChatId.Value,
                    $"Companion found!\n" +
                    "/leave to leave",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Leave", "leave_chat_button")));
            }
            else 
            {
                await bot.SendMessage(searchingChatId, "We are looking for a companion");
            }
        }


        private async Task LeaveConversationAsync(long leavingChatId, UserState leavingState)
        {
            if (!leavingState.isChating)
            {
                await bot.SendMessage(leavingChatId, "You are not in a conversation", replyMarkup: keyboard);
                return;
            }

            long companionChatId = leavingState.CompanionId;
            UserState companionState = null;
            _states.TryGetValue(companionChatId, out companionState);

            leavingState.isChating = false;
            leavingState.CompanionId = 0; 

            await bot.SendMessage(leavingChatId, "You left the conversation", replyMarkup: keyboard); 

            if (companionState != null && companionState.isChating && companionState.CompanionId == leavingChatId)
            {
                companionState.isChating = false;
                companionState.CompanionId = 0;
                await bot.SendMessage(companionChatId, "Your companion has just left the conversation", replyMarkup: keyboard); 
            }
            else if (companionState != null)
            {
                await bot.SendMessage(leavingChatId, "Your companion probably already left the conversation");
            }
        }


        private async Task DialogHandler(long chatId, Message message, UserState state)
        {
            var text = message.Text;
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
                    await bot.SendMessage(state.IdToMessage,
                        "*👀Someone is writing you a message...*", 
                        replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Answer", "user_answer")), 
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                   
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

            else if(state.Step == 4) //is waiting for spam text
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
        public long Id { get; set; }


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


        public bool isLookingCon { get; set; }
       
        public long CompanionId { get; set; }
        public bool isChating {  get; set; }
    }
}
