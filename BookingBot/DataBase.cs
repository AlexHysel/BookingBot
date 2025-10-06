using Microsoft.EntityFrameworkCore;

public class Slot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNum { get; set; } = string.Empty;
    public long ChatId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
}

public class Schedule : DbContext
{
    public DbSet<Slot> Slots { get; set; }
    public Schedule() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=schedule.db");
    }

    public string GetText(int day)
    {
        string text = String.Empty;
        DateTime choosenDate = DateTime.Now.AddDays(day);
        IEnumerable<Slot> daySchedule = GetDaySchedule(day);

        if (day == 0) text = "Today, ";
        else if (day == 1) text = "Tomorrow, ";
        else if (day == -1) text = "Yesterday, ";
        text += $"{choosenDate.Day}.{choosenDate.Month}.{choosenDate.Year}, ";
        text += choosenDate.DayOfWeek.ToString() + "\n";

        foreach (Slot slot in daySchedule)
            text += $"{slot.Date}, {slot.Time} | {slot.Name} | {slot.PhoneNum}\n";

        return text;
    }

    public IEnumerable<Slot> GetDaySchedule(int day)
    {
        IEnumerable<Slot> result = new List<Slot>();
        result = Slots.Where((p) => p.Date == DateOnly.FromDateTime(DateTime.Today.AddDays(day)));
        return result;
    }

    public bool HasBooking(long chatId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        IEnumerable<Slot> currSlots = Slots.Where((p) => p.Date >= today);
        if (currSlots.Count() == 0) return false;
        foreach (Slot slot in currSlots)
            if (slot.ChatId == chatId) return true;
        return false;
    }

    public bool HasBooking(long chatId, out Slot? result)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        IEnumerable<Slot> currSlots = Slots.Where((p) => p.Date >= today);
        result = null;
        if (currSlots.Count() == 0)
            return false;
        foreach (Slot slot in currSlots)
            if (slot.ChatId == chatId)
            {
                result = slot;
                return true;
            }
        return false;
    }

    public bool getLastBooking(long chatId, out Slot slot)
    {
        if (Slots.Count() > 0)
        {
            IEnumerable<Slot> userBookings = from p in Slots where p.ChatId == chatId select p;
            if (userBookings.Count() > 0)
            {
                slot = userBookings.MaxBy(x => x.Date);
                return true;
            }
        }
        slot = null;
        return false;
    }

    public Slot? getLastBooking(long chatId)
    {
        if (Slots.Count() > 0)
        {
            IEnumerable<Slot> userBookings = from p in Slots where p.ChatId == chatId select p;
            if (userBookings.Count() > 0)
            {
                Slot? slot = userBookings.MaxBy(x => x.Date);
                return slot;
            }
        }
        return null;
    }

    public List<TimeOnly> GetOpenSlots(DateOnly date, string dayStart, string dayEnd)
    {
        List<TimeOnly> openSlots = new List<TimeOnly>();
        TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.Now);
        DateOnly currentDay = DateOnly.FromDateTime(DateTime.Now);

        IEnumerable<TimeOnly> todaySlots = from p in Slots where p.Date == date select p.Time;
        TimeOnly currentSlot = TimeOnly.Parse(dayStart);
        TimeOnly End = TimeOnly.Parse(dayEnd);

        while (currentSlot < TimeOnly.Parse(dayEnd))
        {
            if (!todaySlots.Contains(currentSlot) && !(currentDay == date && currentSlot < currentTime))
                openSlots.Add(currentSlot);
            currentSlot = currentSlot.AddMinutes(30);
        }
        return openSlots;
    }
}