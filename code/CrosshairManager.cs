using Sandbox;

namespace LastExit;

/// <summary>
/// Gère le crosshair et la position du pointeur dans le monde
/// </summary>
public sealed class CrosshairManager : Component
{
	/// <summary>
	/// Position du crosshair dans le monde (via raycast depuis le centre de l'écran)
	/// </summary>
	public static Vector3 TargetPosition { get; private set; }

	/// <summary>
	/// Direction du regard depuis la caméra vers le crosshair
	/// </summary>
	public static Vector3 TargetDirection { get; private set; }

	/// <summary>
	/// La caméra utilisée pour le raycast
	/// </summary>
	[Property] public CameraComponent Camera { get; set; }

	/// <summary>
	/// Distance maximale du raycast
	/// </summary>
	[Property] public float MaxDistance { get; set; } = 10000f;

	protected override void OnAwake()
	{
		// Trouve la caméra automatiquement si non assignée
		if ( Camera == null )
		{
			Camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		}

		// Cache le curseur Windows
		Mouse.Visibility = MouseVisibility.Hidden;
	}

	protected override void OnUpdate()
	{
		UpdateCrosshairWorldPosition();
	}

	private void UpdateCrosshairWorldPosition()
	{
		if ( Camera == null )
			return;

		// Utilise la position de la souris pour le crosshair
		var mousePos = Mouse.Position;

		// Crée un rayon depuis la caméra vers la position de la souris
		var ray = Camera.ScreenPixelToRay( mousePos );

		// Effectue un raycast pour trouver où le crosshair pointe dans le monde
		var trace = Scene.Trace.Ray( ray, MaxDistance )
			.WithoutTags( "player" ) // Ignore le joueur
			.Run();

		if ( trace.Hit )
		{
			TargetPosition = trace.HitPosition;
			TargetDirection = (trace.HitPosition - ray.Position).Normal;
		}
		else
		{
			// Si rien n'est touché, utilise un point loin devant
			TargetPosition = ray.Position + ray.Forward * MaxDistance;
			TargetDirection = ray.Forward;
		}
	}

	/// <summary>
	/// Obtient la direction horizontale (2D) vers le crosshair depuis une position donnée
	/// Utile pour orienter le personnage
	/// </summary>
	public static Vector3 GetHorizontalDirectionFrom( Vector3 position )
	{
		var direction = TargetPosition - position;
		direction.z = 0; // Ignore la hauteur
		return direction.Normal;
	}
}
