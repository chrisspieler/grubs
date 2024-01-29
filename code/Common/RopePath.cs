namespace Grubs.Common;

public sealed class RopePath : Component
{
	[Property] public float MinimumFireDistance { get; set; } = 10f;
	[Property] public LegacyParticleSystem? RopeSystem { get; set; }
	[Property] public ParticleSystem RopeAsset { get; set; } = ParticleSystem.Load( "particles/entity/rope.vpcf" );
	private readonly List<RopeSegment> _ropeSegments = new();

	public bool IsEmpty => !_ropeSegments.Any();
	public IEnumerable<RopeSegment> Segments => _ropeSegments;

	protected override void OnUpdate()
	{
		if ( RopeTester.RopeDebug )
		{
			Gizmo.Draw.Color = Color.Yellow;
			foreach( var segment in _ropeSegments )
			{
				var position = segment.GameObject.Transform.Position + Vector3.Up * 3f;
				Gizmo.Draw.Text( $"index: {_ropeSegments.IndexOf( segment )} angle: {GetAngleToNextPoint( segment )}", new Transform( position ), "Consolas" );
			}
		}

		UpdatePhysics();
	}

	private void UpdatePhysics()
	{
		MergeOnUnwind();
		SplitOnCorner();
		for( int i = 0; i < _ropeSegments.Count; i++ )
		{
			_ropeSegments[i].Tension = Math.Min( i + 1, 5 );
		}
	}

	private void SplitOnCorner()
	{
		if ( !_ropeSegments.Any() )
		{
			return;
		}
		var firstSegment = _ropeSegments.First();
		var tr = Scene.Trace
			.Ray( firstSegment.Transform.Position, firstSegment.NextPoint.Transform.Position )
			.WithAllTags( "solid" )
			.Run();
		var hitDistanceFromNext = tr.EndPosition.Distance( firstSegment.NextPoint.Transform.Position );
		if ( tr.Hit && hitDistanceFromNext > 2f )
		{
			var offset = -tr.Direction * 0.5f;
			SplitSegment( firstSegment, tr.HitPosition + offset );
		}
	}

	private void MergeOnUnwind()
	{
		if ( _ropeSegments.Count < 2 )
		{
			return;
		}
		var firstSegment = _ropeSegments.First();
		var nextSegment = _ropeSegments[1];
		var hitNextPoint = Scene.Trace
			.Ray( firstSegment.Transform.Position, nextSegment.NextPoint.Transform.Position )
			.WithAllTags( "solid" )
			.Run()
			.Hit;
		var startOffset = firstSegment.GetDirectionToNextPoint() * (firstSegment.GetDistanceToNextPoint() - 5f);
		var startPos = firstSegment.Transform.Position + startOffset;
		var endOffset = nextSegment.GetDirectionToNextPoint() * 5f;
		var endPos = nextSegment.Transform.Position + endOffset;
		var direction = (endPos - startPos).Normal;
		var ray = new Ray( startPos, direction );
		startPos = ray.Project( -2f );
		ray = new Ray( startPos, direction );
		var cornerTr = Scene.Trace
			.Ray( ray, 7f )
			.WithAllTags( "solid" )
			.Run();
		if ( RopeTester.RopeDebug )
		{
			Gizmo.Draw.Color = cornerTr.Hit
				? Color.Red : Color.Blue;
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.Line( cornerTr.StartPosition, cornerTr.EndPosition );
		}

		if ( !hitNextPoint && !cornerTr.Hit )
		{
			MergeWithPrevious( nextSegment );
		}
	}

	public void Clear()
	{
		var segments = _ropeSegments.ToArray();
		foreach( var segment in segments )
		{
			RemoveSegment( segment );
		}
		// Clean up the last rope point, which has no RopeSegment component.
		var lastPoint = segments.LastOrDefault()?.NextPoint;
		if ( lastPoint.IsValid() && lastPoint.Tags.Has( "rope" ) )
		{
			lastPoint.Destroy();
		}
	}

	public RopeSegment? GetNearestSegment( Vector3 position )
	{
		if ( !Segments.Any() )
		{
			return null;
		}
		return Segments
			.Select( s => (Segment: s, Distance: s.GetMidpoint().Distance( position ) ) )
			.OrderBy( s => s.Distance )
			.First()
			.Segment;
	}

	private float GetAngleToNextPoint( RopeSegment segment )
	{
		var index = _ropeSegments.IndexOf( segment );
		if ( index == 0 ) return 180;
		var previousSegment = _ropeSegments[index - 1];
		var currentDirection = segment.GetDirectionToNextPoint();
		var previousDirection = previousSegment.GetDirectionToNextPoint();
		return previousDirection.Angle( currentDirection );
	}

	private void RemoveSegment( RopeSegment segment )
	{
		// If a GameObject was created just to be rope, clean it up with the rope.
		if ( segment.Tags.Has( "rope" ) )
		{
			segment.GameObject.Destroy();
		}
		else
		{
			segment.Destroy();
		}
		segment.RopeSystem?.Destroy();
		_ropeSegments.Remove( segment );
	}

	private GameObject CreateRopePointObject( Vector3 position )
	{
		var pointNum = _ropeSegments.Count;
		var go = new GameObject( true, $"Rope Point {pointNum}" );
		go.Tags.Add( "rope" );
		go.Transform.Position = position;
		return go;
	}

	private RopeSegment CreateRopeSegment( GameObject currentPoint, GameObject nextPoint )
	{
		var segment = currentPoint.Components.Create<RopeSegment>();
		segment.RopeAsset = RopeAsset;
		segment.NextPoint = nextPoint;
		return segment;
	}

	public void ExtendRope( Vector3 nextPosition )
	{
		if ( !_ropeSegments.Any() )
		{
			MakeRopeLine( GameObject, nextPosition );
			return;
		}
		var lastSegment = _ropeSegments.Last();
		lastSegment.NextPoint ??= CreateRopePointObject( nextPosition );
		var nextPoint = CreateRopePointObject( nextPosition );
		var segment = CreateRopeSegment( lastSegment.NextPoint, nextPoint );
		_ropeSegments.Add( segment );
	}

	public void Fire( Ray direction, float distance )
	{
		var tr = Scene.Trace
			.Ray( direction, distance )
			.Run();
		if ( !tr.Hit ) return;
		if ( tr.Distance < MinimumFireDistance ) return;

		MakeRopeLine( GameObject, tr.HitPosition );
	}

	public void MakeRopeLine( GameObject from, Vector3 to )
	{
		Clear();
		var ropePoint = CreateRopePointObject( to );
		var segment = CreateRopeSegment( GameObject, ropePoint );
		_ropeSegments.Add( segment );
	}

	public void SplitSegment( RopeSegment segment, Vector3 splitPoint )
	{
		var oldEnd = segment.NextPoint;
		if ( !oldEnd.IsValid() )
		{
			throw new Exception( "Cannot split a segment with no end point." );
		}
		var startIndex = _ropeSegments.IndexOf( segment );
		var newPoint = CreateRopePointObject( splitPoint );
		segment.NextPoint = newPoint;
		segment.RopeLength = segment.GetDistanceToNextPoint();
		var newSegment = CreateRopeSegment( newPoint, oldEnd );
		_ropeSegments.Insert( startIndex + 1, newSegment );
	}

	public void MergeWithPrevious( RopeSegment segment )
	{
		var index = _ropeSegments.IndexOf( segment );
		if ( index == 0 ) return;
		var previousSegment = _ropeSegments[index - 1];
		RemoveSegment( segment );
		previousSegment.NextPoint = segment.NextPoint;
	}
}
