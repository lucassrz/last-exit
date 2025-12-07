using Sandbox;

namespace LastExit;

public sealed class GameManager : Component
{
	[Property] public GameObject CrosshairPrefab { get; set; }
	private GameObject _crosshair;

	protected override void OnStart()
	{
		Mouse.Visibility = MouseVisibility.Auto;
		// Crée l'UI du curseur/viseur
		if ( CrosshairPrefab != null )
		{
			_crosshair = CrosshairPrefab;
		}
	}

	protected override void OnUpdate()
	{
		// Fait suivre le crosshair à la position de la souris
		if ( _crosshair != null )
		{
			var mousePos = Mouse.Position;
			_crosshair.LocalPosition = new Vector3( mousePos.x, mousePos.y, 0 );
		}
	}
}
