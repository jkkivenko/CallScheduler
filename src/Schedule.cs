namespace CallSchedulerApp
{
    /// <summary>
    /// A class representing a 4-month call schedule. Each day is associated with two call shifts. 
    /// </summary>
    class Schedule(DateOnly startDate, DateOnly endDate)
    {
        public DateOnly StartDate { get; set; } = startDate;
        public DateOnly EndDate { get; set; } = endDate;
        private readonly Doctor[] shifts = new Doctor[(endDate.DayNumber - startDate.DayNumber + 1) * 2];
        public void Randomize()
        {
            Doctor[] possibleDocs = Enum.GetValues<Doctor>()[1..];
            Random random = new();
            for (int i = 0; i < shifts.Length; i++)
            {
                shifts[i] = possibleDocs[i % possibleDocs.Length];
            }
            random.Shuffle(shifts);
        }
        public Dictionary<Doctor, int> CountDocShifts()
        {
            Dictionary<Doctor, int> doctorFrequency = [];
            foreach (Doctor doctor in shifts)
            {
                if (doctorFrequency.TryGetValue(doctor, out int value)) doctorFrequency[doctor] = value + 1;
                else doctorFrequency[doctor] = 1;
            }
            return doctorFrequency;
        }
        public override string ToString()
        {
            string representation = "";
            for (int i = 0; i < shifts.Length; i += 2)
            {
                DateOnly date = StartDate.AddDays(i / 2);
                representation += date.ToString() + ":\t" + shifts[i].ToString() + ", " + shifts[i+1].ToString() + "\n";
            }
            return representation;
        }
    }
}