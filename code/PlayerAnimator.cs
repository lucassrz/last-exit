using Sandbox;

namespace LastExit;

/// <summary>
/// Gère les animations du personnage en fonction de sa vitesse
/// </summary>
public sealed class PlayerAnimator : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public float MaxSpeed { get; set; } = 320f;

	private Vector3 _previousPosition;
	private GameObject _rootObject;

	protected override void OnAwake()
	{
		// Trouve le renderer automatiquement s'il n'est pas assigné
		if ( Renderer == null )
		{
			Renderer = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		}

		// Trouve le GameObject racine (celui qui a le PersonnalPlayerController)
		_rootObject = Components.Get<PersonnalPlayerController>()?.GameObject ?? GameObject;
		_previousPosition = _rootObject.WorldPosition;
	}

	protected override void OnUpdate()
	{
		if ( Renderer == null || _rootObject == null )
			return;

		UpdateAnimations();
	}

	private void UpdateAnimations()
	{
		// Calcule la vitesse en comparant la position du GameObject racine
		var velocity = (_rootObject.WorldPosition - _previousPosition) / Time.Delta;
		_previousPosition = _rootObject.WorldPosition;

		// Calcule la vitesse horizontale
		var horizontalVelocity = new Vector3( velocity.x, velocity.y, 0 );
		float speed = horizontalVelocity.Length;

		// Normalise la vitesse pour l'animator (0 = idle, 1 = vitesse max)
		float normalizedSpeed = MaxSpeed > 0 ? speed / MaxSpeed : 0f;

		// Met à jour les paramètres de l'animator
		Renderer.Set( "move_speed", normalizedSpeed );
		Renderer.Set( "b_grounded", true );
	}
}
