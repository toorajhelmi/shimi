namespace Shimi
{
    public enum Highlight
    {
        Information,
        Important
    }

    public class DebugService
    {
        private string logFile = "";
        private bool record;
        private bool display;
        private object _fileLock = new();

        public DebugService(bool display = true, bool record = false)
        {
            this.record = record;
            this.display = display;

            if (record)
            {
                if (!Directory.Exists("logs"))
                {
                    Directory.CreateDirectory("logs");
                }

                logFile = $"logs\\HL-{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                using (File.CreateText(logFile)) { }

            }
        }

        public virtual void WriteLine(string message)
        {
            if (display)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
            }

            if (record)
            {
                lock (_fileLock)
                {
                    File.AppendAllText(logFile, message + '\n');
                }
            }
        }

        public virtual void Highlight(string message, Highlight highlight = Shimi.Highlight.Information)
        {
            if (display)
            {
                switch (highlight)
                {
                    case Shimi.Highlight.Information:
                        Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case Shimi.Highlight.Important:
                        Console.ForegroundColor = ConsoleColor.Green; break;
                }


                Console.WriteLine(message);
            }

            if (record)
            {
                lock (_fileLock)
                {
                    File.AppendAllText(logFile, message + '\n');
                }
            }
        }
    }
}
