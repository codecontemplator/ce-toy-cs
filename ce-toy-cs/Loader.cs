using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ce_toy_cs
{
    public interface ILoader
    {
        string Name { get; }
        int Cost { get; }
        IImmutableSet<string> RequiredKeys { get; }
        IImmutableSet<string> LoadedKeys { get; }
        ImmutableDictionary<string, object> Load(string applicantId, string key, ImmutableDictionary<string, object> input);
    }
    
    class LoadersSelector
    {
        private class Node
        {
            public IImmutableSet<string> KeySet { get; }

            public Node(IImmutableSet<string> keySet)
            {
                KeySet = keySet;
            }

            public bool CompareTo(Node rhs)
            {
                return KeySet.IsSubsetOf(rhs.KeySet);
            }
        }

        private class Edge
        {
            public Edge(ILoader loader)
            {
                Loader = loader;
            }

            public Node Source => new Node(Loader.RequiredKeys);
            public Node Sink => new Node(Loader.LoadedKeys);
            public Node TransitionFrom(Node node)
            {
                return new Node(node.KeySet.Union(Loader.LoadedKeys));
            }

            public int Weight => Loader.Cost;

            public ILoader Loader { get; }
        }

        public static IEnumerable<ILoader> PickOptimizedSet(
            IEnumerable<ILoader> availableLoaders,
            IImmutableSet<string> knownKeys, 
            IImmutableSet<string> requiredKeys
            )
        {
            var solutions = Solve(new Node(knownKeys), new Node(requiredKeys), availableLoaders.Select(x => new Edge(x)));
            if (!solutions.Any())
                throw new ArgumentException("No solution found");
            var bestSolution = solutions.MinBy(x => x.weight);
            return bestSolution.edges.Select(x => x.Loader);
        }

        private static ImmutableList<(int weight,ImmutableList<Edge> edges)> Solve(Node currentNode, Node goalNode, IEnumerable<Edge> edges)
        {
            if (goalNode.CompareTo(currentNode))
                return new[] { (0, ImmutableList<Edge>.Empty) }.ToImmutableList();
            
            var (directEdges, indirectEdges) = Partition(edges, e => e.Source.CompareTo(currentNode));
            var result =
                from re in RemoveEach(directEdges)
                from s in Solve(re.e.TransitionFrom(currentNode), goalNode, re.es.AddRange(indirectEdges))
                select (s.weight + re.e.Weight, s.edges.Insert(0, re.e));

            return result.ToImmutableList();
        }

        private static IEnumerable<(T e, ImmutableList<T> es)> RemoveEach<T>(ImmutableList<T> xs)
        {
            for(int i=0; i<xs.Count; ++i)
            {
                yield return (xs.ElementAt(i), xs.RemoveAt(i));
            }
        }

        private static (ImmutableList<Edge>, ImmutableList<Edge>) Partition(IEnumerable<Edge> edges, Func<Edge,bool> isDirectEdge)
        {
            var directEdges = new List<Edge>();
            var indirectEdges = new List<Edge>();
            foreach(var edge in edges)
            {
                if (isDirectEdge(edge))
                    directEdges.Add(edge);
                else
                    indirectEdges.Add(edge);
            }
            return (directEdges.ToImmutableList(), indirectEdges.ToImmutableList());
        }
    }
}
