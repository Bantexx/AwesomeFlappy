namespace AwesomeFlappyClient.Models;

public class RenderState
{
    public int Score { get; set; }
    public bool Paused { get; set; }

    // размеры игрового мира (логические единицы)
    public int WorldWidth { get; set; }
    public int WorldHeight { get; set; }

    public string? Background { get; set; }
    public PlayerRender? Player { get; set; }
    public List<PipeRender> Pipes { get; set; } = new();
}