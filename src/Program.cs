using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public static class Program
    {
        private static readonly DateOnly StartDate = new(2026, 09, 01);
        private static readonly DateOnly EndDate = new(2027, 01, 01);
        private const int MaxGenerations = 1000000;
        private const int DisplayEveryNGenerations = 50000;
        private const int NumSurvivors = 12;
        private const int NumSchedules = 16;
        private const float MutationChance = 0.25f;
        static void Main(string[] args)
        {
            EvolutionaryAlgorithm algo = new(StartDate, EndDate, NumSchedules, NumSurvivors, MutationChance);

            bool shouldContinue = true;
            int generation = 0;
            for(; generation < MaxGenerations && shouldContinue; generation++)
            {
                if (generation % DisplayEveryNGenerations == 0)
                {
                    // Console.WriteLine("~~~~~~~~~~~~~~~~");
                    // Console.WriteLine(algo);
                    Console.WriteLine("Generation {0} Best Fitness: {1}", generation, algo.GetBest().Fitness);
                    // Console.WriteLine("Best:\n{0}", algo.GetBest());
                    // ScheduleEvaluator.Evaluate(algo.GetBest(), true);
                    // Console.ReadLine();
                }
                shouldContinue = algo.RunGeneration();
            }
            Console.WriteLine(algo.GetBest());
            ScheduleEvaluator.Evaluate(algo.GetBest(), true);
            Console.WriteLine("Finished in {0} generations.", generation);
        }
    }
}