using System;

public static class GameEvents
{
    public static event Action OnPlayerDied;
    public static event Action OnCrystalCollected;
    public static event Action OnLevelCompleted;

    public static void PlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    public static void CrystalCollected()
    {
        OnCrystalCollected?.Invoke();
    }

    public static void LevelCompleted()
    {
        OnLevelCompleted?.Invoke();
    }
}
