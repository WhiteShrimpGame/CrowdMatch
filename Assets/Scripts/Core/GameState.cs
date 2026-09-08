namespace CrowdMatch
{
    /// <summary>
    /// 纯状态机：追踪当前游戏会话所处阶段。调用方（GameController、UI 等）通过
    /// IsGameStart / IsGamePause 等控制输入与胜负检测。
    /// </summary>
    public static class GameState
    {
        public enum State
        {
            None,
            Start,
            Win,
            Pause,
            Fail,
            Idle,
        }

        /// <summary>当前状态。仅通过下方静态转换方法修改。</summary>
        public static State CurrentState { get; private set; }

        public static void GameNone() { CurrentState = State.None; }
        public static void GameStart() { CurrentState = State.Start; }
        public static void GamePause() { CurrentState = State.Pause; }
        public static void GameWin() { CurrentState = State.Win; }
        public static void GameFail() { CurrentState = State.Fail; }
        public static void GameIdle() { CurrentState = State.Idle; }

        public static bool IsGameNone { get { return CurrentState == State.None; } }
        public static bool IsGameStart { get { return CurrentState == State.Start; } }
        public static bool IsGameWin { get { return CurrentState == State.Win; } }
        public static bool IsGamePause { get { return CurrentState == State.Pause; } }
        public static bool IsGameFail { get { return CurrentState == State.Fail; } }
        public static bool IsGameIdle { get { return CurrentState == State.Idle; } }
    }
}
