namespace FlappyMiniApp.Client.Helpers;

public static class Unlocks
{
    // индексы персонажей начинаются с 0
    public static readonly int[] Thresholds = new int[] { 0, 15, 35 }; // 0 - всегда открыт
    public static readonly string[] Descriptions = new string[]
    {
        "Default character — available from start.",
        "Unlock by achieving a best score of 15 points.",
        "Unlock by achieving a best score of 35 points."
    };

    public static int Count => Thresholds.Length;
}