using System.Collections.Generic;

namespace BenchFlow
{
    /// <summary>Simple deterministic net graph. It models connectivity, not voltage or current.</summary>
    public sealed class CircuitGraph
    {
        readonly Dictionary<string, string> parent = new Dictionary<string, string>();

        public void Clear() => parent.Clear();

        public void RegisterNode(string node)
        {
            if (!parent.ContainsKey(node)) parent[node] = node;
        }

        public void Connect(string a, string b)
        {
            RegisterNode(a); RegisterNode(b);
            string rootA = Find(a), rootB = Find(b);
            if (rootA != rootB) parent[rootB] = rootA;
        }

        public bool AreConnected(string a, string b)
        {
            return parent.ContainsKey(a) && parent.ContainsKey(b) && Find(a) == Find(b);
        }

        string Find(string node)
        {
            string p = parent[node];
            if (p != node) parent[node] = Find(p);
            return parent[node];
        }
    }
}
