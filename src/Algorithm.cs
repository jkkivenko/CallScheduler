using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public class EvolutionaryAlgorithm
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;
        private Schedule[] schedules;
        private readonly int numSurvivors;
        private readonly float mutationChance;
        public Dictionary<Doctor, int> NumShiftsPerDoc = [];
        public EvolutionaryAlgorithm(DateOnly startDate, DateOnly endDate, int numSchedules, int numSurvivors, float mutationChance)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            schedules = new Schedule[numSchedules];
            this.numSurvivors = numSurvivors;
            this.mutationChance = mutationChance;
            Initialize();
            foreach (KeyValuePair<Doctor, int> kvp in NumShiftsPerDoc) Console.WriteLine(kvp);
        }
        private void Initialize()
        {
            for (int i = 0; i < schedules.Length; i++)
            {
                Schedule newSchedule = new(startDate, endDate);
                newSchedule.Randomize();
                NumShiftsPerDoc = newSchedule.CountDocShifts();
                schedules[i] = newSchedule;
                schedules[i].Fitness = ScheduleEvaluator.Evaluate(schedules[i]);
            }
        }
        public override string ToString()
        {
            string representation = "";
            foreach(Schedule schedule in schedules)
            {
                representation += schedule.ToString() + "\n";
            }
            return representation;
        }
        public Schedule GetBest()
        {
            schedules.Sort();
            return schedules[^1];
        }
        public bool RunGeneration()
        {
            schedules.Sort();
            if (schedules[^1].Fitness >= 0) return false;
            Schedule[] survivors = SelectSurvivors();
            Schedule[] parents = SelectParents();
            schedules = [..survivors, ..GenerateOffspring(parents)];
            MutateAll();
            return true;
        }

        private void MutateAll()
        {
            foreach (Schedule schedule in schedules)
            {
                if (Random.Shared.NextSingle() < mutationChance) schedule.Mutate();
            }
        }
        /// <summary>
        /// Performs recombination on each pair of parents to create an equal number of offspring.
        /// Corrects for extra shifts. Each doctor shows up in a child schedule the same number of times as they do in the parent schedule.
        /// </summary>
        /// <param name="parents"> An even-length array of schedules to be used as parents for recombination (a.k.a. reproduction). </param>
        /// <returns>An array of offspring with length equal to the number of parents.</returns>
        private Schedule[] GenerateOffspring(Schedule[] parents)
        {
            Schedule[] offspring = new Schedule[parents.Length];
            for(int i = 0; i < parents.Length; i += 2)
            {
                (offspring[i], offspring[i+1]) = Recombine(parents[i], parents[i+1]);

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
        private (Schedule, Schedule) Recombine(Schedule parent1, Schedule parent2)
        {
            Schedule offspring1 = new(parent1.StartDate, parent1.EndDate);
            Schedule offspring2 = new(parent1.StartDate, parent1.EndDate);


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
                if (offspring2.Shifts.Count(parent1[i]) < NumShiftsPerDoc[parent1[i]])
                {
                    offspring2[i - numSkippedOffspring2] = parent1[i];
                } else
                {
                    // Console.WriteLine("SKIPPING ADDING {0} TO OFFSPRING A", parent1[i]);
                    numSkippedOffspring2++;
                }
                if (offspring1.Shifts.Count(parent2[i]) < NumShiftsPerDoc[parent2[i]])
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
                if (numSkippedOffspring1 > 0 && offspring1.Shifts.Count(parent2[i]) < NumShiftsPerDoc[parent2[i]])
                {
                    // Console.WriteLine("RE-ADDING {0} to Child A", parent2[i]);
                    offspring1[indexToReAdd1] = parent2[i];
                    numSkippedOffspring1--;
                }
                if (numSkippedOffspring2 > 0 && offspring2.Shifts.Count(parent1[i]) < NumShiftsPerDoc[parent1[i]])
                {
                    // Console.WriteLine("RE-ADDING {0} to Child B", parent1[i]);
                    offspring2[indexToReAdd2] = parent1[i];
                    numSkippedOffspring2--;
                }
            }
            // Console.WriteLine("Children are: ");
            offspring1.Fitness = ScheduleEvaluator.Evaluate(offspring1);
            offspring2.Fitness = ScheduleEvaluator.Evaluate(offspring2);
            // Console.WriteLine(offspring1);
            // Console.WriteLine(offspring2);
            return (offspring1, offspring2);
        }

        private Schedule[] SelectSurvivors()
        {
            return schedules[^numSurvivors..];
        }
        private Schedule[] SelectParents()
        {
            int numParents = schedules.Length - numSurvivors;
            return schedules[^numParents..];
        }

    }
}