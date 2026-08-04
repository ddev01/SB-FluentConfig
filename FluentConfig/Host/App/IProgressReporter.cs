namespace FluentConfig
{
    /// <summary>
    /// Progress reporter for ShowProgressWindow: Report(int) and Close(); thread-safe.
    /// </summary>
    public interface IProgressReporter
    {
        void Report(int current);
        void Close();
    }
}
