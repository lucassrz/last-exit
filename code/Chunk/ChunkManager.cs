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

				// Vérifier si le chunk est déjà chargé
				if ( !loadedChunks.ContainsKey(chunkCoord) )
				{
					// Charger le chunk
					LoadChunk(chunkCoord);
				}
			}
		}

		// Décharger les chunks trop éloignés
		UnloadDistantChunks(playerChunkX, playerChunkY);
	}

	private void LoadChunk(Vector2Int chunkCoord)
	{
		if ( ChunkPrefabs == null || ChunkPrefabs.Length == 0 )
			return;

		// Obtenir le type de chunk basé sur les coordonnées
		int chunkTypeIndex = GetChunkType(chunkCoord.x, chunkCoord.y);

		// Placer le chunk dans le monde
		GameObject chunkObj = PlaceChunk(ChunkPrefabs[chunkTypeIndex], chunkCoord);

		// Ajouter le chunk au dictionnaire
		loadedChunks[chunkCoord] = chunkObj;
	}

	private void UnloadDistantChunks(int playerChunkX, int playerChunkY)
	{
		List<Vector2Int> chunksToUnload = new List<Vector2Int>();

		// Identifier les chunks trop éloignés
		foreach ( var kvp in loadedChunks )
		{
			Vector2Int chunkCoord = kvp.Key;
			int distanceX = Math.Abs(chunkCoord.x - playerChunkX);
			int distanceY = Math.Abs(chunkCoord.y - playerChunkY);

			// Si le chunk est en dehors de la distance de rendu, le marquer pour déchargement
			if ( distanceX > RenderDistance || distanceY > RenderDistance )
			{
				chunksToUnload.Add(chunkCoord);
			}
		}

		// Décharger les chunks marqués
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
