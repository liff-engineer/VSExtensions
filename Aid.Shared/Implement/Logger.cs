using System.Diagnostics;
using System.IO;

namespace Aid.Shared.Implement
{
    internal class Logger : ILogger
    {
        public string File { get; }
        public Logger(string file) 
        {
            File = file;//Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aid", "Aid.log");
            string path = Path.GetDirectoryName(File);
            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(path);
            }
            Trace.Listeners.Add(new TextWriterTraceListener(new StreamWriter(File,false)));
            Trace.AutoFlush = true;
        } 

        public void Error(string message)
        {
            Trace.TraceError(message);
        }

        public void Info(string message)
        {
            Trace.TraceInformation(message);
        }

        public void Warn(string message)
        {
            Trace.TraceWarning(message);
        }
    }
}
