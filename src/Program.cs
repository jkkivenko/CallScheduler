using CallSchedulerApp.Schedules;

namespace CallSchedulerApp
{
    public static class Program
    {
        private static readonly DateOnly StartDate = new(2027, 01, 01);
        private static readonly DateOnly EndDate = new(2027, 04, 01);
        private const int MaxGenerations = 1_000_000;
        private const int DisplayEveryNGenerations = 1_000;
        private const int NumSurvivors = 6;
        private const int NumSchedules = 8;
        private const float MutationChance = 0.125f;
        private static readonly Dictionary<Doctor, float> DocShiftLoads = new()
            {
                {Doctor.Cram, 1},
                {Doctor.Raymond, 1},
                {Doctor.Frecker, 1},
                {Doctor.Mackenzie, 1},
                {Doctor.Aggarwal, 1},
                {Doctor.Gans, 1},
                {Doctor.Tai, 1},
                {Doctor.Badmos, 1},
                {Doctor.Jolliffe, 1},
                {Doctor.Proisky, 1},
                {Doctor.Brown, 1},
                {Doctor.Chan, 0.5f},
                {Doctor.Iancu, 1},
                {Doctor.Smith, 1}
            };
        static void Main(string[] args)
        {
            EvolutionaryAlgorithm algo = new(StartDate, EndDate, DocShiftLoads, NumSchedules, NumSurvivors, MutationChance);

            bool shouldContinue = true;
            int generation = 0;
            for(; generation < MaxGenerations && shouldContinue; generation++)
            {
                if (generation % DisplayEveryNGenerations == 0)
                {
                    Console.WriteLine("Generation {0} Best Fitness: {1}", generation, ScheduleEvaluator.Evaluate(algo.GetBest(), DebugLevel.Problems));
                }
                shouldContinue = algo.RunGeneration(generation);
            }
            Console.WriteLine(algo.GetBest());
            ScheduleEvaluator.Evaluate(algo.GetBest(), DebugLevel.Problems);
            Console.WriteLine("Finished in {0} generations.", generation);
        }
    }
}