using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kami.Gameplay
{
    /// <summary>
    /// Runs one level: owns the input, sets Kami up from the LevelData, tracks the checkpoint (last pressing
    /// stone), respawns Kami after a tear, handles pause (Esc), restart from the last stone (R), golden
    /// cranes, and the level-complete screen with stars (Enter: next level).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] LevelData level;
        [SerializeField] KamiController kami;
        [SerializeField] KamiHUD hud;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] Transform startPoint;
        [Tooltip("Falling below this height tears Kami.")]
        [SerializeField] float killY = -4f;
        [SerializeField] float respawnDelay = 1.3f;

        public KamiInput Input { get; private set; }
        public LevelData Level => level;
        public KamiController Kami => kami;
        public int CranesFound { get; private set; }
        public int CranesTotal { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsPaused { get; private set; }

        Vector2 checkpoint;
        PressingStone activeStone;

        void Awake()
        {
            Instance = this;
            Input = new KamiInput();
            Time.timeScale = 1f;
        }

        void Start()
        {
            checkpoint = startPoint != null ? (Vector2)startPoint.position : (Vector2)kami.transform.position;
            kami.Setup(level, Input);
            kami.Torn += OnTorn;
            if (hud != null)
                hud.Bind(this);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            Input?.Dispose();
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (IsComplete)
            {
                if (Input.Confirm.WasPressedThisFrame())
                    LoadNext();
                return;
            }

            if (Input.Pause.WasPressedThisFrame())
                SetPaused(!IsPaused);
            if (IsPaused)
                return;

            if (Input.Restart.WasPressedThisFrame() && !kami.IsTorn)
                RestartFromStone();
            if (!kami.IsTorn && kami.transform.position.y < killY)
                kami.Tear("Fell off the scroll.");
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            kami.Locked = paused;
            if (hud != null)
                hud.ShowPause(paused);
        }

        void OnTorn(string reason)
        {
            if (hud != null)
                hud.ShowTorn(reason);
            StartCoroutine(RespawnAfterDelay());
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        void RestartFromStone()
        {
            Respawn();
            if (hud != null)
                hud.Toast("Back to the last stone.");
        }

        void Respawn()
        {
            kami.Respawn(checkpoint);
            if (cameraFollow != null)
                cameraFollow.Snap();
            if (hud != null)
                hud.ShowTorn(null);
        }

        public void ReachStone(PressingStone stone)
        {
            if (kami.Creases.Creases > 0)
                kami.Creases.Smooth();
            if (stone == activeStone)
                return;
            if (activeStone != null)
                activeStone.SetActive(false);
            activeStone = stone;
            stone.SetActive(true);
            checkpoint = stone.RespawnPoint;
            if (hud != null)
                hud.Toast("Pressing stone: creases smoothed. Tab changes your belt.");
        }

        public void RegisterCrane() => CranesTotal++;

        public void CollectCrane()
        {
            CranesFound++;
            if (hud != null)
                hud.Refresh();
        }

        public void ShowHint(string hint)
        {
            if (hud != null)
                hud.ShowHint(hint);
        }

        public void CompleteLevel()
        {
            if (IsComplete)
                return;
            IsComplete = true;
            kami.Locked = true;
            int folds = kami.Creases.Folds;
            int stars = level.Stars(folds);
            GameProgress.RecordLevel(level, stars, CranesFound);
            if (hud != null)
                hud.ShowComplete(stars, folds);
        }

        void LoadNext()
        {
            string next = string.IsNullOrEmpty(level.nextScene) ? "Level_01" : level.nextScene;
            if (Application.CanStreamedLevelBeLoaded(next))
                SceneManager.LoadScene(next);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
