namespace Grubs.Common;

public sealed class RopeSegment : Component
{
	[Property] public LegacyParticleSystem? RopeSystem { get; set; }
	[Property] public ParticleSystem RopeAsset { get; set; } = ParticleSystem.Load( "particles/entity/rope.vpcf" );
	[Property] public GameObject? NextPoint 
	{
		get => _nextPoint;
		set
		{
			_nextPoint = value;
			InitializeParticleSystem();
		}
	}
	private GameObject? _nextPoint;
	[Property, Range( -10f, 10f )] public float DesiredSlackness 
	{ 
		get => _desiredSlackness;
		set
		{
			_desiredSlackness = value;
		}
	}
	private float _desiredSlackness = -3f;
	[Property] public bool TightenOnStart { get; set; } = true;
	[Property] public float TightenDuration { get; set; } = 0.7f;
	[Property] public float RopeLength { get; set; }
	[Property] public float Tension { get; set; } = 1f;
	private TimeUntil _finishedTightening;

	protected override void OnStart()
	{
		_finishedTightening = TightenDuration;
		RopeLength = GetDistanceToNextPoint();
		InitializeParticleSystem();
	}

	private void InitializeParticleSystem()
	{
		RopeSystem ??= Components.GetOrCreate<LegacyParticleSystem>();
		RopeSystem.Particles = RopeAsset;
		RopeSystem.ControlPoints = new()
		{
			new ParticleControlPoint()
			{
				GameObjectValue = GameObject,
				StringCP = "0"
			},
			new ParticleControlPoint()
			{
				GameObjectValue = NextPoint,
				StringCP = "1"
			},
			new ParticleControlPoint()
			{
				Value = ParticleControlPoint.ControlPointValueInput.Float,
				FloatValue = DesiredSlackness,
				StringCP = "2"
			}
		};
	}

	protected override void OnUpdate()
	{
		AdjustSlack( GetEffectiveSlackness() );
		if ( RopeTester.RopeDebug )
		{
			DebugDraw();
		}
	}

	public Vector3 GetMidpoint()
	{
		return GetPoint( 0.5f );
	}

	public Vector3 GetPoint( float fraction )
	{
		return Vector3.Lerp( GameObject.Transform.Position, NextPoint.Transform.Position, fraction );
	}

	public Vector3 GetDirectionToNextPoint()
	{
		return (NextPoint.Transform.Position - GameObject.Transform.Position).Normal;
	}

	public float GetDistanceToNextPoint()
	{
		return GameObject.Transform.Position.Distance( NextPoint.Transform.Position );
	}

	public float GetVerticality()
	{
		return MathF.Abs( GetDirectionToNextPoint().Dot( Vector3.Up ) );
	}

	private float GetEffectiveSlackness()
	{
		// Make the rope gradually tighten on start.
		var untightenedAmount = TightenOnStart
			? 1 - _finishedTightening.Fraction
			: 0f;
		// Make rope slacken as the distance is closed from source to target.
		var slackness = GetDistanceToNextPoint() / RopeLength;
		slackness *= Tension;
		if ( RopeTester.RopeDebug )
		{
			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.Text( $"s:{slackness}, u:{untightenedAmount}", new Transform( Transform.Position ), "Consolas" );
		}
		slackness += untightenedAmount;
		return (slackness - 0.5f) * DesiredSlackness * 2f;
	}

	private void AdjustSlack( float amount )
	{
		if ( !RopeSystem.IsValid() ) return;
		if ( RopeSystem.ControlPoints.Count < 3 ) return;

		RopeSystem.ControlPoints[2] = new ParticleControlPoint()
		{
			Value = ParticleControlPoint.ControlPointValueInput.Float,
			FloatValue = amount,
			StringCP = "2"
		};
	}

	private void DebugDraw()
	{
		Gizmo.Draw.Color = Color.Yellow;
		var text = $"v:{GetVerticality()} s:{GetEffectiveSlackness()} d:{GetDistanceToNextPoint()}";
		Gizmo.Draw.Text( text, new Transform( GetMidpoint() ), "Consolas" );
	}
}
