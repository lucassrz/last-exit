using Sandbox;
using System;
using System.Collections.Generic;

public sealed class ChunkManager : Component
{
	[Property] public int ChunkSize { get; set; } = 200;
	[Property] public GameObject Player { get; set; }
	[Property] public int Seed { get; set; } = 1234;
	[Property] public int RenderDistance { get; set; } = 2;

	// Prefabs pour les différents biomes
	[Property] public GameObject ForestPrefab { get; set; }
	[Property] public GameObject[] TreePrefabs { get; set; }
	[Property] public GameObject PlainsPrefab { get; set; }

	// Paramètres du Perlin Noise
	[Property] public float BiomeNoiseScale { get; set; } = 0.1f; // Échelle du bruit pour les biomes (plus grand = biomes plus petits)
	[Property] public float BiomeThreshold { get; set; } = 0.7f; // Seuil forêt/plaines
	[Property] public float TreeNoiseScale { get; set; } = 0.3f; // Échelle pour placement d'arbres
	[Property] public float TreeDensity { get; set; } = 0.5f; // Densité des arbres (0-1)
	[Property] public int TreesPerChunk { get; set; } = 8; // Nombre max d'arbres par chunk

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

		// Charger et décharger les chunks autour du joueur
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

		// Décharger les chunks trop éloignés
		UnloadDistantChunks(playerChunkX, playerChunkY);
	}

	// Génère une valeur de Perlin-like Noise pour une position donnée (entre 0 et 1)
	private float GetPerlinNoise(int x, int y, float scale, int seedOffset = 0)
	{
		float sampleX = (x + seedOffset) * scale;
		float sampleY = (y + seedOffset) * scale;

		// Utiliser une fonction de hash déterministe basée sur la seed
		return SimplexNoise(sampleX + Seed, sampleY + Seed);
	}

	// Fonction de bruit simplifié (retourne une valeur entre 0 et 1)
	private float SimplexNoise(float x, float y)
	{
		// Interpolation pour rendre le bruit plus smooth
		float ix = MathF.Floor(x);
		float iy = MathF.Floor(y);
		float fx = x - ix;
		float fy = y - iy;

		// Smooth interpolation (fonction smoothstep)
		float sx = fx * fx * (3.0f - 2.0f * fx);
		float sy = fy * fy * (3.0f - 2.0f * fy);

		// Générer du bruit aux 4 coins
		float n00 = Hash2D(ix, iy);
		float n10 = Hash2D(ix + 1, iy);
		float n01 = Hash2D(ix, iy + 1);
		float n11 = Hash2D(ix + 1, iy + 1);

		// Interpolation bilinéaire des 4 coins
		float nx0 = Lerp(n00, n10, sx);
		float nx1 = Lerp(n01, n11, sx);
		float result = Lerp(nx0, nx1, sy);

		// Normaliser pour bien couvrir la plage 0-1
		return result;
	}

	// Hash 2D déterministe (retourne entre 0 et 1)
	private float Hash2D(float x, float y)
	{
		// Utiliser plusieurs composantes pour plus de variation
		float h = MathF.Sin(x * 127.1f + y * 311.7f + Seed * 0.1f) * 43758.5453123f;
		h = h - MathF.Floor(h);

		// Mélanger avec une seconde couche pour plus de randomness
		float h2 = MathF.Sin(x * 269.5f + y * 183.3f + Seed * 0.2f) * 27183.1459f;
		h2 = h2 - MathF.Floor(h2);

		// Combiner les deux
		return (h + h2) * 0.5f;
	}

	// Interpolation linéaire
	private float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	// Détermine le type de biome basé sur le Perlin Noise
	private GameObject GetBiomeForChunk(Vector2Int chunkCoord)
	{
		float biomeNoise = GetPerlinNoise(chunkCoord.x, chunkCoord.y, BiomeNoiseScale);

		// Debug : afficher les valeurs de bruit
		string biomeType = biomeNoise < BiomeThreshold ? "Forêt" : "Plaines";
		Log.Info($"Chunk ({chunkCoord.x}, {chunkCoord.y}) - Noise: {biomeNoise:F3} → {biomeType}");

		// En dessous du seuil = Forêt, au-dessus = Plaines
		if ( biomeNoise < BiomeThreshold )
		{
			return ForestPrefab;
		}
		else
		{
			return PlainsPrefab;
		}
	}

	private void LoadChunk(Vector2Int chunkCoord)
	{
		// Déterminer le biome avec Perlin Noise
		GameObject biomePrefab = GetBiomeForChunk(chunkCoord);

		if ( biomePrefab == null )
		{
			Log.Warning($"Aucun prefab trouvé pour le chunk ({chunkCoord.x}, {chunkCoord.y})");
			return;
		}

		// Placer le chunk dans le monde
		GameObject chunkObj = PlaceChunk(biomePrefab, chunkCoord);

		// Enregistrer le chunk
		loadedChunks[chunkCoord] = chunkObj;

		// Générer les objets (arbres, etc.) avec le second Perlin Noise
		GenerateObjects(chunkObj, chunkCoord);

		Log.Info($"Chunk chargé - Position: ({chunkCoord.x}, {chunkCoord.y}), Biome: {biomePrefab.Name}");
	}

	// Génère des objets dans le chunk (arbres, rochers, etc.)
	private void GenerateObjects(GameObject chunk, Vector2Int chunkCoord)
	{
		// Vérifier si c'est une forêt
		float biomeNoise = GetPerlinNoise(chunkCoord.x, chunkCoord.y, BiomeNoiseScale);
		bool isForest = biomeNoise < BiomeThreshold;

		if ( !isForest || TreePrefabs == null || TreePrefabs.Length == 0 )
			return;

		// Générer des arbres dans la forêt
		int treeCount = 0;
		Random random = new Random(Seed + chunkCoord.x * 73856093 + chunkCoord.y * 19349663);

		// Essayer de placer des arbres à différentes positions
		for ( int i = 0; i < TreesPerChunk * 2; i++ )
		{
			// Position aléatoire dans le chunk
			float localX = (float)random.NextDouble() * ChunkSize;
			float localY = (float)random.NextDouble() * ChunkSize;

			// Utiliser Perlin noise pour décider si on place un arbre ici
			float worldX = chunkCoord.x * ChunkSize + localX;
			float worldY = chunkCoord.y * ChunkSize + localY;
			float treeNoise = GetPerlinNoise((int)worldX, (int)worldY, TreeNoiseScale, seedOffset: 5000);

			// Placer un arbre si le bruit est favorable
			if ( treeNoise > (1f - TreeDensity) && treeCount < TreesPerChunk )
			{
				// Choisir un prefab d'arbre aléatoire
				GameObject treePrefab = TreePrefabs[random.Next(TreePrefabs.Length)];
				GameObject tree = treePrefab.Clone();

				// Positionner l'arbre
				tree.WorldPosition = new Vector3(worldX, worldY, 0);

				// Rotation aléatoire
				tree.WorldRotation = Rotation.FromYaw(random.Next(0, 360));

				// Attacher au chunk pour le déchargement automatique
				tree.SetParent(chunk);

				treeCount++;
			}
		}

		if ( treeCount > 0 )
		{
			Log.Info($"Chunk ({chunkCoord.x}, {chunkCoord.y}) - {treeCount} arbres générés");
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
