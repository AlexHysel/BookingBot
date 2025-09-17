using Telegram.Bot;

public class ErrorHandler
{
    public static Task Handle(ITelegramBotClient _botClient, Exception ex, CancellationToken cToken)
    {
        Logger.Error(ex.Message);
        return Task.CompletedTask;
    }
}
