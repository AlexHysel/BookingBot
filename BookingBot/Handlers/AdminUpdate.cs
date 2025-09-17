using Telegram.Bot;
using Telegram.Bot.Types;

public enum AdminState
{
    Standart,
    ChangePass,
    Schedule,
    DayStart,
    DayEnd
}

public class AdminHandler
{
    ITelegramBotClient _botClient;
    BotConfig _config;
    Schedule _schedule;

    public AdminState adminState;
    public int choosenDay;
    public Message msgToEdit;

    public AdminHandler(ITelegramBotClient botClient, BotConfig config, Schedule schedule)
    {
        _botClient = botClient;
        _config = config;
        _schedule = schedule;

        adminState = AdminState.Standart;
        choosenDay = 0;
        msgToEdit = new Message();
    }

    public async Task Message(Update update)
    {
        if (update.Message == null) return;
        if (update.Message.Text == null) return;

        Message msg = update.Message;
        long chatId = msg.Chat.Id;

        switch (adminState)
        {
            case AdminState.Standart:
                if (msg.Text == "/menu")
                    await _botClient.SendMessage(chatId, "Menu", replyMarkup: Keyboards.AdminMenu);

                break;

            case AdminState.ChangePass:
                _config.AdminPass = msg.Text;
                _config.Save();
                adminState = AdminState.Standart;
                Console.WriteLine($"{msg.From.Username} changed the password");
                await _botClient.SendMessage(chatId, "Password changed");
                break;

            case AdminState.DayStart:
                _config.DayStart = TimeOnly.Parse(msg.Text).ToString();
                _config.Save();
                adminState = AdminState.Standart;
                Console.WriteLine("Day start time changed to " + _config.DayStart);
                await _botClient.SendMessage(chatId, "Day start time changed");
                break;

            case AdminState.DayEnd:
                _config.DayEnd = TimeOnly.Parse(msg.Text).ToString();
                _config.Save();
                adminState = AdminState.Standart;
                Console.WriteLine("Day end time changed to " + _config.DayEnd);
                await _botClient.SendMessage(chatId, "Day end time changed");
                break;
        }
    }

    public async Task Callback(Update update)
    {
        if (update.CallbackQuery == null) return;
        if (update.CallbackQuery.Message == null) return;
        
        CallbackQuery callback = update.CallbackQuery;
        long chatId = callback.Message.Chat.Id;

        switch (callback.Data)
        {
            case "changePass":
                adminState = AdminState.ChangePass;
                await _botClient.SendMessage(chatId, "Type new password");
                break;

            case "schedule":
                msgToEdit = await _botClient.SendMessage(chatId, _schedule.GetText(choosenDay), replyMarkup: Keyboards.ChooseDay);
                break;

            case "timeStart":
                adminState = AdminState.DayStart;
                await _botClient.SendMessage(chatId, "Type new time of the day start. (HH:MM)");
                break;

            case "timeEnd":
                adminState = AdminState.DayEnd;
                await _botClient.SendMessage(chatId, "Type new time of the day end. (HH:MM)");
                break;

            case "today":
                choosenDay = 0;
                await _botClient.EditMessageText(chatId, msgToEdit.Id, _schedule.GetText(0), replyMarkup: Keyboards.ChooseDay);
                break;

            default:
                choosenDay += Convert.ToInt32(update.CallbackQuery.Data);
                await _botClient.EditMessageText(chatId, msgToEdit.Id, _schedule.GetText(choosenDay), replyMarkup: Keyboards.ChooseDay);
                break;
        }
    }
}