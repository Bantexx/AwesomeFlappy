using AwesomeFlappyClient.Models;

namespace AwesomeFlappyClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameEngineService
{
    public int Width { get; set; } = 480;
    public int Height { get; set; } = 800;

    // Player
    private double playerX;
    private double playerY;
    private double playerVy;
    private double gravity = 900;
    private double flapVelocity = -300;

    // Pipes
    private List<Pipe> pipes = new List<Pipe>();
    private double pipeTimer = 0;
    private double pipeInterval = 1.45; // can change with difficulty
    private readonly double pipeSpeedBase = 120;
    private double pipeSpeed = 120;
    private Random rnd = new Random();

    // Score
    public int Score { get; private set; } = 0;

    // Difficulty
    public enum DifficultyLevel { Easy, Normal, Hard }
    public DifficultyLevel Difficulty { get; private set; } = DifficultyLevel.Normal;

    // Character and background
    public int CharacterIndex { get; private set; } = 0;
    public int BackgroundIndex { get; private set; } = 0;

    // State
    public bool IsAlive { get; private set; } = true;
    public bool IsPaused { get; private set; } = false;

    private readonly double playerStartYRatio = 0.4;

    public GameEngineService(int width = 480, int height = 800)
    {
        Width = width;
        Height = height;
        Reset();
    }

    public void Reset()
    {
        playerX = Width * 0.3;
        playerY = Height * playerStartYRatio;
        playerVy = 0;
        pipeTimer = 0;
        pipes.Clear();
        Score = 0;
        IsAlive = true;
        IsPaused = false;
        UpdateDifficultyParams();
    }

    private void UpdateDifficultyParams()
    {
        switch (Difficulty)
        {
            case DifficultyLevel.Easy:
                pipeInterval = 1.8;         
                pipeSpeed = pipeSpeedBase * 0.85; 
                break;
            case DifficultyLevel.Normal:
                pipeInterval = 1.55;
                pipeSpeed = pipeSpeedBase * 1.05;
                break;
            case DifficultyLevel.Hard:
                pipeInterval = 1.25;
                pipeSpeed = pipeSpeedBase * 1.35;
                break;
        }
    }

    public void SetDifficulty(DifficultyLevel d)
    {
        Difficulty = d;
        UpdateDifficultyParams();
    }

    public void SetCharacterIndex(int index)
    {
        if (index < 0) index = 0;
        CharacterIndex = index;
    }

    public void Flap()
    {
        if (!IsAlive || IsPaused) return;
        playerVy = flapVelocity;
    }

    public void Update(double dt)
    {
        if (!IsAlive || IsPaused) return;

        playerVy += gravity * dt;
        playerY += playerVy * dt;

        if (playerY > Height - 30)
        {
            playerY = Height - 30;
            Die();
        }
        if (playerY < 0)
        {
            playerY = 0;
            playerVy = 0;
        }

        pipeTimer += dt;
        if (pipeTimer >= pipeInterval)
        {
            pipeTimer = 0;
            SpawnPipe();
        }

        for (int i = pipes.Count - 1; i >= 0; i--)
        {
            var p = pipes[i];
            p.X -= pipeSpeed * dt;
            if (!p.Passed && p.X + p.Width < playerX)
            {
                p.Passed = true;
                Score++;
                // progression unlocks
                if (Score == 10) BackgroundIndex = Math.Min(BackgroundIndex + 1, 2);
                if (Score == 25) BackgroundIndex = Math.Min(BackgroundIndex + 1, 2);
                if (Score == 15) CharacterIndex = Math.Max(CharacterIndex, 1);
                if (Score == 35) CharacterIndex = Math.Max(CharacterIndex, 2);
            }
            if (p.X + p.Width < -50) pipes.RemoveAt(i);
        }

        foreach (var p in pipes)
        {
            if (AABBOverlap(playerX - 18, playerY - 18, 36, 36, p.X, 0, p.Width, p.TopHeight))
            {
                Die();
            }
            if (AABBOverlap(playerX - 18, playerY - 18, 36, 36, p.X, p.BottomY, p.Width, Height - p.BottomY))
            {
                Die();
            }
        }
    }

    private bool AABBOverlap(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
    {
        return ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
    }

    private void Die()
    {
        IsAlive = false;
    }
    
    private void SpawnPipe()
    {
        // base gap per difficulty with some randomness
        double baseGap = 150.0;
        if (Difficulty == DifficultyLevel.Easy) baseGap = 180;
        else if (Difficulty == DifficultyLevel.Normal) baseGap = 150;
        else baseGap = 130;

        // small random variance to keep it natural
        var gapVariation = (rnd.NextDouble() - 0.5) * 24.0; // ±12 px
        var gapSize = Math.Max(110, baseGap + gapVariation);

        // ensure topY is in sensible bounds
        double minTop = 40;
        double maxTop = Height - 120 - gapSize;
        if (maxTop < minTop) maxTop = minTop + 10;
        double topY = minTop + rnd.NextDouble() * Math.Max(0, maxTop - minTop);

        var pipeWidth = 64; // slightly thinner pipes
        var spawnX = Width + pipeWidth;

        // Optional: ensure last pipe is sufficiently left before spawning next
        if (pipes.Count > 0)
        {
            var lastPipe = pipes[pipes.Count - 1];
            // если последний ещё не ушёл на достаточное расстояние вправо — пропускаем спавн
            var minHorizontalSpacing = 150; // world units — можно тонко настраивать
            if (lastPipe.X > Width - minHorizontalSpacing)
            {
                return;
            }
        }

        var p = new Pipe
        {
            X = spawnX,
            Width = pipeWidth,
            TopHeight = topY,
            BottomY = topY + gapSize,
            Passed = false,
            Height = Height
        };
        pipes.Add(p);
    }

    public RenderState GetRenderState()
    {
        var rs = new RenderState
        {
            Score = Score,
            Paused = IsPaused,
            WorldWidth = Width,
            WorldHeight = Height,
            Background = $"images/bg{BackgroundIndex}.svg",
            Player = new PlayerRender
            {
                x = playerX,
                y = playerY,
                rotation = Math.Atan2(playerVy, 200) * 0.5,
                sprite = $"images/bird{CharacterIndex + 1}.svg",
                size = 48
            },
            Pipes = pipes.Select(p => new PipeRender
            {
                x = p.X,
                width = p.Width,
                topHeight = p.TopHeight,
                bottomY = p.BottomY
            }).ToList()
        };
        return rs;
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    internal class Pipe
    {
        public double X;
        public double Width;
        public double TopHeight;
        public double BottomY;
        public bool Passed;
        public double Height;
    }
}