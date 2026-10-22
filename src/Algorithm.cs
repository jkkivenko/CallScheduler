using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public class EvolutionaryAlgorithm
    {
        private DateOnly StartDate { get; }
        private DateOnly EndDate { get; }
        private Schedule[] Schedules { get; set; }
        private int NumSurvivors { get; }
        private float MutationChance { get; }
        private Dictionary<Doctor, int> DocShiftCounts { get; }
        public EvolutionaryAlgorithm(DateOnly startDate, DateOnly endDate, Dictionary<Doctor, float> docShiftLoads, int numSchedules, int numSurvivors, float mutationChance)
        {
            StartDate = startDate;
            EndDate = endDate;
            Schedules = new Schedule[numSchedules];
            NumSurvivors = numSurvivors;
            MutationChance = mutationChance;
            DocShiftCounts = GetShiftCounts(docShiftLoads);
            Initialize();
        }
        private Dictionary<Doctor, int> GetShiftCounts(Dictionary<Doctor, float> docShiftLoads)
        {
            // First we assign each Doctor a number of shifts according to the total number of shifts and their relative shiftload.
            Dictionary<Doctor, int> shiftCounts = [];
            float totalShiftLoad = 0;
            foreach (float shiftLoad in docShiftLoads.Values)
            {
                totalShiftLoad += shiftLoad;
            }
            int numShiftsMissing = 2 * (EndDate.DayNumber - StartDate.DayNumber);
            foreach (KeyValuePair<Doctor, float> kvp in docShiftLoads)
            {
                int numShifts = (int)(kvp.Value / totalShiftLoad * 2 * (EndDate.DayNumber - StartDate.DayNumber));
                shiftCounts[kvp.Key] = numShifts;
                numShiftsMissing -= numShifts;
            }
            // Due to rounding, this can cause the number of assigned shifts to be slightly too low. We add more shifts according to a random lottery to fill up the gaps.
            Doctor[] doctorOrder = [..docShiftLoads.Keys.Shuffle()];
            int numAdditionaShifts = 0;
            while (numShiftsMissing - numAdditionaShifts > 0)
            {
                shiftCounts[doctorOrder[numAdditionaShifts]] += 1;
                numAdditionaShifts++;
            }
            return shiftCounts;
        }
        private void Initialize()
        {
            for (int i = 0; i < Schedules.Length; i++)
            {
                Schedule newSchedule = new(StartDate, EndDate, DocShiftCounts);
                newSchedule.Populate();
                newSchedule.Shuffle();
                Schedules[i] = newSchedule;
                Schedules[i].Fitness = ScheduleEvaluator.Evaluate(Schedules[i]);
            }
        }
        public override string ToString()
        {
            string representation = "";
            foreach(Schedule schedule in Schedules)
            {
                representation += schedule.ToString() + "\n";
            }
            return representation;
        }
        public Schedule GetBest()
        {
            Schedules.Sort();
            return Schedules[^1];
        }
        public bool RunGeneration(int generation)
        {
            // note GetBest sorts Schedules.
            if (GetBest().Fitness >= 0) return false;
            Schedule[] survivors = SelectSurvivors();
            Schedule[] offspring = GenerateOffspring(SelectParents(), generation);
            Schedules = [..survivors, ..offspring];
            MutateAll(Schedules, generation);
            foreach (Schedule schedule in Schedules) if (schedule.ModifiedAt == generation) schedule.Fitness = ScheduleEvaluator.Evaluate(schedule);
            return true;
        }

        private void MutateAll(Schedule[] schedules, int generation)
        {
            foreach (Schedule schedule in schedules)
            {
                if (Random.Shared.NextSingle() < MutationChance)
                {
                    schedule.Mutate();
                    schedule.ModifiedAt = generation;
                }
            }
        }
        /// <summary>
        /// Performs recombination on each pair of parents to create an equal number of offspring.
        /// Corrects for extra shifts. Each doctor shows up in a child schedule the same number of times as they do in the parent schedule.
        /// </summary>
        /// <param name="parents"> An even-length array of schedules to be used as parents for recombination (a.k.a. reproduction). </param>
        /// <returns>An array of offspring with length equal to the number of parents.</returns>
        private Schedule[] GenerateOffspring(Schedule[] parents, int generation)
        {
            Schedule[] offspring = new Schedule[parents.Length];
            for(int i = 0; i < parents.Length; i += 2)
            {
                (offspring[i], offspring[i+1]) = Recombine(parents[i], parents[i+1], generation);
            }
            return offspring;
        }
        /// <summary>
        /// Duplicate-aware cut-and-crossfill recombination. For each pair of schedules in the input, chooses a random crossover point and creates two children:
        /// <list type="bullet">
        ///   <item>Before the crossover point, Child A gets its genome from parent A. After, it gets its genome from parent B.</item>
        ///   <item>Before the crossover point, Child B gets its genome from parent B. After, it gets its genome from parent A.</item>
        /// </list>
        /// Corrects for extra shifts. Each doctor shows up in a child schedule the same number of times as they do in the parent schedule.
        /// </summary>
        /// <param name="parent1">The first parent whose genome will be used to create the offspring.</param>
        /// <param name="parent2">The second parent whose genome will be used to create the offspring.</param>
        /// <returns>A tuple containing two offspring, each of which has a genome are similar to both parents.</returns>
        private (Schedule, Schedule) Recombine(Schedule parent1, Schedule parent2, int generation)
        {
            Schedule offspring1 = new(parent1.StartDate, parent1.EndDate, DocShiftCounts);
            Schedule offspring2 = new(parent1.StartDate, parent1.EndDate, DocShiftCounts);

            offspring1.ModifiedAt = generation;
            offspring2.ModifiedAt = generation;

            int crossoverPoint = Random.Shared.Next() % parent1.Shifts.Length;

            // Console.WriteLine("Parents are:");
            // Console.WriteLine(parent1);
            // Console.WriteLine(parent2);
            // Console.WriteLine("Crossover point is: {0}", crossoverPoint);

            for (int i = 0; i < crossoverPoint; i++)
            {
                offspring1[i] = parent1[i];
                offspring2[i] = parent2[i];
            }

            int numSkippedOffspring1 = 0;
            int numSkippedOffspring2 = 0;
            for(int i = crossoverPoint; i < parent1.Shifts.Length; i++)
            {
                if (offspring2.Shifts.Count(parent1[i]) < DocShiftCounts[parent1[i]])
                {
                    offspring2[i - numSkippedOffspring2] = parent1[i];
                } else
                {
                    // Console.WriteLine("SKIPPING ADDING {0} TO OFFSPRING A", parent1[i]);
                    numSkippedOffspring2++;
                }
                if (offspring1.Shifts.Count(parent2[i]) < DocShiftCounts[parent2[i]])
                {
                    offspring1[i - numSkippedOffspring1] = parent2[i];
                } else
                {
                    // Console.WriteLine("SKIPPING ADDING {0} TO OFFSPRING B", parent2[i]);
                    numSkippedOffspring1++;
                }
            }
            // Console.WriteLine("Before fixing: ");
            // Console.WriteLine(offspring1);
            // Console.WriteLine(offspring2);
            for (int i = 0; i < crossoverPoint; i++)
            {
                int indexToReAdd1 = parent1.Shifts.Length - numSkippedOffspring1;
                int indexToReAdd2 = parent1.Shifts.Length - numSkippedOffspring2;
                if (numSkippedOffspring1 > 0 && offspring1.Shifts.Count(parent2[i]) < DocShiftCounts[parent2[i]])
                {
                    // Console.WriteLine("RE-ADDING {0} to Child A", parent2[i]);
                    offspring1[indexToReAdd1] = parent2[i];
                    numSkippedOffspring1--;
                }
                if (numSkippedOffspring2 > 0 && offspring2.Shifts.Count(parent1[i]) < DocShiftCounts[parent1[i]])
                {
                    // Console.WriteLine("RE-ADDING {0} to Child B", parent1[i]);
                    offspring2[indexToReAdd2] = parent1[i];
                    numSkippedOffspring2--;
                }
            }
            // Console.WriteLine("Children are: ");
            // Console.WriteLine(offspring1);
            // Console.WriteLine(offspring2);
            return (offspring1, offspring2);
        }

        private Schedule[] SelectSurvivors()
        {
            return Schedules[^NumSurvivors..];
        }
        private Schedule[] SelectParents()
        {
            int numParents = Schedules.Length - NumSurvivors;
            return Schedules[^numParents..];
        }

    }
}