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
}