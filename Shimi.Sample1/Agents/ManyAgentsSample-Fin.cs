using CsvHelper;
using System.Globalization;
using System.Reflection;

namespace Shimi.Samples.Agents
{
    public class SeprateConsole : DebugService
    {
        private int _left, _top, _width;
        private int lineOffset;
        public SeprateConsole(int left, int top, int width)
        {
            _left = left;
            _top = top;
            _width = width;
        }

        public override void WriteLine(string text)
        {
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                int remaining = text.Length - currentIndex;
                int length = Math.Min(_width, remaining);
                string line = text.Substring(currentIndex, length).PadRight(_width);

                Console.SetCursorPosition(_left, _top + lineOffset);
                Console.Write(line);

                currentIndex += length;
                lineOffset++;
            }
        }
    }

    public class ManyAgentsSample_Fin
    {
        public async Task BuildTree()
        {
            //var leftPane = new SeprateConsole(0, 0, 40);
            //var rightPane = new SeprateConsole(41, 0, 40);

            List<string> roots = [
                "Finance Agents",
                "Health Agents",
                "Education Agents",
                "Productivity Agents",
                "Travel Agents",
                "Shopping Agents",
                "Entertainment Agents",
                "Lifestyle Agents",
                "Legal Agents",
                "Social Agents",
                "Technology Agents",
                "Sustainability Agents"
            ];

            var debugService = new DebugService(true, true);
            var engine = new Algo2(debugService, roots, "We are categorizing non-human agents accomplising tasks. The categorization is based on the industry sector and sub sectors an agent belongs to.");

            var agents = LoadAgents();
            
            foreach (var agent in agents)
            {
                debugService.Highlight($"Adding Agent {agents.IndexOf(agent)}, {agent.AgentName}");
                
                await engine.AddEntity(agent);
                debugService.WriteLine(engine.PrintTree());
            }

            File.WriteAllText($"a2-fin-agents-{DateTime.Now:yyyyMMdd_HHmmss}.txt", engine.PrintTree());
        }
        private static List<Agent> LoadAgents()
        {
            var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = assembly.GetManifestResourceNames()
            //    .FirstOrDefault(name => name.EndsWith("Data.Mostly_Fin_Agents.csv"));
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("Data.Mostly_Fin_Agents.csv"));

            if (resourceName == null)
                throw new Exception("CSV resource not found. Check the file name and build action.");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<AgentCsvMap>();
            return csv.GetRecords<Agent>().ToList();
        }
    }
}
