using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

enum UserState
{
    ChooseDay,
    ChooseTime,
    TypeName,
    TypePhone
}

public class UserHandler
{
    ITelegramBotClient _botClient;
    BotConfig _config;
    Schedule _schedule;

    Dictionary<long, UserState> _userStates;

    string[] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    public UserHandler(ITelegramBotClient botClient, BotConfig config, Schedule schedule)
    {
        _botClient = botClient;
        _config = config;
        _schedule = schedule;

        _userStates = new Dictionary<long, UserState>();
    }

    public async Task Message(Update update)
    {
        if (update.Message == null) return;
        if (update.Message.Text == null) return;

        Message msg = update.Message;
        long chatId = msg.Chat.Id;

        if (!_userStates.ContainsKey(chatId))
        {
            if (msg.Text == _config.AdminPass)
            {
                _config.AdminId = msg.Chat.Id.ToString();
                _config.Save();
                Console.WriteLine($"{msg?.From?.Username} became an admin.");
                await _botClient.SendMessage(chatId, "You became an admin. /menu");
            }

            else if (msg.Text == "/start")
                await _botClient.SendMessage(chatId, "Menu", replyMarkup: Keyboards.UserMenu);

            return;
        }

        if (msg.Text == "Cancel")
        {
            _userStates.Remove(chatId);
            _schedule.Remove(chatId);
            _schedule.SaveChanges();
        }

        Slot slot = new Slot();
        if (_userStates.ContainsKey(chatId))
            slot = _schedule.Slots.Where((p) => p.ChatId == chatId).OrderBy((p) => p.Date).ThenBy((p) => p.Time).Last();

        switch (_userStates[chatId])
        {
            case UserState.ChooseDay:
                if (DateOnly.TryParse(msg.Text, out DateOnly date))
                {
                    if ((date < DateOnly.FromDateTime(DateTime.Now)) ||
                            (date == DateOnly.FromDateTime(DateTime.Now) &&
                            TimeOnly.FromDateTime(DateTime.Now) > TimeOnly.Parse(_config.DayEnd)))
                        await _botClient.SendMessage(chatId, "You can't make a booking to this day.");
                    else
                    {
                        _schedule.Add(new Slot() { ChatId = chatId, Date = date });
                        _schedule.SaveChanges();
                        _userStates[chatId] = UserState.ChooseTime;
                        await _botClient.SendMessage(chatId, $"Choose time:\nWorking hours: {_config.DayStart} - {_config.DayEnd}");
                    }
                }
                break;

            case UserState.ChooseTime:
                if (TimeOnly.TryParse(msg.Text, out TimeOnly time))
                {
                    if ((time < TimeOnly.Parse(_config.DayStart) || time > TimeOnly.Parse(_config.DayEnd))
                            || (false))
                        await _botClient.SendMessage(chatId, "You can't make a booking to this time.");
                    else
                    {
                        slot.Time = time;
                        _schedule.SaveChanges();
                        _userStates[chatId] = UserState.ChooseTime;
                    }
                }
                break;

            case UserState.TypeName:
                slot.Name = msg.Text;
                _schedule.SaveChanges();
                _userStates[chatId] = UserState.TypePhone;
                break;
            
            case UserState.TypePhone:

                slot.PhoneNum = msg.Text;
                _schedule.SaveChanges();
                _userStates.Remove(chatId);

                Console.WriteLine($"{msg.From.Username} has made a booking");
                await _botClient.SendMessage(_config.AdminId, $"{msg.From.Username} has made a booking");
                await _botClient.SendMessage(chatId, $"You made a booking.");
                break;
        }
    }

    public async Task Callback(Update update)
    {
        
        if (update.CallbackQuery == null) return;
        if (update.CallbackQuery.Message == null) return;

        CallbackQuery callback = update.CallbackQuery;
        long chatId = callback.Message.Chat.Id;

        if (_userStates.ContainsKey(chatId)) return;

        switch (callback.Data)
        {
            case "booking":
                /*if (_schedule.Slots.Where((p) => p.ChatId == chatId).OrderBy((p) => p.Date).ThenBy((p) => p.Time).Last() != null)
                {
                    await _botClient.SendMessage(chatId, "You have a booking already");
                    break;
                }*/
                _userStates.Add(chatId, UserState.ChooseDay);
                var chooseDay = Keyboards.BookingDaysKeyboard(TimeOnly.Parse(_config.DayEnd));
                await _botClient.SendMessage(chatId, "Choose day:", replyMarkup: chooseDay);
                break;

            case "checkBooking":
                Slot slot = _schedule.Slots.Where((p) => p.ChatId == chatId).OrderBy((p) => p.Date).ThenBy((p) => p.Time).Last();

                if (slot.Date < DateOnly.FromDateTime(DateTime.Now))
                    await _botClient.SendMessage(chatId, "You don't have a booking");

                else
                    await _botClient.SendMessage(chatId, $"You have a booking.\n{slot.Date}, {slot.Time} | {slot.Name} | {slot.PhoneNum}");

                break;
        }
    }
}
