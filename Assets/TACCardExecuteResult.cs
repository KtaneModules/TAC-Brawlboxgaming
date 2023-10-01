namespace TAC
{
    abstract class TACCardExecuteResult
    {
        public static implicit operator TACCardExecuteResult(TACGameState state) => new TACCardExecuteSuccess(state);
        public static implicit operator TACCardExecuteResult(string message) => new TACCardExecuteStrike(message);
    }

    class TACCardExecuteSuccess : TACCardExecuteResult
    {
        private TACGameState _state;
        public TACGameState State => _state;
        public TACCardExecuteSuccess(TACGameState state) { _state = state; }
    }

    class TACCardExecuteStrike : TACCardExecuteResult
    {
        private string _loggingMessage;
        public string LoggingMessage => _loggingMessage;
        public TACCardExecuteStrike(string loggingMessage) { _loggingMessage = loggingMessage; }
    }
}
