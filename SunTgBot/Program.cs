﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace SunTgBot
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading bot token...");
            string botToken = GetTokenFromArgsOrEnv(args, "BOT_TOKEN");

            var botManager = ConfigureBotManager(botToken);

            if (botManager != null)
            {
                await botManager.StartBotAsync();
            }
            else
            {
                Console.WriteLine("Failed to configure BotManager. Exiting...");
            }
        }

        static BotManager? ConfigureBotManager(string botToken)
        {
            try
            {
                var serviceProvider = ConfigureServices();

                var weatherApiManager = serviceProvider.GetService<WeatherApiManager>();
                _ = serviceProvider.GetService<IConfiguration>();

                if (string.IsNullOrEmpty(botToken))
                {
                    Console.WriteLine("Bot token is not provided or configured properly.");
                    return null;
                }

                if (weatherApiManager == null)
                {
                    Console.WriteLine("WeatherApiManager is not configured properly.");
                    return null;
                }

                return new BotManager(botToken, weatherApiManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring BotManager: {ex.Message}");
                return null;
            }
        }

        static string GetTokenFromArgsOrEnv(string[] args, string envVarName)
        {
            string? token = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable(envVarName);

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"The bot token must be provided via command-line arguments or the {envVarName} environment variable.");
            }
            return token;
        }


        private static ServiceProvider ConfigureServices()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddTransient<WeatherApiManager>()
                .BuildServiceProvider();
        }

        internal static async Task HandleGetTodaysInfo(long chatId, string botToken, WeatherApiManager weatherApiManager)
        {
            TelegramBotClient botClient = new TelegramBotClient(botToken);

            DateTime date = DateTime.Now.Date.ToLocalTime();
            float latitude = 51.759050f;
            float longitude = 19.458600f;
            string tzId = "Europe/Warsaw";

            string weatherDataJson = await weatherApiManager.GetTimeAsync(latitude, longitude, date, tzId);

            if (!string.IsNullOrEmpty(weatherDataJson))
            {
                WeatherData? weatherData = Newtonsoft.Json.JsonConvert.DeserializeObject<WeatherData>(weatherDataJson);

                if (weatherData != null)
                {
                    string sunriseTimeString = weatherData.SunriseTime?.ToString() ?? "N/A";
                    string sunsetTimeString = weatherData.SunsetTime?.ToString() ?? "N/A";
                    string dayLengthString = weatherData.DayLength?.ToString() ?? "N/A";

                    await botClient.SendTextMessageAsync(chatId, $"Sunrise time: {sunriseTimeString}" +
                        $"\nSunset time: {sunsetTimeString}" +
                        $"\nThe day length: {dayLengthString}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Unable to retrieve weather data.");
                }
            }
            else
            {
                Console.WriteLine("Error fetching weather data");
            }
        }

    }
}

