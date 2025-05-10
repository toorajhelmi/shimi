using Shimi.Samples.Agents;

namespace Shimi.Samples
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var sample = new RagCompareSample();
            //await sample.BuildTree();
            await sample.Query();

            Console.ReadLine();
        }
    }
}
