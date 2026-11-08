using UnityEngine;

namespace Kami.Gameplay
{
    public enum Biome { BambooForest, River, Mountain, Cave, Finale }

    /// <summary>
    /// One level's rules and layout. Kami > Levels > Build All Levels turns the layout text into the
    /// scene Assets/Scenes/Levels/Level_XX.unity (rebuilding overwrites hand edits to that scene).
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Kami/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int number = 1;
        public string title = "First steps";
        public Biome biome;
        public string goalText;

        [Header("Rules")]
        [Range(1, 12)] public int creaseLimit = 8;
        public int parFolds = 2;
        [Tooltip("Slot 1 is always the ninja.")]
        public ShapeData[] startingBelt = new ShapeData[4];
        [Tooltip("Shapes Kami may put on the belt at a pressing stone.")]
        public ShapeData[] swapPool;
        [Tooltip("Shape unlocked by finishing this level (can be empty).")]
        public ShapeData unlocks;

        [Header("Story")]
        [TextArea(2, 4)] public string sageHint;

        [Header("Flow")]
        [Tooltip("Scene loaded after the level-complete screen. Empty = back to Level_01.")]
        public string nextScene;

        [Header("Layout (one character per tile, top row first)")]
        [TextArea(8, 30)] public string layout;

        /// <summary>GDD: 3 stars at or under par, 2 up to par + 2, 1 for finishing.</summary>
        public int Stars(int folds) => folds <= parFolds ? 3 : folds <= parFolds + 2 ? 2 : 1;

        public string SceneName => $"Level_{number:00}";
    }
}
