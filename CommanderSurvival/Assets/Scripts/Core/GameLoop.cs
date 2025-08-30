using UnityEngine;

namespace RTSPrototype.Core
{
    /// <summary>
    /// Manages the main game loop and game state
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private float gameSpeed = 1.0f;
        
        public static GameLoop Instance { get; private set; }
        
        public bool IsGameActive { get; private set; } = true;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeGame();
        }
        
        private void Update()
        {
            if (IsGameActive)
            {
                UpdateGame();
            }
        }
        
        private void InitializeGame()
        {
            // Initialize game systems
            Debug.Log("GameLoop: Game initialized");
        }
        
        private void UpdateGame()
        {
            // Update game logic
        }
        
        public void PauseGame()
        {
            IsGameActive = false;
            Time.timeScale = 0f;
        }
        
        public void ResumeGame()
        {
            IsGameActive = true;
            Time.timeScale = gameSpeed;
        }
        
        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.1f, 5.0f);
            if (IsGameActive)
            {
                Time.timeScale = gameSpeed;
            }
        }
    }
}