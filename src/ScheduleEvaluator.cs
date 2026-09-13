namespace CallSchedulerApp.Schedules
{
    internal static class ScheduleEvaluator
    {
        private const int ThreeCallPenalty = -100;
        private const int MoreThanOneDayPerFiveDayPenalty = -100;
        private const int MoreWeekendsPenalty = -1;
        internal static int Evaluate(Schedule schedule, bool debug = false)
        {
            int fitness = 0;
            // Rule 1: Nobody works three calls in a row
            for (int i = 0; i < schedule.Shifts.Length - 2; i++)
            {
                if (schedule[i] == schedule[i+1] && schedule[i] == schedule[i+2])
                {
                    fitness += ThreeCallPenalty;
                    if (debug) Console.WriteLine("At index {0}, {1} works three shifts in a row.", i, schedule[i]);
                }
            }
            // Rule 2: Nobody works twice in a 5-day span
            for (int i = 0; i < schedule.Shifts.Length - 2; i++)
            {
                if (
                    schedule[i] == schedule[i+2] ||
                    (i < schedule.Shifts.Length - 3 && schedule[i] == schedule[i+3]) ||
                    (i < schedule.Shifts.Length - 4 && schedule[i] == schedule[i+4]) ||
                    (i < schedule.Shifts.Length - 5 && schedule[i] == schedule[i+5]) ||
                    (i < schedule.Shifts.Length - 6 && schedule[i] == schedule[i+6]) ||
                    (i < schedule.Shifts.Length - 7 && schedule[i] == schedule[i+7]) ||
                    (i < schedule.Shifts.Length - 8 && schedule[i] == schedule[i+8]) ||
                    (i < schedule.Shifts.Length - 9 && schedule[i] == schedule[i+9]) ||
                    (i < schedule.Shifts.Length - 10 && schedule[i] == schedule[i+10])
                    )
                {
                    fitness += MoreThanOneDayPerFiveDayPenalty;
                    if (debug) Console.WriteLine("At index {0}, {1} works twice in a 5-day span.", i, schedule[i]);
                }
            }
            Dictionary<Doctor, int> numWeekendShifts = [];
            foreach (Doctor doctor in Enum.GetValues<Doctor>()[1..]) numWeekendShifts[doctor] = 0;
            for (int i = 0; i < schedule.Shifts.Length; i += 2)
            {
                DateOnly day = schedule.StartDate.AddDays(i / 2);
                if (day.DayOfWeek == DayOfWeek.Saturday)
                {
                    numWeekendShifts[schedule[i]]++;
                    numWeekendShifts[schedule[i+1]]++;
                }
                if (day.DayOfWeek == DayOfWeek.Sunday)
                {
                    numWeekendShifts[schedule[i]]++;
                    numWeekendShifts[schedule[i+1]]++;
                    i += 6; // jumps ahead to next saturday
                }
            }
            int weekendDifference = numWeekendShifts.Values.Max() - numWeekendShifts.Values.Min() - 1; // TODO: explain this
            fitness += MoreWeekendsPenalty * weekendDifference;
            if (debug && weekendDifference != 0)
            {
                Console.WriteLine("One doctor has {0} more weekend calls than another.", weekendDifference);
                // foreach (KeyValuePair<Doctor, int> kvp in numWeekendShifts) Console.WriteLine(kvp);
            }
            return fitness;
        }
    }
}