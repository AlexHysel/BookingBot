using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

class UpdateHandler
{
    AdminHandler    _adminHandler;
    UserHandler     _userHandler;
    BotConfig       _config;
    Schedule        _schedule;

    public UpdateHandler(ITelegramBotClient botClient, BotConfig config)
    {
        _schedule = new Schedule();
        _adminHandler = new AdminHandler(botClient, config, _schedule);
        _userHandler = new UserHandler(botClient, config, _schedule);
        _config = config;
    }

    public async Task Handle(ITelegramBotClient _botClient, Update update, CancellationToken cToken)
    {
        if (update == null) return;
        switch (update.Type)
        {
            case UpdateType.Message:

                if (update.Message?.Chat.Id.ToString() == _config.AdminId) await _adminHandler.Message(update);
                else await _userHandler.Message(update);

                break;

            case UpdateType.CallbackQuery:

                if (update.CallbackQuery?.Message?.Chat.Id.ToString() == _config.AdminId) await _adminHandler.Callback(update);
                else await _userHandler.Callback(update);

                break;
        }
    }
}