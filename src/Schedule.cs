namespace CallSchedulerApp
{
    /// <summary>
    /// A class representing a 4-month call schedule. Each day is associated with two call shifts. 
    /// </summary>
    class Schedule(DateOnly startDate, DateOnly endDate)
    {
        public DateOnly StartDate { get; set; } = startDate;
        public DateOnly EndDate { get; set; } = endDate;
        private readonly Doctor[] shifts = new Doctor[(endDate.DayNumber - startDate.DayNumber) * 2];
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