using Newtonsoft.Json;
using Shimi.Shared;
using System.Data;
using System.Text;
using System.Xml.Linq;

namespace Shimi
{
    public enum RequiredMatchLevel
    {
        Extreme,
        High,
        Medium,
        Low,
        Any
    }

    public class Algo2(DebugService debugService, List<string> roots, string overview)
    {
        // Max number of nodes
        public int T { get; set; } = 4;
        // max depth levels per entity summary chain
        public int L { get; set; } = 3;
        public double CompressionRatio { get; set; } = 0.50;
        public double SimilarityThreshold { get; set; } = 0.5;

        private readonly string openAIKey = "";

        //private List<Node> rootList = new List<Node>([new Node("Other")]).Union(roots.Select(x => new Node(x))).ToList();
        private List<Node> rootList = roots.Select(x => new Node(x)).ToList();

        private string buckets = string.Join('\n', roots.Select(r => $"{roots.IndexOf(r)}: {r}"));

        public List<Entity> Entities { get; set; } = [];

        public async Task AddEntity(Entity entity)
        {
            var matchingRoots = await MatchToBucket(entity);
            List<Node> marchingRootNodes = [];
            if (matchingRoots.Count > 0)
                marchingRootNodes = matchingRoots.Select(index => rootList[index]).ToList();
            else
            {
                marchingRootNodes.Add(rootList[0]);
                debugService.Highlight($"No parent found for [{entity.Concept}]", Highlight.Important);
            }

            var parentNodes = await DescendTree(marchingRootNodes, entity.FullExplanation);

            if (parentNodes.Count > 1)
                debugService.Highlight($"Found mutiple parents: {string.Join(',', parentNodes.Select(pn => $"[{pn.Summary}]"))}"); 

            foreach (var parentNode in parentNodes)
            {
                //var parentNode = await DescendTree(root, entity.Concept);
                var leafNode = await AddNode(new Node(entity.Concept, entity.Explantion), parentNode);

                ///if (!leafNode.Entities.Any(entity => entity.Concept == entity.Concept))
                {
                    leafNode.EntityId = entity.Id;
                }
            }
        }

        private async Task<Node> AddNode(Node node, Node parent)
        {
            Node newNode = await FindSimilarSibiling(node.Description, parent.Children);

            if (newNode != null)
            {
                debugService.Highlight($"Used sibiling '{newNode.Summary}' for new one '{node.Summary}'.", Highlight.Important);
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

        private async Task MergeNodesIfNeeded(Node parent)
        {
            var list = parent == null ? rootList : parent.Children;

            // While number of roots > T, merge the two most similar nodes
            while (list.Count > T)
            {
                // Find two most similar roots
                var groups = await Split(list);

                foreach (var group in groups)
                {
                    debugService.Highlight($"Generated: [{group.Summary}] = {string.Join('+', group.Children.Select(n => $" [{n.Summary}]"))}", Highlight.Important);
                }

                // Move to new parent. First remove to avoid using them during AddNode.
                parent.Children.Clear();
                parent.Children.AddRange(groups);
                groups.ForEach(g => g.Parent = parent);

                //var nodeA = list[iA];
                //var nodeB = list[iB];

                //if (iA > iB)
                //{
                //    list.RemoveAt(iA);
                //    list.RemoveAt(iB);
                //}
                //else
                //{
                //    list.RemoveAt(iB);
                //    list.RemoveAt(iA);
                //}

                //var newParent = await AddNode(mergedNode, parent);
                //await AddNode(nodeA, newParent);
                //await AddNode(nodeB, newParent);
            }
        }

        //private async Task MergeNodesIfNeeded(Node parent)
        //{
        //    var list = parent == null ? rootList : parent.Children;

        //    // While number of roots > T, merge the two most similar nodes
        //    while (list.Count > T)
        //    {
        //        // Find two most similar roots
        //        (int iA, int iB) = await FindTwoMostSimilarNodes(list);
        //        if (iA < 0 || iB < 0) return; //THere are no similar nodes

        //        var mergedNode = await MergeConcepts(list[iA], list[iB], list[iA].Parent);
        //        if (mergedNode == null) return; //Could not semantically merge
                
        //        debugService.Highlight($"[{mergedNode.Summary}] = [{list[iA].Summary}] + [{list[iB].Summary}]", Highlight.Important);

        //        // Move to new parent. First remove to avoid using them during AddNode.
        //        var nodeA = list[iA];
        //        var nodeB = list[iB];

        //        if (iA > iB)
        //        {
        //            list.RemoveAt(iA);
        //            list.RemoveAt(iB);
        //        }
        //        else
        //        {
        //            list.RemoveAt(iB);
        //            list.RemoveAt(iA);
        //        }

        //        var newParent =  await AddNode(mergedNode, parent);
        //        await AddNode(nodeA, newParent);
        //        await AddNode(nodeB, newParent);
        //    }
        //}

        private async Task<List<Node>> DescendTree(List<Node> ancestors, string concept)
        {
            var detectedAncestors = new List<Node>();

            foreach (var root in ancestors)
            {
                foreach (var child in root.Children)
                {
                    var relation = await GetRelation(child.Description, concept);

                    if (relation == 1) //Ancestor
                    {
                        detectedAncestors.Add(child);
                        break;
                    }
                    else if (relation == 0) // Same node detected
                    {
                        return [root];
                    }
                }
            }

            if (!detectedAncestors.Any()) //Node of children could be an ancestor
                return ancestors;

            return await DescendTree(detectedAncestors, concept);
        }

        private async Task<(int, int)> FindTwoMostSimilarNodes(List<Node> list)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
GIven the context '{overview}', read the following concepts and determine the two that are most similar in meaning. Only return comma sepatated 0-based index of the two most similar concepts.

{string.Join("\n", list.Select(n => n.Description))}";
            var indexes = await gpt.SendMessage(prompt);

            var indexList = indexes.Split(',').Select(i => int.Parse(i.Trim())).ToList();
            return (indexList[0], indexList[1]);
        }

        private async Task<List<Node>> Split(List<Node> list)
        {
            var gpt = new GptService(openAIKey);

            var concepts = string.Join('\n', list.Select(n => $"Summary: {n.Summary}, Details: {n.Details}"));
            string prompt = $@"
GIven the context '{overview}', read the following concepts and split them into two or more group given their similarity in meaning. Only return comma sepatated 0-based index of the each group followed by a dash and the new parent. The new parent should be a minimal, semantically meaningful general category that they all fall under. Example:
0,1,2 - finance advisor
3,4 - tax advisor
Constraints:
- The parent must be a sub category of '{list[0].Parent.Description}'
- Try to reduce less groups as possible.

{concepts}";
            var response = await gpt.SendMessage(prompt);
            var groups = response.Split('\n').ToList();

            var groupNodes = new List<Node>();
            foreach (var groupInfo in groups)
            {
                var groupIndexes = groupInfo.Split('-')[0].Trim().Split(',').Select(i => int.Parse(i.Trim())).ToList();
                var groupParent = groupInfo.Split('-')[1].Trim();
                
                var groupNode = new Node(groupParent);
                var group = groupIndexes.Select(i => list[i]).ToList();
                groupNode.Children = group;
                group.ForEach(n => n.Parent = groupNode);
                groupNodes.Add(groupNode);
            }

            return groupNodes;
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
You are an ontology validator.

Your task is to find a concept from the list below that has the **same functional and semantic meaning** as the following concept: '{concept}'.

Rules:
- Two concepts must refer to essentially the same idea, purpose, or function.
- Mere domain overlap (e.g. both being in finance or tech) is not enough.
- If none of the listed concepts match strictly, return <NONE>.
- If more than one concept matches, return just one randomly.
- Output the matching concept as-is, or <NONE>.

List:
{string.Join('\n', nodes.Select(c => c.Description))}";

            var response = await gpt.SendMessage(prompt);
            return nodes.FirstOrDefault(n => n.Description == response);
        }
   
        private async Task<Node> MergeConcepts(Node node1, Node node2)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
You are building a precise ontology.

Your task: Given two specific concepts suggest a minimal, semantically meaningful general category that they both fall under. Just return the generated summary and details of the merged concept separated by a comma (don't include words summary or details).
Concept1: Summary: {node1.Summary}, Details: {node1.Details}
Concept2: Summary: {node2.Summary}, Details: {node2.Details}

Constraints:
- The merged category must be a subtype of '{node1.Parent.Description}'.";

            var merged = await gpt.SendMessage(prompt);
            //if (merged == "<CANNOT>")
            //    return null;

            var concept = merged.Split(',');
            return new Node(concept[0], concept[1]);
        }

        private async Task<Node> MergeConcepts(List<Node> nodes)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
You are building a precise ontology.

Your task: Given specific concepts suggest a minimal, semantically meaningful general category that they all fall under. Just return the generated summary and details of the merged concept separated by a comma without including words summary or details.
{string.Join('\n', nodes.Select(n => $"Summary: {n.Summary}, Details: {n.Details}"))}

Constraints:
- The merged category must be a subtype of '{nodes[0].Parent.Description}'.";

            var merged = await gpt.SendMessage(prompt);
            var concept = merged.Split(',');
            var summary = concept[0].Contains(":") ? concept[0].Split(':')[1].Trim() : concept[0];
            var details = concept[1].Contains(":") ? concept[1].Split(':')[1].Trim() : concept[1];
            return new Node(summary, details);
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

        private async Task<List<int>> MatchToBucket(Entity entity)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Pick the buckets that provided concept best belongs to. ONLY return the matching bucket's index (NO TEXT) separated by comma.

Concept : '{entity.FullExplanation}'

Buckets 2: {buckets}
";
            var response = (await gpt.SendMessage(prompt)).Trim();

            if (string.IsNullOrEmpty(response))
                return [];
            if (response.Contains(","))
                return response.Split(',').Select(b => int.Parse(b.Trim())).ToList();
            return [int.Parse(response)];
        }

        private async Task<int> GetRelation(string superConcept, string subConcept)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
Given this concepts: 
Concept A: '{superConcept}'
Concept B: '{subConcept}'

Return 1 if Concept A is a more general category that includes Concept B as a specific type or instance. 
Return 0 if both concepts have the same meaning.
Return -1 otherwise.

Important:
- Consider real-world hierarchical categories.
- Be strict in judgment: opposite or unrelated domains should return -1.
- Respond ONLY -1,0, or 1 without any extra text or charactors";
            var scoreText = await gpt.SendMessage(prompt);
            return Convert.ToInt32(int.Parse(scoreText));
        }

        #region Query

        public async Task<QueryResponse> Query(string groupQuery, string entityQuery, RequiredMatchLevel requiredGroupMatchLevel = RequiredMatchLevel.Medium, RequiredMatchLevel requiredEntityMatchLevel = RequiredMatchLevel.High)
        {
            var matchingEntities = new List<MatchedEntity>();
            var response = new QueryResponse();

            foreach (var root in rootList)
            {
                var matchingEntity = new MatchedEntity {  Path = [root] };
                matchingEntities.AddRange(await QueryDescendents(response, matchingEntity, groupQuery, entityQuery, requiredGroupMatchLevel, requiredEntityMatchLevel));
            }

            response.MatchedEntities = matchingEntities;
            return response;
        }

        private async Task<List<MatchedEntity>> QueryDescendents(QueryResponse response, MatchedEntity matchedEntity, string groupQuery, string entityQuery, RequiredMatchLevel requiredGroupMatchLevel, RequiredMatchLevel requiredEntityMatchLevel)
        {
            double getNumericLevel(RequiredMatchLevel level)
            {
                return level switch
                {
                    RequiredMatchLevel.Extreme => 0.9,
                    RequiredMatchLevel.High => 0.7,
                    RequiredMatchLevel.Medium => 0.5,
                    RequiredMatchLevel.Low => 0.3,
                    RequiredMatchLevel.Any => 0.1
                };
            }

            var groupMatchLevel = getNumericLevel(requiredGroupMatchLevel);
            var entityMatchLevel = getNumericLevel(requiredEntityMatchLevel);

            List<MatchedEntity> matchingEntities = [];


            if (matchedEntity.Path.Last().Children.Count == 0)
            {
                var statement = string.Format(entityQuery, matchedEntity.Path.Last().Description);
                double sim = await Match(statement);
                response.ApiCalls++;

                //debugService.WriteLine($"ENT {sim} : {statement} - {sim >= entityMatchLevel}");
                if (sim >= entityMatchLevel)
                {
                    matchedEntity.Id = matchedEntity.Path.Last().EntityId.Value;
                    matchedEntity.Score = sim;
                    matchingEntities.Add(matchedEntity);
                }
            }
            else
            {
                var statement = string.Format(groupQuery, matchedEntity.Path.Last().Description);
                double sim = await Match(statement);
                response.ApiCalls++;

                //debugService.WriteLine($"GRP {sim} : {statement} - {sim >= groupMatchLevel}");
                if (sim >= groupMatchLevel)
                {
                    foreach (var child in matchedEntity.Path.Last().Children)
                    {
                        var newMatchedEntity = new MatchedEntity { Path = [.. matchedEntity.Path] };

                        newMatchedEntity.Path.Add(child);
                        matchingEntities.AddRange(await QueryDescendents(response, newMatchedEntity, groupQuery, entityQuery, requiredGroupMatchLevel, requiredEntityMatchLevel));
                    }
                }
            }

            return matchingEntities;
        }
        private async Task<double> Match(string statement)
        {
            var gpt = new GptService(openAIKey);

            string prompt = $@"
You are a reasoning engine that evaluates how factually or logically accurate a given statement is. 
Your output is a single numeric score from 0.0 to 1.0, where:

- 1.0 = The statement is clearly and completely true or logically valid.
- 0.8 = Mostly true, but may require mild assumptions or contain minor caveats.
- 0.5 = Partially true, or highly dependent on context, interpretation, or assumptions.
- 0.2 = Mostly false, with limited fragments of truth or plausibility.
- 0.0 = Clearly false or logically invalid.

Use real-world knowledge and logical reasoning. Do not invent missing context, and do not assume capabilities beyond what's explicitly stated.

Respond ONLY with the numeric score.

Statement: '{statement}'";
            var scoreText = await gpt.SendMessage(prompt, 0);
            return double.Parse(scoreText);
        }

        #endregion

        #region Load/Save Tree

        public void Load(string path)
        {
            try
            {
                rootList = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(path), new JsonSerializerSettings
                {
                    MaxDepth = 128 
                });
            }
            catch (Exception ex)
            {
            }
        }

        public void SaveTree(string path) =>
            File.WriteAllText(path, JsonConvert.SerializeObject(rootList, new JsonSerializerSettings {  ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.Indented}));

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