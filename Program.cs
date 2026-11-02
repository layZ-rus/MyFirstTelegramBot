using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyFirstTelegramBot
{
    internal class Program
    {
        // Токен, который вы уже вставили
        private static readonly string BotToken = "9041624278:AAGvw1XgqMLn-XohJMF4BQ2bf-9MvQsZ0-I";

        static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient(BotToken);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            // Обратите внимание: параметр теперь называется errorHandler, а не pollingErrorHandler
            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            // В версии 22.x метод называется GetMe (без Async)
            var me = await botClient.GetMe();

            Console.WriteLine("Бот успешно запущен!");
            Console.WriteLine($"Имя бота: {me.FirstName}, Username: @{me.Username}");
            Console.WriteLine("Нажмите любую клавишу для выхода...");

            Console.ReadLine();

            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message) return;
            if (message.Text is not { } messageText) return;

            var chatId = message.Chat.Id;
            var userName = message.Chat.FirstName ?? "друг";

            Console.WriteLine($"Получено сообщение от {userName}: {messageText}");

            try
            {
                // В версии 22.x метод называется SendMessage
                await botClient.SendMessage(
                    chatId: chatId,
                    text: messageText,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}