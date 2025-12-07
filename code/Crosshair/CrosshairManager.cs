using Sandbox;

namespace LastExit;

public sealed class CrosshairManager : Component
{
	public static Vector3 TargetPosition { get; private set; }
	public static Vector3 TargetDirection { get; private set; }
	[Property] public CameraComponent Camera { get; set; }
	[Property] public float MaxDistance { get; set; } = 10000f;

	protected override void OnAwake()
	{
		if ( Camera == null )
		{
			Camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		}

		// Cache le curseur Windows pour n'utiliser que le crosshair 3D
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

		var mousePos = Mouse.Position;

		var ray = Camera.ScreenPixelToRay( mousePos );

		var trace = Scene.Trace.Ray( ray, MaxDistance )
			.WithoutTags( "player" )
			.Run();

		if ( trace.Hit )
		{
			TargetPosition = trace.HitPosition;
			TargetDirection = (trace.HitPosition - ray.Position).Normal;
		}
		else
		{
			TargetPosition = ray.Position + ray.Forward * MaxDistance;
			TargetDirection = ray.Forward;
		}
	}

	public static Vector3 GetHorizontalDirectionFrom( Vector3 position )
	{
		var direction = TargetPosition - position;
		direction.z = 0;
		return direction.Normal;
	}
}
