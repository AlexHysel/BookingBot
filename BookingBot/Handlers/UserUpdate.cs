using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

enum UserState
{
    ChooseDay,
    ChooseTime,
    TypeName,
    TypePhone,
    CancelBooking,
    ChangeDay,
    ChangeTime
}

public class UserHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotConfig _config;
    private readonly Schedule _schedule;
    private readonly Dictionary<long, UserState> _userStates = new();

    private readonly string[] _months =
    {
        "January","February","March","April","May","June","July","August","September","October","November","December"
    };

    public UserHandler(ITelegramBotClient botClient, BotConfig config, Schedule schedule)
    {
        _botClient = botClient;
        _config = config;
        _schedule = schedule;
    }

    public async Task Message(Update update)
    {
        if (update.Message?.Text == null) return;

        var msg = update.Message;
        long chatId = msg.Chat.Id;

        if (!_userStates.ContainsKey(chatId))
        {
            if (msg.Text == _config.AdminPass)
            {
                _config.AdminId = chatId.ToString();
                _config.Save();
                Console.WriteLine($"{msg.From?.Username} became an admin.");
                await _botClient.SendMessage(chatId, "You became an admin. /menu");
                return;
            }

            if (msg.Text == "/start" || msg.Text == "/menu")
                await _botClient.SendMessage(chatId, "Menu", replyMarkup: Keyboards.UserMenu);

            return;
        }

        if (msg.Text == "Cancel")
        {
            await CancelBooking(chatId);
            return;
        }

        if (_userStates[chatId] != UserState.ChooseDay)
            _schedule.getLastBooking(chatId, out Slot slot);

        await HandleUserState(chatId, msg);
    }

    private async Task CancelBooking(long chatId)
    {
        _userStates.Remove(chatId);

        if (_schedule.HasBooking(chatId, out Slot? slot))
        {
            _schedule.Remove(slot);
            _schedule.SaveChanges();
            await _botClient.SendMessage(chatId, "Cancelled.", replyMarkup: new ReplyKeyboardRemove());
        }
    }

    private async Task HandleUserState(long chatId, Message msg)
    {
        _schedule.getLastBooking(chatId, out Slot slot);

        switch (_userStates[chatId])
        {
            case UserState.ChooseDay:
                await HandleChooseDay(chatId, msg.Text);
                break;

            case UserState.ChooseTime:
                await HandleChooseTime(chatId, msg.Text, slot);
                break;

            case UserState.TypeName:
                slot.Name = msg.Text;
                _schedule.SaveChanges();
                _userStates[chatId] = UserState.TypePhone;
                await _botClient.SendMessage(chatId, "Type your phone number.");
                break;

            case UserState.TypePhone:
                slot.PhoneNum = msg.Text;
                _schedule.SaveChanges();
                _userStates.Remove(chatId);

                Console.WriteLine($"{msg.From.Username} has made a booking");
                if (!string.IsNullOrEmpty(_config.AdminId))
                    await _botClient.SendMessage(_config.AdminId, $"{msg.From.Username} has made a booking.");

                await _botClient.SendMessage(chatId, "You made a booking.", replyMarkup: Keyboards.UserMenu);
                break;

            case UserState.ChangeDay:
                await HandleChangeDay(chatId, msg.Text, slot);
                break;

            case UserState.ChangeTime:
                await HandleChangeTime(chatId, msg.Text, slot);
                break;
        }
    }

    private async Task HandleChooseDay(long chatId, string text)
    {
        if (!DateOnly.TryParse(text, out DateOnly date))
            return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var dayEnd = TimeOnly.Parse(_config.DayEnd);

        if (date < today || (date == today && now > dayEnd))
        {
            await _botClient.SendMessage(chatId, "You can't make a booking to this day.");
            return;
        }

        _schedule.Add(new Slot { ChatId = chatId, Date = date });
        _schedule.SaveChanges();

        _userStates[chatId] = UserState.ChooseTime;

        var times = _schedule.GetOpenSlots(date, _config.DayStart, _config.DayEnd);
        await _botClient.SendMessage(chatId, "Choose day:", replyMarkup: Keyboards.BookingDaysKeyboard(dayEnd));
    }

    private async Task HandleChangeDay(long chatId, string text, Slot slot)
    {
        _userStates.Remove(chatId);
        if (!DateOnly.TryParse(text, out DateOnly date))
            return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var dayEnd = TimeOnly.Parse(_config.DayEnd);

        if (date < today || (date == today && now > dayEnd))
        {
            await _botClient.SendMessage(chatId, "You can't make a booking to this day.");
            return;
        }

        slot.Date = date;
        _schedule.SaveChanges();
        await _botClient.SendMessage(chatId, "Day changed", replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandleChooseTime(long chatId, string text, Slot slot)
    {
        if (!TimeOnly.TryParse(text, out TimeOnly time))
        {
            await _botClient.SendMessage(chatId, "Wrong format.");
            return;
        }

        _schedule.HasBooking(chatId, out Slot? result);
        var openedSlots = _schedule.GetOpenSlots(result.Date, _config.DayStart, _config.DayEnd);

        if (!openedSlots.Contains(time))
        {
            await _botClient.SendMessage(chatId, "You can't make a booking to this time.");
            return;
        }

        slot.Time = time;
        _schedule.SaveChanges();
        _userStates[chatId] = UserState.TypeName;

        await _botClient.SendMessage(chatId, "Type your name.", replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task HandleChangeTime(long chatId, string text, Slot slot)
    {
        _userStates.Remove(chatId);
        if (!TimeOnly.TryParse(text, out TimeOnly time))
        {
            await _botClient.SendMessage(chatId, "Wrong format.");
            return;
        }

        _schedule.HasBooking(chatId, out Slot? result);
        var openedSlots = _schedule.GetOpenSlots(result.Date, _config.DayStart, _config.DayEnd);

        if (!openedSlots.Contains(time))
        {
            await _botClient.SendMessage(chatId, "You can't make a booking to this time.");
            return;
        }

        slot.Time = time;
        try
        {
            _schedule.SaveChanges();
            await _botClient.SendMessage(chatId, "Time changed", replyMarkup: new ReplyKeyboardRemove());
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(chatId, "Something went wrong while saving. Try again later.");
        }
    }

    public async Task Callback(Update update)
    {
        if (update.CallbackQuery?.Message == null) return;

        var callback = update.CallbackQuery;
        long chatId = callback.Message.Chat.Id;

        switch (callback.Data)
        {
            case "booking":
                await HandleBooking(chatId);
                break;

            case "checkBooking":
                await HandleCheckBooking(chatId);
                break;

            case "cancelBooking":
                await HandleCancelBooking(chatId);
                break;

            case "cancelyes":
                await HandleCancelYes(chatId, callback);
                break;

            case "cancelno":
                if (_userStates.ContainsKey(chatId) && _userStates[chatId] == UserState.CancelBooking)
                    _userStates.Remove(chatId);
                break;

            case "changeTime":
                if (!_userStates.ContainsKey(chatId))
                {
                    _userStates.Add(chatId, UserState.ChangeTime);
                    await _botClient.SendMessage(chatId, "Choose new time.", replyMarkup: Keyboards.BookingTimeKeyboard(_schedule.GetOpenSlots(_schedule.getLastBooking(chatId).Date, _config.DayStart, _config.DayEnd)));
                }
                break;

            case "changeDay":
                if (!_userStates.ContainsKey(chatId))
                {
                    _userStates.Add(chatId, UserState.ChangeDay);
                    await _botClient.SendMessage(chatId, "Choose new day.", replyMarkup: Keyboards.BookingDaysKeyboard(TimeOnly.Parse(_config.DayEnd)));
                }
                break;
        }
    }

    private async Task HandleBooking(long chatId)
    {
        if (_schedule.HasBooking(chatId) || _userStates.ContainsKey(chatId))
        {
            await _botClient.SendMessage(chatId, "You have a booking already");
            return;
        }

        _userStates[chatId] = UserState.ChooseDay;
        var keyboard = Keyboards.BookingDaysKeyboard(TimeOnly.Parse(_config.DayEnd));
        await _botClient.SendMessage(chatId, "Choose day:", replyMarkup: keyboard);
    }

    private async Task HandleCheckBooking(long chatId)
    {
        if (_userStates.ContainsKey(chatId)) return;

        if (!_schedule.HasBooking(chatId, out Slot? slot))
        {
            await _botClient.SendMessage(chatId, "You don't have a booking");
            return;
        }

        string text = $"You have a booking.\n{slot.Date}, {slot.Time} | {slot.Name} | {slot.PhoneNum}";
        await _botClient.SendMessage(chatId, text, replyMarkup: Keyboards.BookingMenu);
    }

    private async Task HandleCancelBooking(long chatId)
    {
        if (!_schedule.HasBooking(chatId) || _userStates.ContainsKey(chatId)) return;

        _userStates[chatId] = UserState.CancelBooking;
        await _botClient.SendMessage(chatId, "Are you sure?", replyMarkup: Keyboards.CancelBooking);
    }

    private async Task HandleCancelYes(long chatId, CallbackQuery callback)
    {
        if (!_userStates.ContainsKey(chatId) || _userStates[chatId] != UserState.CancelBooking) return;

        _userStates.Remove(chatId);

        if (_schedule.HasBooking(chatId, out Slot? slot))
        {
            _schedule.Remove(slot);
            _schedule.SaveChanges();

            if (!string.IsNullOrEmpty(_config.AdminId))
                await _botClient.SendMessage(_config.AdminId, $"{callback.From.Username} has cancelled a booking.");

            await _botClient.SendMessage(chatId, "Booking cancelled.", replyMarkup: Keyboards.UserMenu);
        }
    }
}
