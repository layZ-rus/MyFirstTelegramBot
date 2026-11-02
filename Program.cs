using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyFirstTelegramBot
{
    public class User
    {
        [Key]
        public long ChatId { get; set; }
        public string? Username { get; set; }
        public bool IsAiMode { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=bot.db");
        }
    }

    internal class Program
    {
        private static readonly string BotToken = "ВАШ_ТОКЕН_ЗДЕСЬ";
        private static readonly string OpenRouterApiKey = "ВАШ_КЛЮЧ_OPENROUTER_ЗДЕСЬ";
        private static readonly HttpClient HttpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }

            var botClient = new TelegramBotClient(BotToken);
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} запущен (v4.0)");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is { } message && message.Text is { } messageText)
            {
                var chatId = message.Chat.Id;
                var lowerText = messageText.ToLowerInvariant();

                await SaveUserAsync(chatId, message.Chat.Username);

                if (lowerText == "/start" || lowerText == "/menu")
                {
                    await SetAiModeAsync(chatId, false);
                    await SendMainMenuAsync(botClient, chatId, cancellationToken);
                }
                else if (lowerText.Contains("📋 мои контакты") || lowerText.Contains("мои контакты"))
                {
                    await SetAiModeAsync(chatId, false);
                    await SendContactsAsync(botClient, chatId, cancellationToken);
                }
                else if (lowerText.Contains("💵 узнать стоимость") || lowerText.Contains("узнать стоимость"))
                {
                    await SetAiModeAsync(chatId, false);
                    await SendPriceAsync(botClient, chatId, cancellationToken);
                }
                else if (lowerText.Contains("💱 курс валют") || lowerText.Contains("курс валют"))
                {
                    await SetAiModeAsync(chatId, false);
                    await SendCurrencyRateAsync(botClient, chatId, cancellationToken);
                }
                else if (await GetAiModeAsync(chatId))
                {
                    await SendAiResponseAsync(botClient, chatId, messageText, cancellationToken);
                }
            }
            else if (update.CallbackQuery is { } callbackQuery)
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;

                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                if (data == "btn_price")
                {
                    await SetAiModeAsync(chatId, false);
                    await SendPriceAsync(botClient, chatId, cancellationToken);
                }
                else if (data == "btn_contacts")
                {
                    await SetAiModeAsync(chatId, false);
                    await SendContactsAsync(botClient, chatId, cancellationToken);
                }
                else if (data == "btn_currency")
                {
                    await SetAiModeAsync(chatId, false);
                    await SendCurrencyRateAsync(botClient, chatId, cancellationToken);
                }
                else if (data == "btn_ai")
                {
                    await SetAiModeAsync(chatId, true);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "🤖 Режим ИИ (OpenRouter) включен! Напишите любой вопрос, и я отвечу.",
                        cancellationToken: cancellationToken);
                }
            }
        }

        private static async Task SaveUserAsync(long chatId, string? username)
        {
            using var db = new AppDbContext();
            var user = await db.Users.FindAsync(chatId);
            if (user == null)
            {
                db.Users.Add(new User { ChatId = chatId, Username = username, LastSeen = DateTime.UtcNow });
            }
            else
            {
                user.LastSeen = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        private static async Task SetAiModeAsync(long chatId, bool isAiMode)
        {
            using var db = new AppDbContext();
            var user = await db.Users.FindAsync(chatId);
            if (user != null)
            {
                user.IsAiMode = isAiMode;
                await db.SaveChangesAsync();
            }
        }

        private static async Task<bool> GetAiModeAsync(long chatId)
        {
            using var db = new AppDbContext();
            var user = await db.Users.FindAsync(chatId);
            return user?.IsAiMode ?? false;
        }

        private static async Task SendMainMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("💵 Узнать стоимость", "btn_price"),
                    InlineKeyboardButton.WithCallbackData("📋 Мои контакты", "btn_contacts")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("💱 Курс валют", "btn_currency"),
                    InlineKeyboardButton.WithCallbackData("🧠 Задать вопрос ИИ", "btn_ai")
                }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: "Привет! Я Игорь, разработчик. Я создал этого умного ИИ-ассистента (OpenRouter). Выберите, что вас интересует:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        private static async Task SendCurrencyRateAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await HttpClient.GetStringAsync("https://www.cbr-xml-daily.ru/daily_json.js");
                using var doc = JsonDocument.Parse(response);
                var usd = doc.RootElement.GetProperty("Valute").GetProperty("USD").GetProperty("Value").GetDouble();
                var eur = doc.RootElement.GetProperty("Valute").GetProperty("EUR").GetProperty("Value").GetDouble();

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"💱 Курс ЦБ РФ на сегодня:\n🇺🇸 USD: {usd:F2} руб.\n🇪🇺 EUR: {eur:F2} руб.",
                    cancellationToken: cancellationToken);
            }
            catch
            {
                await botClient.SendMessage(chatId: chatId, text: "Не удалось получить курс валют. Попробуйте позже.", cancellationToken: cancellationToken);
            }
        }

        private static async Task SendAiResponseAsync(ITelegramBotClient botClient, long chatId, string userQuestion, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    model = "openrouter/free",
                    messages = new[]
                    {
                        new { role = "system", content = "Ты — полезный ИИ-ассистент. Отвечай кратко, по делу и строго на русском языке." },
                        new { role = "user", content = userQuestion }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpClient.DefaultRequestHeaders.Clear();
                HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenRouterApiKey}");

                var url = "https://openrouter.ai/api/v1/chat/completions";

                var response = await HttpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                using var doc = JsonDocument.Parse(responseString);
                var answer = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                await botClient.SendMessage(chatId: chatId, text: $"🧠 {answer}", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenRouter API Error: {ex.Message}");
                await botClient.SendMessage(chatId: chatId, text: "🧠 Не удалось получить ответ от ИИ. Проверьте ключ и лимиты.", cancellationToken: cancellationToken);
            }
        }

        private static async Task SendPriceAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "💰 Мой прайс-лист:\n🔹 Простой лендинг — от 3 000 руб.\n🔹 Telegram-бот на C# — от 4 000 руб.\n🔹 Парсер данных / Скрипт — от 2 500 руб.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private static async Task SendContactsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "🤝 Связаться со мной можно здесь:\n" +
                      "📱 Telegram: <a href=\"https://t.me/layz102\">@layz102</a>\n" +
                      "🖥 GitHub: <a href=\"https://github.com/layZ-rus\">github.com/layZ-rus</a>\n" +
                      "💼 Kwork: <a href=\"https://kwork.ru/user/layz102\">kwork.ru/user/layz102</a>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}