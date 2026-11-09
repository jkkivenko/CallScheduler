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
        private const float TwoSevenths = 0.28571428571f;
        private const int NightThenDayPenalty = -800;
        private const int TwiceIn5DaysPenalty = -100;
        private const int TwiceIn4DaysPenalty = -300;
        private const int TwiceIn3DaysPenalty = -400;
        private const int TwiceIn2DaysPenalty = -800;
        private const int OperatingPenalty = -1000;
        private const int ReservedDayPenalty = -2200;
        private const int PreferredDayBonus = 50;
        private const int PreferredOffDayPenalty = -200;
        private const int MoreWeekendsPenalty = -650;
        private const int MoreDaytimePenalty = -650;
        private const int Prefer12Penalty = -200;
        private const int Require12Penalty = -1200;
        internal static int Evaluate(Schedule schedule) => Evaluate(schedule, DebugLevel.None);
        internal static int Evaluate(Schedule schedule, DebugLevel debug)
        {
            int fitness = 0;
            if (debug > DebugLevel.None) Console.WriteLine(schedule);
            // Rule: Nobody works a night shift then a day shift
            for (int i = 1; i < schedule.NumShifts - 1; i += 2)
            {
                if (schedule[i] == schedule[i+1])
                {
                    fitness += NightThenDayPenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} works in the evening and the following morning.", schedule.StartDate.AddDays(i / 2), schedule[i]);
                }
            }

            // Rule: Disincentivize two shifts in close proximity (working two shifts directly in a row is acceptable as long as it does not violate the above) 
            for (int i = 0; i < schedule.NumShifts - 2; i++)
            {
                // Check if doctor works twice in two days (i.e. the next three shifts)
                if (
                    (schedule[i] == schedule[i+2]) ||
                    (i < schedule.NumShifts - 3 && schedule[i] == schedule[i+3])
                    )
                {
                    fitness += TwiceIn2DaysPenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} works twice in a 2-day span.", schedule.StartDate.AddDays(i / 2), schedule[i]);
                }
                else if (
                    (i < schedule.NumShifts - 4 && schedule[i] == schedule[i+4]) ||
                    (i < schedule.NumShifts - 5 && schedule[i] == schedule[i+5])
                    )
                {
                    fitness += TwiceIn3DaysPenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} works twice in a 3-day span.", schedule.StartDate.AddDays(i / 2), schedule[i]);
                }
                else if (
                    (i < schedule.NumShifts - 6 && schedule[i] == schedule[i+6]) ||
                    (i < schedule.NumShifts - 7 && schedule[i] == schedule[i+7])
                    )
                {
                    fitness += TwiceIn4DaysPenalty;
                    if (debug >= DebugLevel.Problems) Print.PrintYellow("On {0}, {1} works twice in a 4-day span.", schedule.StartDate.AddDays(i / 2), schedule[i]);
                }
                else if (
                    (i < schedule.NumShifts - 8 && schedule[i] == schedule[i+8]) ||
                    (i < schedule.NumShifts - 9 && schedule[i] == schedule[i+9])
                    )
                {
                    fitness += TwiceIn5DaysPenalty;
                    if (debug >= DebugLevel.Problems) Print.PrintYellow("On {0}, {1} works twice in a 5-day span.", schedule.StartDate.AddDays(i / 2), schedule[i]);
                }
            }

            // Rule: Every doctor must work 50% day call and 50% night call.
            int[] numDayShifts = new int[Enum.GetValues<Doctor>().Length];
            for (int i = 0; i < schedule.NumShifts; i += 2)
            {
                numDayShifts[(int)schedule[i]]++;
            }
            if (debug > DebugLevel.None) Console.WriteLine("~~~~~ Day Fractions ~~~~~");
            for (int i = 0; i < numDayShifts.Length; i++)
            {
                // Penalty is equal to the square error of the doctor's day fraction compared to 50%
                int idealNumber = (int)Math.Round(schedule.DocShiftCounts[i] * 0.5);
                if (Math.Abs(numDayShifts[i] - idealNumber) > 1)
                {
                    fitness += (int)(MoreDaytimePenalty * Math.Pow(numDayShifts[i] - idealNumber, 2));
                    if (debug > DebugLevel.None) Console.WriteLine("{0} worked {1} day shifts but should work {2}", (Doctor)i, numDayShifts[i], idealNumber);
                }
            }

            // Rule: Every doctor should work approximately two sevenths of their shifts on the weekend.
            int[] numWeekendShifts = new int[Enum.GetValues<Doctor>().Length];
            for (int i = 0; i < schedule.NumShifts; i += 2)
            {
                DateOnly date = schedule.StartDate.AddDays(i / 2);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    numWeekendShifts[(int)schedule[i]]++;
                    numWeekendShifts[(int)schedule[i+1]]++;
                }
                if (date.DayOfWeek == DayOfWeek.Sunday) i += 10; // jumps ahead to next saturday
            }
            if (debug > DebugLevel.None) Console.WriteLine("~~~ Weekend Fractions ~~~");
            for (int i = 0; i < numWeekendShifts.Length; i++)
            {
                // Penalty is equal to the square error of the doctor's weekend fraction compared to 2/7
                int idealNumber = (int)Math.Round(schedule.DocShiftCounts[i] * TwoSevenths);
                if (Math.Abs(numWeekendShifts[i] - idealNumber) > 1)
                {
                    fitness += (int)(MoreWeekendsPenalty * Math.Pow(numWeekendShifts[i] - idealNumber, 2));
                    if (debug > DebugLevel.None) Console.WriteLine("{0} worked {1} weekend shifts but should work {2}", (Doctor)i, numWeekendShifts[i], idealNumber);
                }
            }
            if (debug > DebugLevel.None) Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~");
            // Evaluate the days which are specially reserved off
            fitness += DoctorPreferences.CountAssignedViolations(schedule, debug) * ReservedDayPenalty;
            fitness += DoctorPreferences.CountReservedViolations(schedule, debug) * ReservedDayPenalty;
            // Evaluate every doctor's individual preferences
            for(int i = 0; i < schedule.NumShifts; i++)
            {
                bool isDay = i % 2 == 0;
                Doctor previous = i != 0 ? schedule[i-1] : (Doctor)(-1);
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
                else if (schedule[i] == Doctor.Porisky)   fitness += EvaluatePorisky(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Brown)     fitness += EvaluateBrown(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Chan)      fitness += EvaluateChan(date, isDay, previous, debug);
                else if (schedule[i] == Doctor.Iancu)     fitness += EvaluateIancu(date, isDay, previous, debug);
                // else if (schedule[i] == Doctor.Smith)     fitness += EvaluateSmith(date, isDay, previous, debug);
            }
            return fitness;
        }

        // private static int EvaluateSmith(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
        //     StandardHappiness
        //     (
        //         Doctor.Smith,
        //         [2, 4],
        //         [DayOfWeek.Tuesday],
        //         [],
        //         [],
        //         [],
        //         [],
        //         [],
        //         [],
        //         Preference.Prefer12,
        //         date, isDay, previous, debug
        //     );

        private static int EvaluateIancu(DateOnly date, bool isDay, Doctor previous, DebugLevel debug)
        {
            int happiness = StandardHappiness
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
                Preference.Require12,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [5],
                [DayOfWeek.Tuesday, DayOfWeek.Thursday],
                date, isDay, debug
            );
            if (date > new DateOnly(2027, 02, 15) && !isDay)
            {
                happiness += ReservedDayPenalty;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, Iancu is working a night shift after February 15th.", date);
            }
            return happiness;
        }

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

        private static int EvaluatePorisky(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Porisky,
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
                [DayOfWeek.Wednesday],
                [DayOfWeek.Wednesday],
                [],
                [],
                [],
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
                [DayOfWeek.Saturday],
                Preference.Prefer24,
                date, isDay, previous, debug
            );
            if (date.DayOfWeek == DayOfWeek.Tuesday) {
                numGansTuesdaysThisMonth++;
                if (numGansTuesdaysThisMonth > 2)
                {
                    happiness += ReservedDayPenalty;
                    if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, Gans is working on a Tuesday after already working two Tuesdays that month.", date);
                }
            }
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
                [DayOfWeek.Saturday],
                Preference.Require12,
                date, isDay, previous, debug
            ) +
            ORHappiness
            (
                [2, 4],
                [DayOfWeek.Thursday],
                date, isDay, debug
            ) +
            ORHappiness
            (
                [4],
                [DayOfWeek.Friday],
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
                [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                [DayOfWeek.Tuesday, DayOfWeek.Thursday],
                [],
                [],
                [DayOfWeek.Saturday],
                [DayOfWeek.Saturday, DayOfWeek.Friday],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateRaymond(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Raymond,
                [],
                [],
                [DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Tuesday],
                [DayOfWeek.Thursday],
                [],
                [DayOfWeek.Monday],
                [DayOfWeek.Saturday],
                [DayOfWeek.Wednesday],
                Preference.Require12,
                date, isDay, previous, debug
            );

        private static int EvaluateCram(DateOnly date, bool isDay, Doctor previous, DebugLevel debug) =>
            StandardHappiness
            (
                Doctor.Cram,
                [],
                [],
                [DayOfWeek.Wednesday, DayOfWeek.Monday],
                [DayOfWeek.Tuesday, DayOfWeek.Monday],
                [],
                [],
                [DayOfWeek.Saturday],
                [DayOfWeek.Saturday],
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
                if (debug >= DebugLevel.Problems) Print.PrintYellow("On {0}, {1} is working on a preferred-off morning.", date, doctor);
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
                if (debug >= DebugLevel.Problems) Print.PrintYellow("On {0}, {1} is working on a preferred-off evening.", date, doctor);
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
                    Print.PrintYellow("On {0}, {1} is working a 24-hour shift but they prefer 12-hour shifts.", date, doctor);
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