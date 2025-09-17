using Telegram.Bot.Types.ReplyMarkups;

class Keyboards
{
    public static InlineKeyboardMarkup AdminMenu = new InlineKeyboardMarkup(
        new List<InlineKeyboardButton[]>
        {
            new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Change Password", "changePass") },
            new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Schedule", "schedule") },
            new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Change time of day start", "timeStart") },
            new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Change time of day end", "timeEnd") }
        });

    public static InlineKeyboardMarkup UserMenu = new InlineKeyboardMarkup(
        new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("Booking", "booking"),
            InlineKeyboardButton.WithCallbackData("Check my booking", "checkBooking")
        });

    public static InlineKeyboardMarkup ChooseDay = new InlineKeyboardMarkup(
        new List<InlineKeyboardButton[]>
        {
            new InlineKeyboardButton[] {
                 InlineKeyboardButton.WithCallbackData("Previous Day", "-1"),
                 InlineKeyboardButton.WithCallbackData("Next Day", "1")
            },
            new InlineKeyboardButton[] {
                 InlineKeyboardButton.WithCallbackData("Today", "today")
            }
        });

    public static ReplyKeyboardMarkup BookingDaysKeyboard(TimeOnly dayEnd)
    {
        DateTime dateTime = DateTime.Now;

        if (TimeOnly.FromDateTime(dateTime) > dayEnd)
            dateTime = dateTime.AddDays(1);

        DateOnly date = DateOnly.FromDateTime(dateTime);

        return new ReplyKeyboardMarkup(new List<KeyboardButton[]>() {
            new KeyboardButton[] {new KeyboardButton(date.ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(1).ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(2).ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(3).ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(4).ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(5).ToString())},
            new KeyboardButton[] {new KeyboardButton(date.AddDays(6).ToString())},
            new KeyboardButton[] {new KeyboardButton("Cancel")}
        });
    }
}
