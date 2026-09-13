namespace CallSchedulerApp.Schedules
{
    /// <summary>
    /// A class representing a 4-month call schedule. Each day is associated with two call shifts. 
    /// </summary>
    public class Schedule(DateOnly startDate, DateOnly endDate) : IComparable<Schedule>
    {
        public DateOnly StartDate { get; set; } = startDate;
        public DateOnly EndDate { get; set; } = endDate;
        public int Fitness { get; set; }

        internal Doctor[] Shifts { get; } = new Doctor[(endDate.DayNumber - startDate.DayNumber) * 2];
        public void Randomize()
        {
            for (int i = 0; i < Shifts.Length; i++)
            {
                Shifts[i] = (Doctor)((i % (Enum.GetValues<Doctor>().Length - 1))+1);
            }
            Random random = new();
            random.Shuffle(Shifts);
        }
        public Dictionary<Doctor, int> CountDocShifts()
        {
            Dictionary<Doctor, int> doctorFrequency = [];
            foreach (Doctor doctor in Shifts)
            {
                if (doctorFrequency.TryGetValue(doctor, out int value)) doctorFrequency[doctor] = value + 1;
                else doctorFrequency[doctor] = 1;
            }
            return doctorFrequency;
        }
        /// <summary>
        /// A simple unary swap mutation. Chooses two random shifts and swaps their doctors.
        /// </summary>
        internal void Mutate()
        {
            int index1 = Random.Shared.Next(Shifts.Length);
            int index2 = Random.Shared.Next(Shifts.Length);
            (Shifts[index2], Shifts[index1]) = (Shifts[index1], Shifts[index2]); // Tuple swap
            Fitness = ScheduleEvaluator.Evaluate(this);
        }
        public Doctor this[int i]
        {
            get => Shifts[i];
            set => Shifts[i] = value;
        }
        public override string ToString()
        {
            string representation = "";
            for (int i = 0; i < Shifts.Length; i += 2)
            {
                DateOnly date = StartDate.AddDays(i / 2);
                if (date.DayOfWeek == DayOfWeek.Sunday) representation += "\n";
                representation += date.ToString() + ":\t" + Shifts[i].ToString() + ", " + Shifts[i+1].ToString() + "\n";
            }
            representation += "Fitness: " + Fitness.ToString();
            return representation;
        }
        public int CompareTo(Schedule? other)
        {
            if (other == null) return 1;
            return Fitness - other.Fitness;
        }
    }
}