using System.Text.Json;
using CallSchedulerApp.Printing;
using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public static class DoctorPreferences
    {
        private static readonly Doctor[] AssignedShifts;
        private static readonly Doctor[][] ReservedShifts;
        private static JsonSerializerOptions jsonSerializerOptions = new() {WriteIndented = true};
        private const string AssignedShiftsFilePath = "data\\assigned_shifts.json";
        private const string ReservedShiftsFilePath = "data\\reserved_shifts.json";

        static DoctorPreferences()
        {
            string assignedShiftJson = File.ReadAllText(AssignedShiftsFilePath);
            AssignedShifts = JsonSerializer.Deserialize<Doctor[]>(assignedShiftJson, jsonSerializerOptions) ?? throw new JsonException("Assigned shift json is invalid.");
            string reservedShiftJson = File.ReadAllText(ReservedShiftsFilePath);
            ReservedShifts = JsonSerializer.Deserialize<Doctor[][]>(reservedShiftJson, jsonSerializerOptions) ?? throw new JsonException("Reserved shift json is invalid.");
        }

        public static int CountAssignedViolations(Schedule schedule, DebugLevel debug)
        {
            int numViolations = 0;
            for (int i = 0; i < schedule.NumShifts; i++) if (AssignedShifts[i] != (Doctor)(-1) && AssignedShifts[i] != schedule[i])
            {
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} is working a shift specially assigned to {2}.", schedule.StartDate.AddDays(i / 2), schedule[i], AssignedShifts[i]);
                numViolations++;
            }
            return numViolations;
        }
        public static int CountReservedViolations(Schedule schedule, DebugLevel debug)
        {
            int numViolations = 0;
            for (int i = 0; i < schedule.NumShifts; i++) if (ReservedShifts[i].Contains(schedule[i]))
            {
                numViolations++;
                if (debug >= DebugLevel.Violations) Print.PrintRed("On {0}, {1} is working a shift they reserved off", schedule.StartDate.AddDays(i / 2), schedule[i]);
            }
            return numViolations;
        }
    }
}