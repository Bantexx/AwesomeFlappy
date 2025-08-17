namespace FlappyMiniApp.Client.Models;

public class PipeRender
{
    public double x { get; set; }
    public double width { get; set; }
    
    // высота области верхней трубы (расстояние от верхнего края до начала gap)
    public double topHeight { get; set; }
    // Y координата начала нижней трубы (в логических единицах)
    public double bottomY { get; set; }
}