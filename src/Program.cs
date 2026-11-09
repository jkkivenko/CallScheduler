using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public static class Program
    {
        private static readonly DateOnly StartDate = new(2027, 01, 01);
        private static readonly DateOnly EndDate = new(2027, 05, 01);
        private const int MaxGenerations = 1_000_000;
        private const int DisplayEveryNGenerations = 2_000;
        private const int NumSurvivors = 6;
        private const int NumSchedules = 8;
        private const float MutationChance = 0.15f;
        private static readonly float[] DocShiftLoads =
            [
                1,     // Cram
                1,     // Raymond
                1,     // Frecker
                1,     // Mackenzie
                1,     // Aggarwal
                1,     // Gans
                1,     // Tai
                1,     // Badmos
                1,     // Jolliffe
                1,     // Porisky
                1,     // Brown
                0.5f,  // Chan
                1,     // Iancu
            ];
        static void Main(string[] args)
        {
            EvolutionaryAlgorithm algo = new(StartDate, EndDate, DocShiftLoads, NumSchedules, NumSurvivors, MutationChance);

            bool shouldContinue = true;
            int generation = 0;
            for(; generation < MaxGenerations && shouldContinue; generation++)
            {
                if (generation % DisplayEveryNGenerations == 0)
                {
                    Console.WriteLine("Generation {0} Best Fitness: {1}", generation, ScheduleEvaluator.Evaluate(algo.GetBest(), DebugLevel.Violations));
                }
                shouldContinue = algo.RunGeneration(generation);
            }
            Console.WriteLine(algo.GetBest());
            ScheduleEvaluator.Evaluate(algo.GetBest(), DebugLevel.Problems);
            Console.WriteLine("Finished in {0} generations.", generation);
        }
    }
}