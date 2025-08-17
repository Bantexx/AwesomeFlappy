using System.Text.Json;
using Microsoft.JSInterop;

namespace FlappyMiniApp.Client.Services;

public class LeaderboardService(IJSRuntime js) 
{
    private const string Key = "flappy_leaderboard_v1";
    
    public async Task SaveScoreLocalAsync(int score)
    {
        var list = await GetLocalTopAsync();
        list.Add(score);
        list.Sort((a,b)=>b-a);
        if (list.Count > 20) list = list.GetRange(0,20);
        await js.InvokeVoidAsync("flappy.saveLocal", Key, list);
    }
    
    public async Task<List<int>> GetLocalTopAsync()
    {
        var raw = await js.InvokeAsync<string>("flappy.loadLocal", Key);
        if (string.IsNullOrEmpty(raw)) return new List<int>();
        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw) ?? new List<int>();
        }
        catch { return new List<int>(); }
    }
}