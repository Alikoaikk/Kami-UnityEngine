using UnityEngine;

namespace Kami.Gameplay
{
    /// <summary>Saved progress (PlayerPrefs): best stars and golden cranes per level, and unlocked shapes.</summary>
    public static class GameProgress
    {
        public static int BestStars(int level) => PlayerPrefs.GetInt($"kami.stars.{level}", 0);

        public static int BestCranes(int level) => PlayerPrefs.GetInt($"kami.cranes.{level}", 0);

        public static bool IsUnlocked(ShapeId shape) =>
            shape <= ShapeId.Boat || PlayerPrefs.GetInt($"kami.unlock.{shape}", 0) == 1;

        public static void RecordLevel(LevelData level, int stars, int cranes)
        {
            if (stars > BestStars(level.number))
                PlayerPrefs.SetInt($"kami.stars.{level.number}", stars);
            if (cranes > BestCranes(level.number))
                PlayerPrefs.SetInt($"kami.cranes.{level.number}", cranes);
            if (level.unlocks != null)
                PlayerPrefs.SetInt($"kami.unlock.{level.unlocks.id}", 1);
            PlayerPrefs.Save();
        }
    }
}
