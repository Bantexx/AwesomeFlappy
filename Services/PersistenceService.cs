using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace FlappyMiniApp.Client.Services;

public class PersistenceService(IJSRuntime js)
{
    private const string BestKey = "flappy_best";
    private const string UnlockedKey = "flappy_unlocked_chars";
    private const string SettingsKey = "flappy_settings";
    
    // best score (int)
    public async Task<int> GetBestAsync()
    {
        var raw = await js.InvokeAsync<string>("flappy.loadLocal", BestKey);
        if (string.IsNullOrEmpty(raw)) return 0;
        try
        {
            return JsonSerializer.Deserialize<int>(raw);
        }
        catch
        {
            return 0;
        }
    }

    public async Task SaveBestAsync(int best)
    {
        await js.InvokeVoidAsync("flappy.saveLocal", BestKey, best);
    }

    // unlocked characters (list of ints)
    public async Task<List<int>> GetUnlockedCharsAsync()
    {
        var raw = await js.InvokeAsync<string>("flappy.loadLocal", UnlockedKey);
        if (string.IsNullOrEmpty(raw)) return new List<int> { 0 }; // default unlocked first character
        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw) ?? new List<int> { 0 };
        }
        catch
        {
            return new List<int> { 0 };
        }
    }

    public async Task UnlockCharAsync(int index)
    {
        var list = await GetUnlockedCharsAsync();
        if (!list.Contains(index)) list.Add(index);
        list.Sort();
        await js.InvokeVoidAsync("flappy.saveLocal", UnlockedKey, list);
    }

    // settings (selected character index + difficulty)
    private class StoredSettings
    {
        [JsonPropertyName("selectedCharacterIndex")]
        public int SelectedCharacterIndex { get; set; }
        
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }
    }

    public async Task SaveSettingsAsync(int selectedCharacterIndex, GameEngineService.DifficultyLevel difficulty)
    {
        var s = new StoredSettings
        {
            SelectedCharacterIndex = selectedCharacterIndex,
            Difficulty = difficulty.ToString()
        };
        await js.InvokeVoidAsync("flappy.saveLocal", SettingsKey, s);
    }

    public async Task<(int selectedCharacter, GameEngineService.DifficultyLevel difficulty)> LoadSettingsAsync()
    {
        var raw = await js.InvokeAsync<string>("flappy.loadLocal", SettingsKey);
        if (string.IsNullOrEmpty(raw)) return (0, GameEngineService.DifficultyLevel.Normal);
        try
        {
            var s = JsonSerializer.Deserialize<StoredSettings>(raw);
            if (s == null) return (0, GameEngineService.DifficultyLevel.Normal);
            if (!Enum.TryParse<GameEngineService.DifficultyLevel>(s.Difficulty, out var diff)) diff = GameEngineService.DifficultyLevel.Normal;
            return (s.SelectedCharacterIndex, diff);
        }
        catch
        {
            return (0, GameEngineService.DifficultyLevel.Normal);
        }
    }
}