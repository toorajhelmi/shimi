using CsvHelper;
using System.Globalization;
using System.Reflection;

namespace Shimi.Samples.Agents
{
    public class ManyAgentsSample_Math
    {
        public async Task BuildTree()
        {
            List<string> roots = [
                "Arithmetic",
                "Algebraic",
                "Statistical",
                "Geometric",
                "Miscellaneous"
            ];

            var debugService = new DebugService();
            var engine = new Algo1(debugService, roots, "We are categorizing non-human agents accomplising mathematic tasks. The categorization is based on the type of math opereration the agent performs.");

            var agents = LoadAgents();
            
            foreach (var agent in agents)
            {
                await engine.AddEntity(agent);

                debugService.Highlight($"Adding Agent {agents.IndexOf(agent)}, {agent.AgentName}");
                debugService.WriteLine(engine.PrintTree());
            }

            File.WriteAllText("agents.txt", engine.PrintTree());
        }
        private static List<Agent> LoadAgents()
        {
            var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = assembly.GetManifestResourceNames()
            //    .FirstOrDefault(name => name.EndsWith("Data.Mostly_Fin_Agents.csv"));
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("Data.Mostly_Math_Agents.csv"));

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
