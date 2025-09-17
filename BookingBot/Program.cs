using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

class Program
{
    static async Task Main()
    {
        BotConfig           _config;
        ITelegramBotClient  _botClient;
        ReceiverOptions     _options;
        UpdateHandler       _updateHandler;

        _config = BotConfig.Load();

        _botClient = Register(_config);

        _options = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = true
        };

        _updateHandler = new UpdateHandler(_botClient, _config);

        Logger.Success("Bot is launched");
        await _botClient.ReceiveAsync(_updateHandler.Handle, ErrorHandler.Handle, _options, CancellationToken.None);
    }

    static TelegramBotClient Register(BotConfig config)
    {
        TelegramBotClient botClient;

        while (config.BotId == "")
        {
            Console.Write("Type ID of the bot: ");
            config.BotId = Console.ReadLine();
            try
            {
                botClient = new TelegramBotClient(config.BotId);
                botClient.GetMyName();
            }
            catch
            {
                Logger.Error("Bot with this token does not exist.");
                config.BotId = "";
            }
        }
        botClient = new TelegramBotClient(config.BotId);
        Logger.Success($"Bot {botClient.GetMe().Result.Username} is found.");

        while (config.AdminPass == "")
        {
            Console.Write("Type admin password: ");
            config.AdminPass = Console.ReadLine();
        }

        config.Save();
        return botClient;
    }
}