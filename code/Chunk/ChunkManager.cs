using Sandbox;
using System;
using System.Collections.Generic;

public sealed class ChunkManager : Component
{
	[Property] public int ChunkSize { get; set; } = 200;
	[Property] public GameObject Player { get; set; }
	[Property] public int Seed { get; set; } = 1234;
	[Property] public int RenderDistance { get; set; } = 2;

	// Configuration des biomes
	[Property] public BiomeConfig[] Biomes { get; set; }
	[Property] public float BiomeNoiseScale { get; set; } = 0.1f;

	private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();

	protected override void OnUpdate()
	{
		CheckChunks();
	}

	public void CheckChunks()
	{
		if ( Player == null )
			return;

		var playerPos = Player.WorldPosition;

		int chunkX = (int)Math.Floor((playerPos.x + ChunkSize / 2f) / ChunkSize);
		int chunkY = (int)Math.Floor((playerPos.y + ChunkSize / 2f) / ChunkSize);

		UpdateChunks(chunkX, chunkY);
	}

	public void UpdateChunks(int playerChunkX, int playerChunkY)
	{
		// Charger les chunks autour du joueur
		for ( int x = playerChunkX - RenderDistance; x <= playerChunkX + RenderDistance; x++ )
		{
			for ( int y = playerChunkY - RenderDistance; y <= playerChunkY + RenderDistance; y++ )
			{
				Vector2Int chunkCoord = new Vector2Int(x, y);

				if ( !loadedChunks.ContainsKey(chunkCoord) )
				{
					LoadChunk(chunkCoord);
				}
			}
		}

		UnloadDistantChunks(playerChunkX, playerChunkY);
	}

	// Génère une valeur de Perlin-like Noise pour une position donnée (entre 0 et 1)
	private float GetPerlinNoise(int x, int y, float scale, int seedOffset = 0)
	{
		float sampleX = (x + seedOffset) * scale;
		float sampleY = (y + seedOffset) * scale;

		return SimplexNoise(sampleX + Seed, sampleY + Seed);
	}

	// Fonction de bruit simplifié (retourne une valeur entre 0 et 1)
	private float SimplexNoise(float x, float y)
	{
		float ix = MathF.Floor(x);
		float iy = MathF.Floor(y);
		float fx = x - ix;
		float fy = y - iy;

		float sx = fx * fx * (3.0f - 2.0f * fx);
		float sy = fy * fy * (3.0f - 2.0f * fy);

		float n00 = Hash2D(ix, iy);
		float n10 = Hash2D(ix + 1, iy);
		float n01 = Hash2D(ix, iy + 1);
		float n11 = Hash2D(ix + 1, iy + 1);

		float nx0 = Lerp(n00, n10, sx);
		float nx1 = Lerp(n01, n11, sx);
		return Lerp(nx0, nx1, sy);
	}

	// Hash 2D déterministe (retourne entre 0 et 1)
	private float Hash2D(float x, float y)
	{
		float h = MathF.Sin(x * 127.1f + y * 311.7f + Seed * 0.1f) * 43758.5453123f;
		h -= MathF.Floor(h);

		float h2 = MathF.Sin(x * 269.5f + y * 183.3f + Seed * 0.2f) * 27183.1459f;
		h2 -= MathF.Floor(h2);

		return (h + h2) * 0.5f;
	}

	// Interpolation linéaire
	private float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	// Détermine le biome basé sur le Perlin Noise
	private BiomeConfig GetBiomeForChunk(Vector2Int chunkCoord, out float noiseValue)
	{
		noiseValue = GetPerlinNoise(chunkCoord.x, chunkCoord.y, BiomeNoiseScale);

		if ( Biomes == null || Biomes.Length == 0 )
			return null;

		// Trouver le biome correspondant à la valeur de noise
		foreach ( var biome in Biomes )
		{
			if ( biome != null && biome.MatchesNoise(noiseValue) )
			{
				return biome;
			}
		}

		// Retourner le premier biome par défaut
		return Biomes[0];
	}

	private void LoadChunk(Vector2Int chunkCoord)
	{
		// Déterminer le biome avec Perlin Noise
		BiomeConfig biome = GetBiomeForChunk(chunkCoord, out float noiseValue);

		if ( biome == null || biome.GroundPrefab == null )
		{
			Log.Warning($"Aucun biome trouvé pour le chunk ({chunkCoord.x}, {chunkCoord.y}) - Noise: {noiseValue:F3}");
			return;
		}

		// Placer le chunk dans le monde
		GameObject chunkObj = PlaceChunk(biome.GroundPrefab, chunkCoord);

		// Enregistrer le chunk
		loadedChunks[chunkCoord] = chunkObj;

		// Générer les objets avec le biome
		GenerateObjects(chunkObj, chunkCoord, biome);

		Log.Info($"Chunk ({chunkCoord.x}, {chunkCoord.y}) - Biome: {biome.BiomeName} (Noise: {noiseValue:F3})");
	}

	// Génère des objets dans le chunk selon le biome
	private void GenerateObjects(GameObject chunk, Vector2Int chunkCoord, BiomeConfig biome)
	{
		if ( biome.SpawnablePrefabs == null || biome.SpawnablePrefabs.Length == 0 )
			return;

		int objectCount = 0;
		Random random = new Random(Seed + chunkCoord.x * 73856093 + chunkCoord.y * 19349663);

		// Essayer de placer des objets à différentes positions
		for ( int i = 0; i < biome.MaxObjectsPerChunk * 2; i++ )
		{
			// Position aléatoire dans le chunk
			float localX = (float)random.NextDouble() * ChunkSize;
			float localY = (float)random.NextDouble() * ChunkSize;

			// Utiliser Perlin noise pour décider si on place un objet ici
			float worldX = chunkCoord.x * ChunkSize + localX;
			float worldY = chunkCoord.y * ChunkSize + localY;
			float objectNoise = GetPerlinNoise((int)worldX, (int)worldY, biome.ObjectNoiseScale, seedOffset: 5000);

			// Placer un objet si le bruit est favorable
			if ( objectNoise > (1f - biome.ObjectDensity) && objectCount < biome.MaxObjectsPerChunk )
			{
				// Choisir un prefab aléatoire
				GameObject objectPrefab = biome.SpawnablePrefabs[random.Next(biome.SpawnablePrefabs.Length)];
				GameObject obj = objectPrefab.Clone();

				// Positionner l'objet
				obj.WorldPosition = new Vector3(worldX, worldY, 0);

				// Rotation aléatoire
				obj.WorldRotation = Rotation.FromYaw(random.Next(0, 360));

				// Attacher au chunk
				obj.SetParent(chunk);

				objectCount++;
			}
		}

		if ( objectCount > 0 )
		{
			Log.Info($"  → {objectCount} objets générés");
		}
	}

	private void UnloadDistantChunks(int playerChunkX, int playerChunkY)
	{
		List<Vector2Int> chunksToUnload = new List<Vector2Int>();

		foreach ( var kvp in loadedChunks )
		{
			Vector2Int chunkCoord = kvp.Key;
			int distanceX = Math.Abs(chunkCoord.x - playerChunkX);
			int distanceY = Math.Abs(chunkCoord.y - playerChunkY);

			if ( distanceX > RenderDistance || distanceY > RenderDistance )
			{
				chunksToUnload.Add(chunkCoord);
			}
		}

		foreach ( var chunkCoord in chunksToUnload )
		{
			if ( loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObj) )
			{
				chunkObj.Destroy();
				loadedChunks.Remove(chunkCoord);
			}
		}
	}

	public GameObject PlaceChunk(GameObject chunkPrefab, Vector2Int gridCoord)
	{
		GameObject chunkClone = chunkPrefab.Clone();

		float worldX = gridCoord.x * ChunkSize;
		float worldY = gridCoord.y * ChunkSize;

		chunkClone.WorldPosition = new Vector3(worldX, worldY, 0);

		return chunkClone;
	}
}
