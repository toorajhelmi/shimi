using Shimi.Shared;
using System.Data;
using System.Text;

namespace Shimi
{
    public class Algo1(DebugService debugService, List<string> roots, string overview)
    {
        private readonly string openAIKey = "";

        private List<Node> rootList = roots.Select(x => new Node(x)).ToList();

        // Max number of nodes
        public int T { get; set; } = 4;
        // max depth levels per entity summary chain
        public int L { get; set; } = 3;
        public double CompressionRatio { get; set; } = 0.50;
        public double SimilarityThreshold { get; set; } = 0.5;

        public List<Entity> Entities { get; set; } = [];

        public async Task AddEntity(Entity entity)
        {
            var root = await MatchToBucket(entity, roots);
            var generalizations = await Generalize(entity, root);

            if (generalizations.Count == 0)
            {
                return;
            }

            string topSummary = generalizations[0];

            var relatedRoot = await DescendTree(rootList.First(r => r.Summary == root), topSummary);
            //var isSuper = await IsSuper(attachParent.Summary, topSummary);
            //Node topNode;

            //Node similarSibiling = await FindSimilarSibiling(topSummary, attachParent.Children);
            //if (similarSibiling != null)
            //{
            //    debugService.Highlight($"Used sibiling '{similarSibiling.Summary}' for new one '{topSummary}'.", Highlight.Important);
            //    topNode = similarSibiling;
            //}
            //else
 
            //var topNode = await AddNode(new Node(topSummary), attachParent);
      

            var leafNode = await AttachSummaryChain(relatedRoot, generalizations);

            leafNode.EntityId = entity.Id;
            //if (!leafNode.Entities.Any(entity => entity.Concept == entity.Concept))
            //{
            //    leafNode.Entities.Add(entity);
            //    Entities.Add(entity);
            //}
        }

        private async Task<Node> AttachSummaryChain(Node parentNode, List<string> chain)
        {
            Node currentParent = parentNode;

            foreach (var summary in chain)
            {
                currentParent = await DescendTree(currentParent, summary);
                currentParent = await AddNode(new Node(summary), currentParent);
            }

            return currentParent;
        }

        private async Task<Node> AddNode(Node node, Node parent)
        {
            //if (parent != null)
            {
                Node similarSibiling = await FindSimilarSibiling(node.Summary, parent.Children.Union([parent]).ToList());

                Node newNode = null;
                if (similarSibiling != null)
                {
                    debugService.Highlight($"Used sibiling '{similarSibiling.Summary}' for new one '{node.Summary}'.", Highlight.Important);
                    newNode = similarSibiling;
                }
                else
                {
                    parent.Children.Add(node);
                    node.Parent = parent;
                    await MergeNodesIfNeeded(parent);
                    newNode = node;
                }

                return newNode;
            }
            //else
            //{
            //    rootList.Add(node);
            //}
        }

        private async Task MergeNodesIfNeeded(Node parent)
        {
            var list = parent == null ? rootList : parent.Children;

            // While number of roots > T, merge the two most similar nodes
            while (list.Count > T)
            {
                // Find two most similar roots
                (int iA, int iB, double simAB) = await FindTwoMostSimilarNodes(list);
                if (iA < 0 || iB < 0) return;

                // Create new parent with a summary that merges them conceptually
                var merged = await MergeConcepts(list[iA].Summary, list[iB].Summary);
                debugService.Highlight($"[{merged}] = [{list[iA].Summary}] + [{list[iB].Summary}]", Highlight.Important);

                var newParent = new Node(merged);

                // Move children
                var nodeA = list[iA];
                var nodeB = list[iB];

                await AddNode(nodeA, newParent);
                await AddNode(nodeB, newParent);

                // Remove from rootList, add new parent
                if (iA > iB)
                {
                    list.RemoveAt(iA);
                    list.RemoveAt(iB);
                }
                else
                {
                    list.RemoveAt(iB);
                    list.RemoveAt(iA);
                }

                list.Add(newParent);
            }
        }

        private async Task<Node> DescendTree(Node root, string summary)
        {
            //Node bestChild = root;
            //var bestSim = await ComputeSemanticSimilarity(root.Summary, summary);

            Node bestChild = null;
            double bestSim = SimilarityThreshold; 

            foreach (var child in root.Children)
            {
                if (await IsSuper(root.Summary, child.Summary))
                {
                    bestChild = child;
                    break;
                }
                //var sim = await ComputeSemanticSimilarity(child.Summary, summary);

                //if (sim >= bestSim)
                //{
                //    bestSim = sim;
                //    bestChild = child;
                //}
            }

            if (bestChild == null)
                return root;

            return await DescendTree(bestChild, summary);
        }

        private async Task<(int, int, double)> FindTwoMostSimilarNodes(List<Node> list)
        {
            int bestI = -1;
            int bestJ = -1;
            double bestSim = 0.0;

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    double sim = await ComputeSemanticSimilarity(list[i].Summary, list[j].Summary);
                    if (sim > bestSim)
                    {
                        bestSim = sim;
                        bestI = i;
                        bestJ = j;
                    }
                }
            }

            return (bestI, bestJ, bestSim);
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private async Task<List<string>> Generalize(Entity entity, string root)
        {
            var gpt = new GptService(openAIKey);

            var explantionPrompt = entity.Explantion == "" ? "" : $"Also, here is more explanation about the concept: '{entity.Explantion}'";

            string prompt = $@"
Your task is to generate {L} or less levels of generalization between least general concept: '{entity.Concept}' and most general concept: '{root}' excluding either. Note that concepts are around: '{overview}'. {explantionPrompt} 

Rule 1. The number of words in each generalization should be {CompressionRatio} of the previous one.
Rule 2. Generalizations should convey the original meaning in the provided concept.
Rule 3. Don't include any commas other than ones used to separated different generalization levels.
Rule 4. ONLY return a comma-separated list of generalization from the most general to the least.
Rule 5: Avoid using vague or overly generic terms such as sector, industry, category, area, type, service, agents, etc. Use domain-relevant terminology that provides real semantic value.";

            var response = await gpt.SendMessage(prompt);
            var generalizations = response.Split(',').ToList();
            return generalizations;
        }

        private async Task<Node> FindSimilarSibiling(string concept, List<Node> nodes)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Your task is to find a concept within the list below which could be assume simliar to this concept: '{concept}'.
Note: If none of the concepts are similar JUST return <NONE>; otherwise ONLY return the similar concept as provided in the list

List: {string.Join('\n', nodes.Select(c => c.Summary))}";

            var response = await gpt.SendMessage(prompt);
            return nodes.FirstOrDefault(n => n.Summary == response);
        }

        private async Task<string> MergeConcepts(string concept1, string concept2)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Your task is to obtain a higher level concept that the following concepts are a subset of. The new concept can have at most same number of words the longest concept. 
The overall focus of these concepts is '{overview}' which you should consider when coming up with the new higher level concept.
Note: ONLY return the highler level concept.
Concept 1: {concept1}
Concept 2: {concept2}";

            var merged = await gpt.SendMessage(prompt);
            return merged;
        }

        private async Task<double> ComputeSemanticSimilarity(string concept1, string concept2)
        {
            var gpt = new GptService(openAIKey);

            if (string.IsNullOrEmpty(concept1) || string.IsNullOrEmpty(concept2)) return 0.0;

            string prompt = $@"
Read the following two concepts and determine how similar they are in meaning, on a scale from 0.0 to 1.0, where 0.0 = completely different and 1.0 = nearly identical in meaning.
Note: Provide ONLY the numeric score in your response, nothing else.

Concept 1: '{concept1}'

COncept 2: '{concept2}'
";
            var scoreText = await gpt.SendMessage(prompt);
            return double.Parse(scoreText);
        }

        private async Task<string> MatchToBucket(Entity entity, List<string> buckets)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Pick the bucket that provided concept best belongs to. ONLY return the bucket name.

Concept : '{entity.FullExplanation}'

Buckets 2: {string.Join(", ", buckets)}
";
            var bucket = await gpt.SendMessage(prompt);
            return bucket;
        }

        private async Task<bool> IsSuper(string superConcept, string subConcept)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Given the provided concepts, return 1 if concept A is a superset of concept B. Return 0, otherwise.
Note: Provide ONLY the numeric value in your response, nothing else.
Concept A: '{superConcept}'

COncept B: '{subConcept}'
";
            var scoreText = await gpt.SendMessage(prompt);
            return Convert.ToBoolean(int.Parse(scoreText));
        }

        #region Query

        public async Task<List<int>> Query(string query)
        {
            // 1. Compare query to each root
            List<Node> matchingRoots = [];

            foreach (var root in rootList)
            {
                var statement = string.Format(query, root.Summary);
                double sim = await Match(statement);
                if (sim >= SimilarityThreshold)
                {
                    matchingRoots.Add(root);
                }
            }

            if (!matchingRoots.Any())
                return []; // No suitable match

            List<Node> matchingNodes = [];

            // 2. Descend further from bestRoot
            foreach (var root in matchingRoots)
                matchingNodes.AddRange(await QueryDescendents(root, query));

            var matchingEntities = new List<int>();
            
            foreach (var node in matchingNodes)
            {
                matchingEntities.Add(node.EntityId.Value);
            }

            return matchingEntities;
        }
        private async Task<List<Node>> QueryDescendents(Node node, string query)
        {
            List<Node> matchingNodes = [];

            if (node.Children.Count == 0)
            {
                matchingNodes.Add(node);
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var statement = string.Format(query, child.Summary);
                    double sim = await Match(statement);
                    if (sim >= SimilarityThreshold)
                    {
                        return await QueryDescendents(child, query);
                    }
                }
            }

            return matchingNodes;
        }
        private async Task<double> Match(string statement)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
You are a system designed to specify how true the provided statement is on a scale from 0.0 to 1.0,
where 0.0 = completely false and 1.0 = completely true.

Provide ONLY the numeric score in your response, nothing else.

Statement $'{statement}'";
            var scoreText = await gpt.SendMessage(prompt);
            return double.Parse(scoreText);
        }

        #endregion

        #region Print

        public string PrintTree()
        {
            var sb = new StringBuilder();
            foreach (var root in rootList)
                PrintSelfAndDesendents(sb, root, 1);

            return sb.ToString();
        }

        private StringBuilder PrintSelfAndDesendents(StringBuilder sb, Node node, int level)
        {
            if (node == null) return sb;

            sb.AppendLine($"{new string('-', level)}{node.Summary}");

            //foreach (var entity in node.Entities)
            //    sb.AppendLine($"{new string('-', level+1)}*{entity.Id}:{entity.Concept}");

            foreach (var child in node.Children)
                PrintSelfAndDesendents(sb, child, level + 1);

            return sb;
        }

        #endregion
    }
}