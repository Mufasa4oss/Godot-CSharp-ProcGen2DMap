using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Simple implementation of Prim's algorithm for Minimum Spanning Tree
/// 
/// Prim's Algorithm Steps:
/// 1. Start with any vertex (we'll use vertex 0)
/// 2. Keep track of visited vertices
/// 3. Find the smallest edge that connects a visited vertex to an unvisited vertex
/// 4. Add that edge to our MST and mark the new vertex as visited
/// 5. Repeat until all vertices are visited
/// </summary>
public class PrimsAlgorithmSimple
{
	/// <summary>
	/// Represents an edge with a weight
	/// </summary>
	public class Edge
	{
		public int From, To;
		public double Weight;
		
		public Edge(int from, int to, double weight)
		{
			From = from;
			To = to;
			Weight = weight;
		}
	}
	
	private List<Edge> edges;
	private int vertexCount;
	
	public PrimsAlgorithmSimple(int numVertices)
	{
		vertexCount = numVertices;
		edges = new List<Edge>();
	}
	
	/// <summary>
	/// Adds an undirected edge to the graph
	/// </summary>
	public void AddEdge(int from, int to, double weight)
	{
		if (from < 0 || from >= vertexCount || to < 0 || to >= vertexCount)
			throw new ArgumentException("Invalid vertex");
			
		// Add edge in both directions for undirected graph
		edges.Add(new Edge(from, to, weight));
		edges.Add(new Edge(to, from, weight));
	}
	
	/// <summary>
	/// Finds the Minimum Spanning Tree using Prim's algorithm
	/// </summary>
	public List<Edge> FindMST()
	{
		if (vertexCount < 2) return new List<Edge>();
		
		List<Edge> mst = new List<Edge>();
		HashSet<int> visited = new HashSet<int>();
		
		// Start with vertex 0
		visited.Add(0);
		
		// Keep going until we've visited all vertices
		while (visited.Count < vertexCount)
		{
			Edge smallestEdge = null;
			double smallestWeight = double.MaxValue;
			
			// Find the smallest edge that connects visited to unvisited
			foreach (Edge edge in edges)
			{
				bool fromVisited = visited.Contains(edge.From);
				bool toVisited = visited.Contains(edge.To);
				
				// We want exactly one vertex visited and one unvisited
				if (fromVisited && !toVisited && edge.Weight < smallestWeight)
				{
					smallestEdge = edge;
					smallestWeight = edge.Weight;
				}
			}
			
			// If no edge found, graph is disconnected
			if (smallestEdge == null)
				break;
				
			// Add the edge to MST and mark the new vertex as visited
			mst.Add(smallestEdge);
			visited.Add(smallestEdge.To);
		}
		
		return mst;
	}
	
	/// <summary>
	/// Prints the MST edges and total weight
	/// </summary>
	public void PrintMST(List<Edge> mst)
	{
		GD.Print("Minimum Spanning Tree:");
		double totalWeight = 0;
		
		foreach (Edge edge in mst)
		{
			GD.Print($"  {edge.From} -> {edge.To} (weight: {edge.Weight})");
			totalWeight += edge.Weight;
		}

		GD.Print($"Total weight: {totalWeight}");
	}
}

/// <summary>
/// Example usage
/// </summary>
