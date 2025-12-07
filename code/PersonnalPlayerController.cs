using Sandbox;

namespace LastExit;

/// <summary>
/// Contrôleur de personnage simple pour mouvement WASD avec physique
/// </summary>
public sealed class PersonnalPlayerController : Component
{
	[Property] public float WalkSpeed { get; set; } = 110f;
	[Property] public float RunSpeed { get; set; } = 320f;
	[Property] public float RotationSpeed { get; set; } = 10f;

	private Rigidbody _rigidbody;

	protected override void OnAwake()
	{
		_rigidbody = Components.Get<Rigidbody>();
	}

	protected override void OnUpdate()
	{
		HandleMovement();
	}

	private void HandleMovement()
	{
		if ( _rigidbody == null )
			return;

		// Récupère les inputs WASD
		var input = Input.AnalogMove;

		// Calcule la direction de mouvement (horizontal seulement)
		var moveDirection = new Vector3( input.x, input.y, 0 ).Normal;

		// Calcule le multiplicateur de vitesse basé sur la direction
		float speedMultiplier = GetSpeedMultiplier( moveDirection );

		// Détermine la vitesse de base
		var baseSpeed = Input.Down( "run" ) ? RunSpeed : WalkSpeed;

		// Applique le multiplicateur
		var speed = baseSpeed * speedMultiplier;

		// Applique la force de mouvement au Rigidbody
		var targetVelocity = moveDirection * speed;

		// Garde la vélocité verticale (gravité) et applique le mouvement horizontal
		var currentVelocity = _rigidbody.Velocity;
		_rigidbody.Velocity = new Vector3( targetVelocity.x, targetVelocity.y, currentVelocity.z );

		// Rotation du personnage vers la position du crosshair
		var directionToCrosshair = CrosshairManager.GetHorizontalDirectionFrom( WorldPosition );
		if ( directionToCrosshair.Length > 0.1f )
		{
			var targetRotation = Rotation.LookAt( directionToCrosshair, Vector3.Up );
			WorldRotation = Rotation.Slerp( WorldRotation, targetRotation, Time.Delta * RotationSpeed );
		}
	}

	private float GetSpeedMultiplier( Vector3 moveDirection )
	{
		// Si le joueur ne bouge pas, retourne 1
		if ( moveDirection.Length < 0.1f )
			return 1f;

		// Récupère la direction vers laquelle regarde le personnage (en 2D)
		var forwardDirection = WorldRotation.Forward;
		forwardDirection.z = 0;
		forwardDirection = forwardDirection.Normal;

		// Calcule l'angle entre la direction de mouvement et la direction du regard
		var dot = Vector3.Dot( moveDirection, forwardDirection );

		// Si on va en arrière (dot < 0), réduit la vitesse de moitié
		if ( dot < 0 )
			return 0.5f;

		// Sinon vitesse normale
		return 1f;
	}
}
