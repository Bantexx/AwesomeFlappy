namespace FlappyMiniApp.Client.Services;

public class GameSettingsService
{
    public int SelectedCharacterIndex { get; set; } = 0;
    
    public GameEngineService.DifficultyLevel Difficulty { get; set; } = GameEngineService.DifficultyLevel.Normal;
}