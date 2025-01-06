namespace Aid.Shared
{
    public interface ILogger
    {
        public string File { get; }
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
