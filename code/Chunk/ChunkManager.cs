using Sandbox;
using System;
using System.Collections.Generic;

public sealed class ChunkManager : Component
{
	[Property] public int ChunkSize { get; set; } = 700;
	[Property] public GameObject Player { get; set; }
	[Property] public int Seed { get; set; } = 1234;
	[Property] public GameObject[] ChunkPrefabs { get; set; }
	[Property] public int RenderDistance { get; set; } = 2;

	private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
	// Stocke pour chaque position de chunk, quelle est sa position master (coin supérieur gauche du multi-chunk)
	private Dictionary<Vector2Int, Vector2Int> chunkToMaster = new Dictionary<Vector2Int, Vector2Int>();

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

		Log.Info($"Chunk actuel - X: {chunkX}, Y: {chunkY} | Position: ({playerPos.x:F1}, {playerPos.y:F1})");

		// Charger et décharger les chunks autour du joueur
		UpdateChunks(chunkX, chunkY);
	}

	public int GetChunkType(int chunkX, int chunkY)
	{
		string sousSeed = $"{Seed}_{chunkX}_{chunkY}";
		int hashSousSeed = sousSeed.GetHashCode();

		int totalWeight = 0;
		for ( int i = 0; i < ChunkPrefabs.Length; i++ )
		{
			var chunkComp = ChunkPrefabs[i].GetComponent<Chunk>();
			totalWeight += chunkComp.Weight;
		}

		int r =  new Random(hashSousSeed).Next( 0, totalWeight );

		var chunkType = 0;
		for ( int i = 0; i < ChunkPrefabs.Length; i++ )
		{
			var chunkComp = ChunkPrefabs[i].GetComponent<Chunk>();
			r -= chunkComp.Weight;
			if ( r <= 0 )
			{
				chunkType = i;
				break;
			}
		}

		return chunkType;
	}

	public void UpdateChunks(int playerChunkX, int playerChunkY)
	{
		// Charger les chunks autour du joueur
		for ( int x = playerChunkX - RenderDistance; x <= playerChunkX + RenderDistance; x++ )
		{
			for ( int y = playerChunkY - RenderDistance; y <= playerChunkY + RenderDistance; y++ )
			{
				Vector2Int chunkCoord = new Vector2Int(x, y);

				// Trouver la position master de ce chunk
				Vector2Int masterPos = GetMasterChunkPosition(chunkCoord);

				// Vérifier si le chunk master est déjà chargé
				if ( !loadedChunks.ContainsKey(masterPos) )
				{
					// Charger le chunk master
					LoadChunk(masterPos);
				}
			}
		}

		// Décharger les chunks trop éloignés
		UnloadDistantChunks(playerChunkX, playerChunkY);
	}

	// Détermine la position master (coin supérieur gauche) du chunk qui occupe cette position
	private Vector2Int GetMasterChunkPosition(Vector2Int chunkCoord)
	{
		// Si déjà dans le cache, retourner directement
		if ( chunkToMaster.TryGetValue(chunkCoord, out Vector2Int cachedMaster) )
		{
			return cachedMaster;
		}

		// Déterminer quelle structure devrait être à cette position
		// On teste TOUTES les positions qui pourraient être le master (pour un chunk 2x2 max)
		// On commence par les offsets les plus éloignés pour donner priorité aux grands chunks
		Vector2Int? foundMaster = null;
		int maxSize = 0;

		for ( int offsetX = -1; offsetX <= 0; offsetX++ )
		{
			for ( int offsetY = -1; offsetY <= 0; offsetY++ )
			{
				Vector2Int testMasterPos = new Vector2Int(chunkCoord.x + offsetX, chunkCoord.y + offsetY);
				int chunkTypeIndex = GetChunkType(testMasterPos.x, testMasterPos.y);

				if ( chunkTypeIndex < 0 || chunkTypeIndex >= ChunkPrefabs.Length )
					continue;

				var chunkComp = ChunkPrefabs[chunkTypeIndex].GetComponent<Chunk>();
				if ( chunkComp == null )
					continue;

				// Vérifier si cette position master contient notre chunk cible
				int endX = testMasterPos.x + chunkComp.GridWidth;
				int endY = testMasterPos.y + chunkComp.GridHeight;

				if ( chunkCoord.x >= testMasterPos.x && chunkCoord.x < endX &&
					 chunkCoord.y >= testMasterPos.y && chunkCoord.y < endY )
				{
					// Calculer la taille totale de ce chunk
					int size = chunkComp.GridWidth * chunkComp.GridHeight;

					// Donner priorité au chunk le plus grand
					if ( size > maxSize )
					{
						maxSize = size;
						foundMaster = testMasterPos;
					}
				}
			}
		}

		// Utiliser le master trouvé ou la position elle-même par défaut
		Vector2Int finalMaster = foundMaster ?? chunkCoord;
		chunkToMaster[chunkCoord] = finalMaster;
		return finalMaster;
	}

	private void LoadChunk(Vector2Int chunkCoord)
	{
		if ( ChunkPrefabs == null || ChunkPrefabs.Length == 0 )
			return;

		// Obtenir le type de chunk basé sur les coordonnées
		int chunkTypeIndex = GetChunkType(chunkCoord.x, chunkCoord.y);
		GameObject chunkPrefab = ChunkPrefabs[chunkTypeIndex];

		// Obtenir les dimensions du chunk
		var chunkComp = chunkPrefab.GetComponent<Chunk>();
		if ( chunkComp == null )
			return;

		int gridWidth = chunkComp.GridWidth;
		int gridHeight = chunkComp.GridHeight;

		// Placer le chunk dans le monde
		GameObject chunkObj = PlaceChunk(chunkPrefab, chunkCoord);

		// Marquer toutes les positions occupées par ce chunk multi-grille
		for ( int x = 0; x < gridWidth; x++ )
		{
			for ( int y = 0; y < gridHeight; y++ )
			{
				Vector2Int occupiedPos = new Vector2Int(chunkCoord.x + x, chunkCoord.y + y);

				// Toutes les positions pointent vers le même GameObject master
				loadedChunks[occupiedPos] = chunkObj;

				// Toutes les positions pointent vers la position master
				chunkToMaster[occupiedPos] = chunkCoord;
			}
		}

		Log.Info($"Chunk chargé - Type: {chunkTypeIndex}, Position master: ({chunkCoord.x}, {chunkCoord.y}), Taille: {gridWidth}x{gridHeight}");
	}

	private void UnloadDistantChunks(int playerChunkX, int playerChunkY)
	{
		HashSet<Vector2Int> masterChunksToUnload = new HashSet<Vector2Int>();

		// Identifier les chunks master trop éloignés
		foreach ( var kvp in loadedChunks )
		{
			Vector2Int chunkCoord = kvp.Key;

			// Obtenir la position master de ce chunk
			if ( !chunkToMaster.TryGetValue(chunkCoord, out Vector2Int masterPos) )
				continue;

			// Vérifier la distance depuis la position master
			int distanceX = Math.Abs(masterPos.x - playerChunkX);
			int distanceY = Math.Abs(masterPos.y - playerChunkY);

			// Si le chunk master est en dehors de la distance de rendu, le marquer
			if ( distanceX > RenderDistance || distanceY > RenderDistance )
			{
				masterChunksToUnload.Add(masterPos);
			}
		}

		// Décharger les chunks master marqués
		foreach ( var masterPos in masterChunksToUnload )
		{
			if ( !loadedChunks.TryGetValue(masterPos, out GameObject chunkObj) )
				continue;

			// Obtenir les dimensions du chunk pour nettoyer toutes les positions occupées
			var chunkComp = chunkObj.GetComponent<Chunk>();
			if ( chunkComp != null )
			{
				for ( int x = 0; x < chunkComp.GridWidth; x++ )
				{
					for ( int y = 0; y < chunkComp.GridHeight; y++ )
					{
						Vector2Int occupiedPos = new Vector2Int(masterPos.x + x, masterPos.y + y);
						loadedChunks.Remove(occupiedPos);
						chunkToMaster.Remove(occupiedPos);
					}
				}
			}

			// Détruire l'objet
			chunkObj.Destroy();
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
