namespace LivePulse.Simulator;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string ApiBaseUrl { get; set; } = "http://localhost:5152";

    public int IntervalSeconds { get; set; } = 3;

    public int MinutesPerTick { get; set; } = 3;

    public int FullTimeMinute { get; set; } = 90;

    public double GoalProbability { get; set; } = 0.25;

    public double RedCardProbability { get; set; } = 0.05;
}
