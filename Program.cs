using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Lab2_Petrenko_bot
{
    class Program
    {
        private static readonly string BotToken = "7139271828:AAG0vJDhtEVSH27uijjNkZbLY0DlcnQ4hoE";

        private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);
        private static readonly HttpClient HttpClient = new HttpClient();

        private static bool isUkrainian = false;

        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            try
            {
                var me = await BotClient.GetMeAsync();
                Console.WriteLine($"Bot id: {me.Id}. Bot name: {me.FirstName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return;
            }

            BotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );

            Console.WriteLine("Bot started, press any key to exit.");
            Console.ReadLine();

            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandleMessageReceived(botClient, update.Message);
            }
        }

        private static async Task HandleMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await SendMainMenu(botClient, message.Chat.Id);
            }
        else if (message.Text == (isUkrainian ? "Випадкова стаття" : "Random Article"))
        {
            var article = await GetRandomWikipediaArticle(isUkrainian ? "uk" : "en");
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: article
            );
        }
            else if (message.Text == (isUkrainian ? "Обрати мову" : "Choose Language"))
            {
                await SendLanguageMenu(botClient, message.Chat.Id);
            }
            else if (message.Text == "English")
            {
                isUkrainian = false;
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "You have selected: English"
                );
                await SendMainMenu(botClient, message.Chat.Id);
            }
            else if (message.Text == "Українська")
            {
                isUkrainian = true;
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Ви вибрали: Українська"
                );
                await SendMainMenu(botClient, message.Chat.Id);
            }
            else if (message.Text == (isUkrainian ? "Назад" : "Go Back"))
            {
                await SendMainMenu(botClient, message.Chat.Id);
            }
        }

        private static async Task SendMainMenu(ITelegramBotClient botClient, long chatId)
        {
            var mainKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { isUkrainian ? "Випадкова стаття" : "Random Article" },
                new KeyboardButton[] { isUkrainian ? "Обрати мову" : "Choose Language" }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: isUkrainian ? "Ласкаво просимо! Виберіть опцію:" : "Welcome! Please choose an option:",
                replyMarkup: mainKeyboard
            );
        }

        private static async Task SendLanguageMenu(ITelegramBotClient botClient, long chatId)
        {
            var languageKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "English" },
                new KeyboardButton[] { "Українська" },
                new KeyboardButton[] { isUkrainian ? "Назад" : "Go Back" }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: isUkrainian ? "Виберіть мову:" : "Please choose a language:",
                replyMarkup: languageKeyboard
            );
        }

        private static async Task<string> GetRandomWikipediaArticle(string language)
        {
            string apiUrl = $"https://{language}.wikipedia.org/api/rest_v1/page/random/summary";
            try
            {
                var response = await HttpClient.GetStringAsync(apiUrl);
                var json = JObject.Parse(response);

                string title = json["title"].ToString();
                string extract = json["extract"].ToString();
                string url = json["content_urls"]["desktop"]["page"].ToString();

                return $"{title}\n\n{extract}\n\n{url}";
            }
            catch
            {
                if (language != "en")
                {
                    return await GetRandomWikipediaArticle("en");
                }
                return "Could not fetch a random article at this time.";
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
