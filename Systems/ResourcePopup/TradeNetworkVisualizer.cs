using Godot;
using System;
using System.Collections.Generic;

// Draws colored packets moving along MST edges to visualize trade flow between capitals.
// Add as a child of Main, then call Initialize() after the MST is built.
public partial class TradeNetworkVisualizer : Node2D
{
    private class TradePacket
    {
        public Vector2 Start;
        public Vector2 End;
        public float Progress; // 0.0 = at Start, 1.0 = at End
        public Color Color;

        public TradePacket(Vector2 start, Vector2 end, Color color)
        {
            Start = start;
            End = end;
            Progress = 0f;
            Color = color;
        }
    }

    private List<(Vector2 From, Vector2 To)> edges = new();
    private List<TradePacket> activePackets = new();

    private float spawnTimer = 0f;
    private const float SpawnInterval = 0.35f; // seconds between new packets
    private const float PacketSpeed  = 0.15f;  // fraction of edge traveled per second
    private const float PacketRadius = 60f;

    private Random rng = new Random();

    //CHANGES 23-Jun-26// (was: colors matched ResourceManager.rawResourceNames) decorative palette until M5 drives real flows
    private static readonly Color[] ResourceColors =
    {
        new Color(0.55f, 0.27f, 0.07f), // Wood      - brown
        new Color(0.60f, 0.60f, 0.60f), // Stone     - gray
        new Color(0.70f, 0.15f, 0.15f), // Iron      - dark red
        new Color(1.00f, 0.85f, 0.00f), // Gold      - yellow
        new Color(0.60f, 0.90f, 0.20f), // Grain     - yellow-green
        new Color(0.90f, 0.50f, 0.20f), // Livestock - orange
        new Color(0.25f, 0.25f, 0.25f), // Coal      - dark gray
    };

    // Call this after the MST is computed in Main._Ready()
    public void Initialize(List<PrimsAlgorithmSimple.Edge> mstEdges, List<Vector2I> capitals, TileMapLayer tml)
    {
        foreach (var edge in mstEdges)
        {
            Vector2 from = tml.MapToLocal(capitals[edge.From]);
            Vector2 to   = tml.MapToLocal(capitals[edge.To]);
            edges.Add((from, to));
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Advance each packet along its edge, remove ones that arrived
        for (int i = activePackets.Count - 1; i >= 0; i--)
        {
            activePackets[i].Progress += PacketSpeed * dt;
            if (activePackets[i].Progress >= 1f)
                activePackets.RemoveAt(i);
        }

        // Spawn a new packet on a random edge
        spawnTimer += dt;
        if (spawnTimer >= SpawnInterval && edges.Count > 0)
        {
            spawnTimer = 0f;
            SpawnPacket();
        }

        QueueRedraw();
    }

    private void SpawnPacket()
    {
        var edge = edges[rng.Next(edges.Count)];

        // Trade flows both ways — randomly pick a direction
        bool reverse = rng.Next(2) == 0;
        Vector2 start = reverse ? edge.To   : edge.From;
        Vector2 end   = reverse ? edge.From : edge.To;

        Color color = ResourceColors[rng.Next(ResourceColors.Length)];
        activePackets.Add(new TradePacket(start, end, color));
    }

    public override void _Draw()
    {
        foreach (var packet in activePackets)
        {
            Vector2 pos = packet.Start.Lerp(packet.End, packet.Progress);
            DrawCircle(pos, PacketRadius, packet.Color);
        }
    }
}
