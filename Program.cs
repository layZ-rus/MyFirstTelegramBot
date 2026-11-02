using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MyFirstTelegramBot
{
    internal class Program
    {
        private static readonly string BotToken = "9041624278:AAEC_I-wlGR_ReC6RiaKV2kNWllE_YgReD0";
        private static readonly HashSet<long> AiModeUsers = new HashSet<long>();

        static async Task Main(string[] args)
        {
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
            Console.WriteLine($"Бот @{me.Username} успешно запущен и готов к работе!");
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

                if (lowerText == "/start" || lowerText == "/menu")
                {
                    AiModeUsers.Remove(chatId);
                    await SendMainMenuAsync(botClient, chatId, cancellationToken);
                }
                else if (lowerText.Contains("📋 мои контакты") || lowerText.Contains("мои контакты"))
                {
                    AiModeUsers.Remove(chatId);
                    await SendContactsAsync(botClient, chatId, cancellationToken);
                }
                else if (lowerText.Contains("💵 узнать стоимость") || lowerText.Contains("узнать стоимость"))
                {
                    AiModeUsers.Remove(chatId);
                    await SendPriceAsync(botClient, chatId, cancellationToken);
                }
                else if (AiModeUsers.Contains(chatId))
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"🧠 ИИ-Ответ: Спасибо за вопрос! Я проанализировал ваш запрос '{messageText}' и скоро выдам результат. Проект находится в разработке!",
                        cancellationToken: cancellationToken);
                }
            }
            else if (update.CallbackQuery is { } callbackQuery)
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;

                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                if (data == "btn_price")
                {
                    AiModeUsers.Remove(chatId);
                    await SendPriceAsync(botClient, chatId, cancellationToken);
                }
                else if (data == "btn_contacts")
                {
                    AiModeUsers.Remove(chatId);
                    await SendContactsAsync(botClient, chatId, cancellationToken);
                }
                else if (data == "btn_ai")
                {
                    AiModeUsers.Add(chatId);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "🤖 Режим ИИ включен! Напишите любой вопрос, и я отвечу на него (Имитация работы нейросети)",
                        cancellationToken: cancellationToken);
                }
            }
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
                    InlineKeyboardButton.WithCallbackData("🧠 Задать вопрос ИИ", "btn_ai")
                }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: "Привет! Я Игорь, разработчик. Я создал этого умного ИИ-ассистента. Выберите, что вас интересует:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        private static async Task SendPriceAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "💰 Мой прайс-лист:\n" +
                      "🔹 Простой лендинг — от 3 000 руб.\n" +
                      "🔹 Telegram-бот на C# — от 4 000 руб.\n" +
                      "🔹 Парсер данных / Скрипт — от 2 500 руб.",
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