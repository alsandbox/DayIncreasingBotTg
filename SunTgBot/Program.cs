﻿using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace SunTgBot
{
    internal static class Program
    {
        static async Task Main()
        {
            string botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            long chatId = Environment.GetEnvironmentVariable("CHAT_ID"); ;
            var botManager = new BotManager(botToken, chatId);

            botManager.StartBot();
        }

        internal static async Task HandleGetTodaysInfo(long chatId, string botToken)
        {
            var weatherApiManager = new WeatherApiManager();

            TelegramBotClient botClient = new TelegramBotClient(botToken);

            DateTime date = DateTime.Now.Date.ToLocalTime();
            float latitude = 51.759050f;
            float longitude = 19.458600f;
            string tzId = "UTC+1 CET";

            string weatherDataJson = await weatherApiManager.GetTimeAsync(latitude, longitude, date, tzId);

            if (!string.IsNullOrEmpty(weatherDataJson))
            {
                WeatherData ? weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

                string sunriseTimeString = weatherData.SunriseTime.ToString();
                string sunsetTimeString = weatherData.SunsetTime.ToString();
                string dayLengthString = weatherData.DayLength.ToString();
                await botClient.SendTextMessageAsync(chatId, $"Sunrise time: {sunriseTimeString}" +
                    $"\nSunset time: {sunsetTimeString}" +
                    $"\nThe day length: {dayLengthString}");
            }
            else
            {
                Console.WriteLine("Error fetching weather data");
            }
        }
    }
}

