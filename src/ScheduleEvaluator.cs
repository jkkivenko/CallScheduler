using System.Diagnostics;
using CallSchedulerApp.Printing;

namespace CallSchedulerApp.Schedules
{
    public enum DebugLevel
    {
        None,
        Violations,
        Problems,
        All
    }
    internal static class ScheduleEvaluator
    {
        private enum Preference
        {
            Prefer12,
            Require12,
            Prefer24,
            Require24
        };
        private const int NightThenDayPenalty = -800;
        private const int MoreThanOneDayPerFivePenalty = -3500;
        private const int OperatingPenalty = -1000;
        private const int ReservedDayPenalty = -1200;
        private const int PreferredDayBonus = 100;
        private const int PreferredOffDayPenalty = -200;
        private const int MoreWeekendsPenalty = -5500;
        private const int Prefer12Penalty = -200;
        private const int Require12Penalty = -3600;
        internal static int Evaluate(Schedule schedule, DebugLevel debug = 0)
        {
            int fitness = 0;
            // Rule 1: Nobody works night and then day
            for (int i = 1; i < schedule.Shifts.Length - 1; i += 2)
            {
                if (schedule[i] == schedule[i+1])
                {
                    fitness += NightThenDayPenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} works in the evening and the following morning.", schedule.StartDate.AddDays(i), schedule[i]);
                }
            }
            // Rule 2: Nobody works twice in a 5-day span (working two calls directly in a row is acceptable) 
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
                    fitness += MoreThanOneDayPerFivePenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} works twice in a 5-day span.", schedule.StartDate.AddDays(i), schedule[i]);
                }
            }
            Dictionary<Doctor, float> weekendShiftFraction = [];
            foreach (Doctor doctor in Enum.GetValues<Doctor>()[1..]) weekendShiftFraction[doctor] = 0;
            for (int i = 0; i < schedule.Shifts.Length; i += 2)
            {
                DateOnly day = schedule.StartDate.AddDays(i / 2);
                if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                {
                    weekendShiftFraction[schedule[i]] += 1f / schedule.DocShiftCounts[schedule[i]];
                    weekendShiftFraction[schedule[i+1]] += 1f / schedule.DocShiftCounts[schedule[i+1]];
                }
                if (day.DayOfWeek == DayOfWeek.Sunday) i += 10; // jumps ahead to next saturday
            }
            float weekendFractionDifference = weekendShiftFraction.Values.Max() - weekendShiftFraction.Values.Min();
            fitness += (int)(MoreWeekendsPenalty * weekendFractionDifference);
            if (weekendFractionDifference > 0.3 && debug >= DebugLevel.Violations)
                {
                    Print.PrintRed("One doctor has {0}% weekend shifts while another has {1}%.", weekendShiftFraction.Values.Max() * 100, weekendShiftFraction.Values.Min() * 100);
                    foreach (KeyValuePair<Doctor, float> kvp in weekendShiftFraction) Print.PrintRed(kvp.ToString());
                }
            else if (weekendFractionDifference > 0.1 && debug >= DebugLevel.Problems)
            {
                Print.PrintOrange("One doctor has {0}% weekend shifts while another has {1}%.", weekendShiftFraction.Values.Max() * 100, weekendShiftFraction.Values.Min() * 100);
                foreach (KeyValuePair<Doctor, float> kvp in weekendShiftFraction) Print.PrintOrange(kvp.ToString());
            }
            else if (debug >= DebugLevel.All)
            {
                foreach (KeyValuePair<Doctor, float> kvp in weekendShiftFraction) Console.WriteLine(kvp.ToString());
            }
            // Evaluate every doctor's individual preferences
            for(int i = 0; i < schedule.Shifts.Length; i++)
            {
                bool isDay = i % 2 == 0;
                Doctor previous = i != 0 ? schedule[i-1] : Doctor.None;
                DateOnly date = schedule.StartDate.AddDays(i / 2);
                if (date.Day == 1 && isDay) numGansTuesdaysThisMonth = 0;
                if      (schedule[i] == Doctor.Cram)      fitness += EvaluateCram(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Raymond)   fitness += EvaluateRaymond(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Frecker)   fitness += EvaluateFrecker(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Mackenzie) fitness += EvaluateMackenzie(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Aggarwal)  fitness += EvaluateAggarwal(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Gans)      fitness += EvaluateGans(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Tai)       fitness += EvaluateTai(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Badmos)    fitness += EvaluateBadmos(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Jolliffe)  fitness += EvaluateJolliffe(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Proisky)   fitness += EvaluateProisky(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Brown)     fitness += EvaluateBrown(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Chan)      fitness += EvaluateChan(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Iancu)     fitness += EvaluateIancu(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Smith)     fitness += EvaluateSmith(date, isDay, previous, debug);
            }
            return fitness;
        }

        private static int EvaluateSmith(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Smith,
                [2, 4],
                [DayOfWeek.Tuesday],
                [],
                [],
                [],
                [],
                [],
                [],
                Preference.Prefer12,
                date, isDay, previous, debug
            );

        private static int EvaluateIancu(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Iancu,
                [2],
                [DayOfWeek.Friday],
                [],
                [],
                [],
                [],
                [],
                [],
                Preference.Prefer12,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [5],
                [DayOfWeek.Tuesday, DayOfWeek.Thursday],
                date, isDay, debug
            );

        private static int EvaluateChan(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Chan,
                [],
                [],
                [DayOfWeek.Sunday],
                [DayOfWeek.Sunday],
                [DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
                [DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
                [],
                [],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateBrown(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Brown,
                [2],
                [DayOfWeek.Thursday],
                [],
                [],
                [DayOfWeek.Wednesday, DayOfWeek.Thursday],
                [DayOfWeek.Wednesday, DayOfWeek.Thursday],
                [],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [1, 2, 3, 4, 5],
                [DayOfWeek.Tuesday],
                date, isDay, debug
            );

        private static int EvaluateProisky(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Proisky,
                [1, 3],
                [DayOfWeek.Monday],
                [],
                [],
                [],
                [],
                [],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            );

        private static int EvaluateJolliffe(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Jolliffe,
                [2],
                [DayOfWeek.Tuesday, DayOfWeek.Wednesday],
                [],
                [],
                [],
                [],
                [],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [4],
                [DayOfWeek.Thursday],
                date, isDay, debug
            );

        private static int EvaluateBadmos(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Badmos,
                [1, 2, 3, 4, 5],
                [DayOfWeek.Monday],
                [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday],
                [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday],
                [],
                [],
                [],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            );

        private static int EvaluateTai(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Tai,
                [1],
                [DayOfWeek.Friday],
                [],
                [],
                [],
                [],
                [DayOfWeek.Wednesday],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [5],
                [DayOfWeek.Thursday],
                date, isDay, debug
            ) +
            ORHappiness
            (
                [2,4,5],
                [DayOfWeek.Monday],
                date, isDay, debug
            ) +
            ORHappiness
            (
                [1,3],
                [DayOfWeek.Tuesday],
                date, isDay, debug
            );

        private static int numGansTuesdaysThisMonth = 0;
        private static int EvaluateGans(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) {
            int happiness = StandardHappiness(
                Doctor.Gans,
                [],
                [],
                [DayOfWeek.Wednesday],
                [],
                [DayOfWeek.Tuesday],
                [],
                [DayOfWeek.Saturday],
                [],
                Preference.Prefer24,
                date, isDay, previous, debug
            );
            if (date.DayOfWeek == DayOfWeek.Tuesday) numGansTuesdaysThisMonth++;
            if (numGansTuesdaysThisMonth > 2) happiness += ReservedDayPenalty; 
            return happiness;
        }

        private static int EvaluateAggarwal(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Aggarwal,
                [1, 3],
                [DayOfWeek.Wednesday],
                [DayOfWeek.Wednesday],
                [],
                [],
                [],
                [DayOfWeek.Saturday],
                [],
                Preference.Require12,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [2, 4],
                [DayOfWeek.Thursday],
                date, isDay, debug
            );

        private static int EvaluateMackenzie(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Mackenzie,
                [1, 2],
                [DayOfWeek.Wednesday],
                [DayOfWeek.Wednesday],
                [],
                [],
                [],
                [DayOfWeek.Saturday],
                [],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateFrecker(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Frecker,
                [1, 2],
                [DayOfWeek.Thursday],
                [DayOfWeek.Wednesday, DayOfWeek.Friday],
                [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
                [],
                [],
                [DayOfWeek.Saturday],
                [],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateRaymond(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Raymond,
                [],
                [],
                [DayOfWeek.Thursday, DayOfWeek.Friday],
                [DayOfWeek.Thursday],
                [],
                [DayOfWeek.Monday],
                [DayOfWeek.Saturday],
                [],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateCram(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Cram,
                [],
                [],
                [DayOfWeek.Wednesday],
                [DayOfWeek.Tuesday],
                [],
                [],
                [DayOfWeek.Saturday],
                [],
                Preference.Prefer12,
                date, isDay, previous, debug
            );

        private static int StandardHappiness(Doctor doctor, int[] operatingWeeks, DayOfWeek[] operatingDays, DayOfWeek[] reservedDays, DayOfWeek[] reservedNights,
                                             DayOfWeek[] preferredDays, DayOfWeek[] preferredNights, DayOfWeek[] preferredOffDays, DayOfWeek[] preferredOffNights,
                                             Preference preference, DateOnly date, bool isDay, Doctor previous, DebugLevel debug)
        {
            int happiness = 0;
            // Apply penalty if the call would interfere with the doctor's operating schedule
            happiness += ORHappiness(operatingWeeks, operatingDays, date, isDay, debug);
            // Apply penalty if it is a reserved morning
            if (reservedDays.Contains(date.DayOfWeek) && isDay)
            {
                happiness += ReservedDayPenalty;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} is working during a reserved morning.", date, doctor);
            }
            // Apply bonus if this is a preferred morning
            if (preferredDays.Contains(date.DayOfWeek) && isDay)
            {
                happiness += PreferredDayBonus;
                if (debug >= DebugLevel.All) Print.PrintGreen("On {0}, {1} is working during a preferred morning.", date, doctor);
            }
            // Apply penalty if it is a preferred-off morning
            if (preferredOffDays.Contains(date.DayOfWeek) && isDay)
            {
                happiness += PreferredOffDayPenalty;
                if (debug >= DebugLevel.Problems) Print.PrintOrange("On {0}, {1} is working on a preferred-off morning.", date, doctor);
            }
            // Apply bonus if this is a preferred evening
            if (preferredNights.Contains(date.DayOfWeek) && !isDay)
            {
                happiness += PreferredDayBonus;
                if (debug >= DebugLevel.All) Print.PrintGreen("On {0}, {1} is working during a preferred evening.", date, doctor);
            }
            // Apply penalty if it is a preferred-off evening
            if (preferredOffNights.Contains(date.DayOfWeek) && !isDay)
            {
                happiness += PreferredOffDayPenalty;
                if (debug >= DebugLevel.Problems) Print.PrintOrange("On {0}, {1} is working on a preferred-off evening.", date, doctor);
            }
            // Apply penalty if it is a reserved evening
            if (reservedNights.Contains(date.DayOfWeek) && !isDay)
            {
                happiness += ReservedDayPenalty;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} is working during a reserved evening.", date, doctor);
            }
            // We add this extra penalty in order to keep numbers around zero. If this is a 24 hour shift, the penalty will exactly cancel out.
            if (preference == Preference.Prefer24) happiness += Prefer12Penalty / 2;
            if (preference == Preference.Require24) happiness += Require12Penalty / 2;
            // Apply penalty or bonus depending on if this is a 24-hour shift and whether the doctor likes 24-hour shifts
            if (doctor == previous)
            {
                if      (preference == Preference.Prefer12)  happiness += Prefer12Penalty;
                else if (preference == Preference.Require12) happiness += Require12Penalty;
                else if (preference == Preference.Prefer24)  happiness -= Prefer12Penalty;
                else if (preference == Preference.Require24) happiness -= Require12Penalty;
                if (debug >= DebugLevel.Violations && preference == Preference.Require12)
                    Print.PrintRed("On {0}, {1} is working a 24-hour shift but they only accept 12-hour shifts.", date, doctor);
                if (debug >= DebugLevel.Problems && preference == Preference.Prefer12)
                {
                    Print.PrintOrange("On {0}, {1} is working a 24-hour shift but they prefer 12-hour shifts.", date, doctor);
                }
            }
            return happiness;
        }
        private static int ORHappiness(int[] operatingWeeks, DayOfWeek[] operatingDays, DateOnly date, bool isDay, DebugLevel debug)
        {
            int happiness = 0;
            int weekOfMonth = (int)Math.Ceiling(date.Day / 7f);
            int tomorrowsWeekOfMonth = (int)Math.Ceiling(date.AddDays(1).Day / 7f);
            // Apply penalty if it is the morning of an operating day
            if (operatingWeeks.Contains(weekOfMonth) && operatingDays.Contains(date.DayOfWeek) && isDay)
            {
                happiness += OperatingPenalty;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, a doctor is working on a morning they are operating.", date);
            }
            // Apply penalty if it is the evening before of an operating day
            if (operatingWeeks.Contains(tomorrowsWeekOfMonth) && operatingDays.Contains(date.AddDays(1).DayOfWeek) && !isDay)
            {
                happiness += OperatingPenalty;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, a doctor is working the night before they are operating.", date);
            }
            return happiness;
        }
    }
}