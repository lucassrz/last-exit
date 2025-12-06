using Sandbox;

public sealed class GameCamera : Component
{
	[Property] public GameObject Target { get; set; }
    [Property] public Vector3 Offset { get; set; } = new Vector3(-250, 0, 300);
    [Property] public float SmoothSpeed { get; set; } = 15f;
    [Property] public Angles FixedRotation { get; set; } = new Angles(45, 0, 0);

    private Rotation _fixedRot;

    protected override void OnAwake()
    {
        _fixedRot = Rotation.From(FixedRotation);
    }

    protected override void OnUpdate()
    {
        if (Target != null)
        {
            Vector3 desiredPosition = Target.WorldPosition + Offset;
            WorldPosition = Vector3.Lerp(WorldPosition, desiredPosition, Time.Delta * SmoothSpeed);
        }

        WorldRotation = _fixedRot;
    }

    protected override void OnFixedUpdate()
    {
        WorldRotation = _fixedRot;
    }

    protected override void OnPreRender()
    {
        WorldRotation = _fixedRot;
    }
}
