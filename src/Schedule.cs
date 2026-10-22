namespace CallSchedulerApp.Schedules
{
    /// <summary>
    /// A class representing a 4-month call schedule. Each day is associated with two call shifts. 
    /// </summary>
    public class Schedule(DateOnly startDate, DateOnly endDate, Dictionary<Doctor, int> docShiftCounts) : IComparable<Schedule>
    {
        public DateOnly StartDate { get; set; } = startDate;
        public DateOnly EndDate { get; set; } = endDate;
        public int ModifiedAt { get; set; }
        public int Fitness { get; set; }
        public Dictionary<Doctor, int> DocShiftCounts { get; set; } = docShiftCounts;

        internal Doctor[] Shifts { get; set; } = new Doctor[(endDate.DayNumber - startDate.DayNumber) * 2];
        /// <summary>
        /// Fills the Schedule with shifts.
        /// </summary>
        /// <exception cref="ArgumentException">If the number of shifts assigned to all doctors does not sum to the length of the Schedule.</exception>
        public void Populate()
        {
            // First verify that the number of shifts to be assigned to each doctor adds up to the total number of shifts to be assigned.
            int numTotalShifts = 0;
            foreach (int value in DocShiftCounts.Values) numTotalShifts += value;
            if (numTotalShifts != Shifts.Length) throw new ArgumentException("The number of shifts assigned to all doctors is invalid for this schedule length.");
            int i = 0;
            foreach (KeyValuePair<Doctor, int> kvp in DocShiftCounts)
            {
                Array.Fill(Shifts, kvp.Key, i, kvp.Value);
                i += kvp.Value;
            }
        }
        /// <summary>
        /// Shuffles the schedule so the shifts are in a pseudo-random order. The number of shifts assigned to each doctor does not change.
        /// </summary>
        public void Shuffle()
        {
            Random random = new();
            random.Shuffle(Shifts);
        }
        /// <summary>
        /// A simple unary swap mutation. Chooses two random shifts and swaps their doctors.
        /// </summary>
        internal void Mutate()
        {
            int index1 = Random.Shared.Next(Shifts.Length);
            int index2 = Random.Shared.Next(Shifts.Length);
            (Shifts[index2], Shifts[index1]) = (Shifts[index1], Shifts[index2]); // Tuple swap
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