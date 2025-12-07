using Sandbox;

[GameResource("Biome Configuration", "biome", "Configuration d'un type de biome", Icon = "🌍")]
public class BiomeConfig : GameResource
{
	[Property] public string BiomeName { get; set; } = "Nouveau Biome";

	// Seuils de Perlin Noise pour ce biome
	[Property, Range(0f, 1f)] public float MinNoiseValue { get; set; } = 0f;
	[Property, Range(0f, 1f)] public float MaxNoiseValue { get; set; } = 0.5f;

	// Prefab du terrain pour ce biome
	[Property] public GameObject GroundPrefab { get; set; }

	// Objets qui peuvent spawner dans ce biome
	[Property] public GameObject[] SpawnablePrefabs { get; set; }

	// Paramètres de spawn des objets
	[Property, Range(0f, 1f)] public float ObjectDensity { get; set; } = 0.3f;
	[Property] public int MaxObjectsPerChunk { get; set; } = 5;
	[Property] public float ObjectNoiseScale { get; set; } = 0.3f;

	// Vérifie si une valeur de noise correspond à ce biome
	public bool MatchesNoise(float noiseValue)
	{
		return noiseValue >= MinNoiseValue && noiseValue < MaxNoiseValue;
	}
}
