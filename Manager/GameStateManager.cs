namespace HyperModule
{
    public static class GameStateManager
    {
        private static GameState currentGameState;
        public static GameState CurrentGameState
        {
            get => currentGameState;
            set
            {
                if (currentGameState != value)
                {
                    currentGameState = value;
                    OnGameStateChanged?.Invoke(currentGameState);
                }
            }
        }

        public static System.Action<GameState> OnGameStateChanged;
    }
}