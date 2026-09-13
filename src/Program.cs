namespace CallSchedulerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            DateOnly start = new(2026, 09, 01);
            DateOnly end = new(2027, 08, 31);
            Schedule schedule = new(start, end);
            schedule.Randomize();
            Console.WriteLine(schedule);
            foreach (KeyValuePair<Doctor, int> kvp in schedule.CountDocShifts())
            {
                Console.WriteLine(kvp);
            }
        }
    }
}