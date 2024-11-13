﻿using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace SunTgBot
{
    internal class MessageHandler
    {
        private bool isDaylightIncreasing;
        private readonly string botToken;
        private readonly WeatherApiManager weatherApiManager;
        private readonly TelegramBotClient botClient;

        internal MessageHandler(string botToken, WeatherApiManager weatherApiManager, TelegramBotClient botClient)
        {
            this.botToken = botToken;
            this.weatherApiManager = weatherApiManager;
            this.botClient = botClient;
        }

        public async Task SendDailyMessageAsync()
        {
            var today = DateTime.Now.Date;
            var solsticeStatus = GetSolsticeStatus(today);

            if (solsticeStatus.isSolsticeDay)
            {
                await Console.Out.WriteLineAsync($"It's the {solsticeStatus.solsticeType} solstice.");
            }

            isDaylightIncreasing = solsticeStatus.isDaylightIncreasing;
        }

        private static (bool isSolsticeDay, string solsticeType, bool isDaylightIncreasing) GetSolsticeStatus(DateTime currentDate)
        {
            var solstice = SolsticeData.GetSolsticeByYear(currentDate.Year);

            bool isSolsticeDay = false;
            string solsticeType = string.Empty;
            bool isDaylightIncreasing = false;

            if (solstice == null) return (isSolsticeDay, solsticeType, isDaylightIncreasing);

            if (currentDate == solstice.Value.Winter)
            {
                isSolsticeDay = true;
                solsticeType = "winter";
            }
            else if (currentDate == solstice.Value.Summer)
            {
                isSolsticeDay = true;
                solsticeType = "summer";
            }

            isDaylightIncreasing = currentDate >= solstice.Value.Winter && currentDate < solstice.Value.Summer;
            return (isSolsticeDay, solsticeType, isDaylightIncreasing);
        }

        }

        public async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message]
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Bot receiving has been cancelled.");
            }
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text?.StartsWith("/gettodaysinfo") == true)
            {
                await SendDailyMessageAsync();
                long chatId = update.Message.Chat.Id;

                if (isDaylightIncreasing)
                {
                    await HandleGetTodaysInfo(chatId);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Daylight hours are shortening, wait for the winter solstice.");
                }
            }

            if (update.Message?.Text?.StartsWith("/getdaystillsolstice") == true)
            {
                await SendDailyMessageAsync();
                long chatId = update.Message.Chat.Id;

                if (!isDaylightIncreasing)
                {
                    DateTime today = DateTime.Now;
                    
                    await botClient.SendTextMessageAsync(chatId, $"Days till the solstice: {WeatherDataParser.CalculateDaysTillNearestSolstice(today)}.");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Daylight hours are increasing, wait for the summer solstice.");
                }
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}
