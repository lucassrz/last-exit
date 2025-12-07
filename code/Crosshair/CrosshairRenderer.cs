using Sandbox;

namespace LastExit;

/// <summary>
/// Affiche un indicateur 3D au sol à la position du crosshair
/// </summary>
public sealed class CrosshairRenderer : Component
{
	/// <summary>
	/// Couleur du point indicateur
	/// </summary>
	[Property] public Color PointColor { get; set; } = Color.Red;

	/// <summary>
	/// Taille du point
	/// </summary>
	[Property] public float PointSize { get; set; } = 5f;

	protected override void OnUpdate()
	{
		DrawGroundIndicator();
	}

	private void DrawGroundIndicator()
	{
		using ( Gizmo.Scope( "ground_indicator" ) )
		{
			var position = CrosshairManager.TargetPosition;

			// Point rouge
			Gizmo.Draw.Color = PointColor;
			Gizmo.Draw.SolidSphere( position, PointSize );
		}
	}
}
