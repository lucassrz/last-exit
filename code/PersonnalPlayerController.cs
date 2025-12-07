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
	private Vector2 _lastMousePosition;
	private float _timeSinceMouseMoved = 0f;
	private const float MOUSE_IDLE_THRESHOLD = 0.5f; // Temps en secondes avant de considérer la souris inactive

	protected override void OnAwake()
	{
		_rigidbody = Components.Get<Rigidbody>();
		_lastMousePosition = Mouse.Position;
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

		// Détecte si la souris a bougé
		var currentMousePosition = Mouse.Position;
		if ( Vector2.Distance( _lastMousePosition, currentMousePosition ) > 0.001f )
		{
			_timeSinceMouseMoved = 0f;
			_lastMousePosition = currentMousePosition;
		}
		else
		{
			_timeSinceMouseMoved += Time.Delta;
		}

		// Calcule le multiplicateur de vitesse basé sur la direction
		float speedMultiplier = GetSpeedMultiplier( moveDirection );

		// Détermine la vitesse de base (court par défaut, marche si la touche "walk" est enfoncée)
		var baseSpeed = Input.Down( "walk" ) ? WalkSpeed : RunSpeed;

		// Applique le multiplicateur
		var speed = baseSpeed * speedMultiplier;

		// Applique la force de mouvement au Rigidbody
		var targetVelocity = moveDirection * speed;

		// Garde la vélocité verticale (gravité) et applique le mouvement horizontal
		var currentVelocity = _rigidbody.Velocity;
		_rigidbody.Velocity = new Vector3( targetVelocity.x, targetVelocity.y, currentVelocity.z );

		// Rotation du personnage
		HandleRotation( moveDirection );
	}

	private void HandleRotation( Vector3 moveDirection )
	{
		// Si la souris a bougé récemment, utilise le crosshair pour la rotation
		if ( _timeSinceMouseMoved < MOUSE_IDLE_THRESHOLD )
		{
			var directionToCrosshair = CrosshairManager.GetHorizontalDirectionFrom( WorldPosition );
			if ( directionToCrosshair.Length > 0.1f )
			{
				var targetRotation = Rotation.LookAt( directionToCrosshair, Vector3.Up );
				WorldRotation = Rotation.Slerp( WorldRotation, targetRotation, Time.Delta * RotationSpeed );
			}
		}
		// Sinon, utilise la direction des inputs WASD
		else if ( moveDirection.Length > 0.1f )
		{
			var targetRotation = Rotation.LookAt( moveDirection, Vector3.Up );
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
