using System;
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

                if (messageText == "/start" || messageText == "/menu")
                {
                    await SendMainMenuAsync(botClient, chatId, cancellationToken);
                }
            }
            else if (update.CallbackQuery is { } callbackQuery)
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;

                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                if (data == "btn_price")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "💰 <b>Стоимость услуг:</b>\n\n" +
                              "🤖 Разработка Telegram-бота — от 5 000 руб.\n" +
                              "🕸 Написание парсера — от 3 000 руб.\n" +
                              "💻 Верстка сайта (HTML/CSS/JS) — от 10 000 руб.\n\n" +
                              "Для точного расчета напишите мне в Telegram!",
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
                else if (data == "btn_contacts")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "📋 <b>Мои контакты:</b>\n\n" +
                              "🐙 GitHub: <a href=\"https://github.com/layZ-rus\">github.com/layZ-rus</a>\n" +
                              "✈️ Telegram: <a href=\"https://t.me/layz102\">@layz102</a>\n" +
                              "📧 Email: <a href=\"mailto:igor_romanyuk_08@Inbox.ru\">igor_romanyuk_08@Inbox.ru</a>",
                        parseMode: ParseMode.Html,
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
                }
            });

            await botClient.SendMessage(
                chatId: chatId,
                text: "Привет! Я Игорь, разработчик. Выберите, что вас интересует:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}