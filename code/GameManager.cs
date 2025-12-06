using Sandbox;

namespace LastExit;

public sealed class GameManager : Component
{
	[Property] public GameObject CrosshairPrefab { get; set; }

	protected override void OnStart()
	{
		// Crée l'UI du curseur/viseur
		if ( CrosshairPrefab != null )
		{
			var crosshair = CrosshairPrefab.Clone();
			crosshair.Parent = GameObject;
		}
	}
}
