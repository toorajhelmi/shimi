namespace Shimi.Shared
{
    public class Entity
    {
        public static int id = 0;

        public virtual string Concept { get; set; } 
        public virtual string Explantion { get; set; }
        public int Id { get; set; } = id++;  

        public override string ToString() => $"[{Id}]: {Concept}";

        public string FullExplanation => Concept + (Explantion == null ? "" : $" (Here is more explanation: {Explantion})");
    }
}
