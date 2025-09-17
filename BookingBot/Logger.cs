public class Logger
{
    public static void Error(string text)
    {
        Console.BackgroundColor = ConsoleColor.Red;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write("ERROR:");
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" " + text);
        Console.ResetColor();
    }

    public static void Success(string text)
    {
        Console.BackgroundColor = ConsoleColor.Green;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write("SUCCESS:");
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" " + text);
        Console.ResetColor();
    }
}
