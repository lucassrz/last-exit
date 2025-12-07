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

	protected override void OnAwake()
	{
		// Trouve le controller
		_controller = Components.Get<PersonnalPlayerController>( FindMode.EnabledInSelfAndDescendants );

		// Trouve le renderer automatiquement s'il n'est pas assigné
		BodyRenderer = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
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
		Vector3 worldVelocity = rb != null ? rb.Velocity : Vector3.Zero;

		// Transforme la vélocité mondiale en vélocité locale (relative à la rotation du body)
		var localVelocity = BodyRenderer.WorldRotation.Inverse * worldVelocity;

		// Calcule les paramètres d'animation
		var horizontalVelocity = new Vector3( worldVelocity.x, worldVelocity.y, 0 );
		float speed = horizontalVelocity.Length;

		// Paramètres pour l'AnimationGraph Citizen - utilise la vélocité locale
		BodyRenderer.Set( "b_grounded", true );
		BodyRenderer.Set( "move_groundspeed", speed );
		BodyRenderer.Set( "move_x", localVelocity.x );
		BodyRenderer.Set( "move_y", localVelocity.y );
		BodyRenderer.Set( "move_z", localVelocity.z );
		BodyRenderer.Set( "wish_groundspeed", speed );
		BodyRenderer.Set( "wish_x", localVelocity.x );
		BodyRenderer.Set( "wish_y", localVelocity.y );
		BodyRenderer.Set( "wish_z", 0f );

		_lastVelocity = worldVelocity;
	}
}
