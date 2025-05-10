namespace Shimi.Samples.Agents
{
    public class BasicSample
    {
        Algo1 engine = new(new DebugService(), ["agents"], "agents");

        public BasicSample()
        {
            
        }

        private async Task AddMathAgents()
        {
            await engine.AddEntity(new Agent
            {
                Description = "I can add two numbers.",
            });

            await engine.AddEntity(new Agent
            {
                Description = "I can divide two numbers.",
            });
        }

        private async Task AddFinAgents()
        { 
            await engine.AddEntity(new Agent
            {
                Description = "I can call trading platform APIs to purchase assets.",
                //Skills = "trading, calling trading platform APIs"
            });

            await engine.AddEntity(new Agent
            {
                Description = "I can select financial assets to achieve an expected yield within a specified period.",
                //Skills = "ROI Optimization"
            });
        }

        public async Task MatchFinancialAgents()
        {
            //await AddMathAgents();
            await AddFinAgents();

            string task = "I have some cash and would love to keep it safe first of all, second I would like to have 10% return on it per year.";
            string query = "Agent with capability '{0}' could be used in accomplishing this task: " + $"'{task}'";
            var matchingEntities = await engine.Query(query);

            Console.WriteLine("Matching agents: ");

            foreach (var entity in matchingEntities)
            {
                Console.WriteLine(entity.ToString());
            }
        }

        public async Task MatchMathAgents()
        {
            await AddMathAgents();
            //await AddFinAgents();

            string task = "Calculate 5 times 8 divided by 3";
            string query = "Agent with capability '{0}' could be used in accomplishing this task: " + $"'{task}'";
            var matchingEntities = await engine.Query(query);

            Console.WriteLine("Matching agents: ");

            foreach (var entity in matchingEntities)
            {
                Console.WriteLine(entity.ToString());
            }
        }
    }
}
