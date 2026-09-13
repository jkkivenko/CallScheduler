namespace CallSchedulerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            DateOnly start = new(2026, 10, 01);
            DateOnly end = new(2026, 11, 01);
            Schedule schedule = new(start, end);
            Console.WriteLine(schedule);
        }
    }
}