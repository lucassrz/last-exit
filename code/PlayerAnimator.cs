using Sandbox;

namespace LastExit;

/// <summary>
/// Gère les animations du personnage en fonction de sa vitesse
/// </summary>
public sealed class PlayerAnimator : Component
{
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }

	private PersonnalPlayerController _controller;
	private Vector3 _lastVelocity;
	private int _frameCount;

	protected override void OnAwake()
	{
		// Trouve le controller
		_controller = Components.Get<PersonnalPlayerController>( FindMode.EnabledInSelfAndDescendants );
		if ( _controller == null )
		{
			_controller = Components.GetInAncestors<PersonnalPlayerController>();
		}

		// Trouve le renderer automatiquement s'il n'est pas assigné
		if ( BodyRenderer == null )
		{
			BodyRenderer = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		}

		Log.Info( $"[PlayerAnimator] Controller: {_controller != null}, Renderer: {BodyRenderer != null}" );
	}

	protected override void OnUpdate()
	{
		if ( BodyRenderer == null )
			return;

		UpdateAnimation();
	}

	private void UpdateAnimation()
	{
		// Récupère la vélocité du Rigidbody si disponible
		var rb = _controller?.Components.Get<Rigidbody>();
		Vector3 velocity = rb != null ? rb.Velocity : Vector3.Zero;

		// Calcule les paramètres d'animation
		var horizontalVelocity = new Vector3( velocity.x, velocity.y, 0 );
		float speed = horizontalVelocity.Length;

		// Paramètres pour l'AnimationGraph Citizen
		BodyRenderer.Set( "b_grounded", true );
		BodyRenderer.Set( "move_groundspeed", speed );
		BodyRenderer.Set( "move_x", velocity.x );
		BodyRenderer.Set( "move_y", velocity.y );
		BodyRenderer.Set( "move_z", velocity.z );
		BodyRenderer.Set( "wish_groundspeed", speed );
		BodyRenderer.Set( "wish_x", velocity.x );
		BodyRenderer.Set( "wish_y", velocity.y );
		BodyRenderer.Set( "wish_z", 0f );

		// Debug toutes les 60 frames pour éviter le spam
		_frameCount++;
		if ( _frameCount % 60 == 0 )
		{
			Log.Info( $"[PlayerAnimator] Speed: {speed:F2} | Velocity: {velocity}" );
		}

		_lastVelocity = velocity;
	}
}
