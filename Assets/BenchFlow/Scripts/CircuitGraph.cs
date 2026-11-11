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
            if (string.IsNullOrWhiteSpace(node)) return;
            node = node.Trim();
            if (!parent.ContainsKey(node)) parent[node] = node;
        }

        public void Connect(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return;
            a = a.Trim();
            b = b.Trim();
            RegisterNode(a); RegisterNode(b);
            string rootA = Find(a), rootB = Find(b);
            if (rootA != rootB)
            {
                // Choose the same representative regardless of the order in which wires are added.
                if (string.CompareOrdinal(rootA, rootB) <= 0) parent[rootB] = rootA;
                else parent[rootA] = rootB;
            }
        }

        public bool AreConnected(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            a = a.Trim();
            b = b.Trim();
            return parent.ContainsKey(a) && parent.ContainsKey(b) && Find(a) == Find(b);
        }

        public bool ContainsNode(string node) => !string.IsNullOrWhiteSpace(node) && parent.ContainsKey(node.Trim());

        string Find(string node)
        {
            string root = node;
            while (parent[root] != root) root = parent[root];
            while (parent[node] != node)
            {
                string next = parent[node];
                parent[node] = root;
                node = next;
            }
            return root;
        }
    }
}
