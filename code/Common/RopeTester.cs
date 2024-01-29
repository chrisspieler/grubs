namespace Grubs.Common;

public sealed class RopeTester : Component
{
	[Property] public RopePath? Rope { get; set; }
	[Property] public GameObject? Gun { get; set; }
	[Property] public Model HookModel { get; set; } = Model.Load( "models/tools/ninjarope/ninjarope_hook.vmdl" );
	[Property] public float MoveSpeed { get; set; } = 100f;

	private GameObject? _hookInstance;

	[ConVar( "rope_debug" )]
	public static bool RopeDebug { get; set; } = false;

	protected override void OnUpdate()
	{
		Rope ??= Components.GetOrCreate<RopePath>();
		Gun ??= GameObject;

		var mousePos = GetMousePosition();
		UpdateGunRotation( mousePos );
		UpdateGizmos( mousePos );
		UpdateGunInput( mousePos );
		UpdateHook();
		UpdateMovement();
	}

	private Vector3 GetMousePosition()
	{
		var camera = Scene.Camera;
		var screenRay = camera.ScreenPixelToRay( Mouse.Position );
		var mousePos = screenRay.Project( camera.Transform.Position.Distance( Vector3.Zero ) );
		mousePos.x = 0f;
		return mousePos;
	}

	private void UpdateGunRotation( Vector3 mousePos )
	{
		var mouseDir = (mousePos - Transform.Position).Normal;
		var nextPoint = Rope.Segments.FirstOrDefault()?.NextPoint;
		if ( nextPoint is not null )
		{
			mouseDir = (nextPoint.Transform.Position - Transform.Position).Normal;
		}
		Gun.Transform.Rotation = Gun.Transform.Rotation
			.Angles()
			.WithPitch( mouseDir.EulerAngles.pitch );
	}

	private void UpdateGizmos( Vector3 mousePos )
	{
		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.LineSphere( new Sphere( mousePos, 1.5f ) );
		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.ScreenText( "LMB: Fire Rope, RMB: Clear Rope, MMB: Split Rope", new Vector2( Screen.Width / 2, Screen.Height - 25 ), "Consolas", 12, TextFlag.Center );
	}

	private void UpdateGunInput( Vector3 mousePos )
	{
		if ( Input.Pressed( "fire" ) )
		{
			Rope.Clear();
			Rope.ExtendRope( mousePos );
		}
		if ( Input.Pressed( "camera_pan" ) )
		{
			Rope.Clear();
		}
		if ( Input.Pressed( "camera_reset" ) )
		{
			var nearestSegment = Rope.GetNearestSegment( mousePos );
			if ( nearestSegment.IsValid() )
			{
				Rope.SplitSegment( nearestSegment, mousePos );
			}
		}
	}

	private void UpdateHook()
	{
		if ( Rope.IsEmpty )
		{
			_hookInstance?.Destroy();
		}
		else
		{
			if ( !_hookInstance.IsValid() )
			{
				_hookInstance = new GameObject( true, "Rope Hook" );
				_hookInstance.Components.Create<ModelRenderer>().Model = HookModel;
			}
			var lastSegment = Rope.Segments.LastOrDefault();
			if ( lastSegment?.NextPoint is null ) return;
			var dir = (lastSegment.NextPoint.Transform.Position - lastSegment.GameObject.Transform.Position).Normal;
			_hookInstance.Transform.Position = lastSegment.NextPoint.Transform.Position;
			_hookInstance.Transform.Rotation = Rotation.LookAt( dir );
		}
	}

	private void UpdateMovement()
	{
		var input = Input.AnalogMove;
		Vector3 movement = Vector3.Zero;
		movement.z = input.x;
		movement.y = input.y;
		movement *= MoveSpeed * Time.Delta;
		Gun.Transform.Position += movement;
	}
}
