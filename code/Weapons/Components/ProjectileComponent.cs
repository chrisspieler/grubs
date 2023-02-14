﻿namespace Grubs;

[Prefab]
public class ProjectileComponent : WeaponComponent
{
	[Prefab]
	public Model ProjectileModel { get; set; }

	[Prefab]
	public bool ProjectileShouldBounce { get; set; } = false;

	[Prefab]
	public bool ProjectileShouldUseTrace { get; set; } = false;

	[Prefab]
	public int ProjectileMaxBounces { get; set; } = 0;

	[Prefab]
	public float ProjectileSpeed { get; set; } = 1000.0f;

	[Prefab]
	public float ProjectileExplosionRadius { get; set; } = 100.0f;

	[Prefab]
	public float ProjectileExplodeAfter { get; set; } = 4.0f;

	[Prefab]
	public float ProjectileForceMultiplier { get; set; } = 1.0f;

	public override bool ShouldStart()
	{
		return Grub.IsTurn && Grub.Controller.IsGrounded;
	}

	public override void Simulate( IClient client )
	{
		base.Simulate( client );

		if ( IsFiring )
			Fire();
	}

	public override void FireInstant()
	{
		Log.Info( "Fire Instant" );
	}

	public override void FireCharged()
	{
		Log.Info( "Fire Charged: " + Charge );

		var projectile = new Projectile()
			.WithGrub( Grub )
			.WithModel( ProjectileModel )
			.WithPosition( Weapon.Position.WithY( 0f ) )
			.WithSpeed( ProjectileSpeed )
			.WithExplosionRadius( ProjectileExplosionRadius )
			.SetCollisionReaction( ProjectileCollisionReaction.Explosive );

		if ( ProjectileShouldUseTrace )
		{
			var arcTrace = new ArcTrace( Grub, Grub.EyePosition );
			var segments = ProjectileShouldBounce
				? arcTrace.RunTowardsWithBounces( Grub.EyeRotation.Forward.Normal * Grub.Facing, ProjectileForceMultiplier * Charge, 0, ProjectileMaxBounces )
				: arcTrace.RunTowards( Grub.EyeRotation.Forward.Normal * Grub.Facing, ProjectileForceMultiplier * Charge, 0f );

			projectile.MoveAlongTrace( segments );
		}
		else
		{
			projectile.SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Weapon.Position.WithY( 0f ), 7f );
			var desiredPosition = Weapon.Position.WithY( 0 ) + (Grub.EyeRotation.Forward.Normal * Grub.Facing * 40f);
			var tr = Trace.Ray( Weapon.Position.WithY( 0 ), desiredPosition ).Ignore( Weapon.Owner ).Run();
			projectile.Position = tr.EndPosition;
			projectile.Velocity = (Grub.EyeRotation.Forward.Normal * Grub.Facing * Charge * ProjectileSpeed).WithY( 0f );
		}

		if ( ProjectileExplodeAfter > 0 )
			projectile.ExplodeAfterSeconds( ProjectileExplodeAfter );

		projectile.Finish();
		Grub.SetAnimParameter( "fire", true );

		IsFiring = false;
		Charge = 0;
	}


}