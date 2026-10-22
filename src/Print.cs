namespace CallSchedulerApp.Printing
{
    public static class Print
    {
        public static void PrintRed(string message) => PrintColor(message, ConsoleColor.Red);
        public static void PrintRed(string message, object? arg0) => PrintColor(message, arg0, ConsoleColor.Red);
        public static void PrintRed(string message, object? arg0, object? arg1) => PrintColor(message, arg0, arg1, ConsoleColor.Red);
        public static void PrintGreen(string message) => PrintColor(message, ConsoleColor.Green);
        public static void PrintGreen(string message, object? arg0) => PrintColor(message, arg0, ConsoleColor.Green);
        public static void PrintGreen(string message, object? arg0, object? arg1) => PrintColor(message, arg0, arg1, ConsoleColor.Green);
        public static void PrintOrange(string message) => PrintColor(message, ConsoleColor.DarkYellow);
        public static void PrintOrange(string message, object? arg0) => PrintColor(message, arg0, ConsoleColor.DarkYellow);
        public static void PrintOrange(string message, object? arg0, object? arg1) => PrintColor(message, arg0, arg1, ConsoleColor.DarkYellow);
        private static void PrintColor(string message, object? arg0, object? arg1, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message, arg0, arg1);
            Console.ResetColor();
        }
        private static void PrintColor(string message, object? arg0, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message, arg0);
            Console.ResetColor();
        }
        private static void PrintColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}