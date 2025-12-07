using Sandbox;

public sealed class Chunk : Component
{
    [Property] public int GridWidth { get; set; } = 1;
    [Property] public int GridHeight { get; set; } = 1;
	[Property] public int Weight { get; set; } = 1;
}
