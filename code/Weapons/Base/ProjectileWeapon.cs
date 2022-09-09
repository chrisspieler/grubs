﻿using System.Threading.Tasks;
using Grubs.Player;
using Grubs.Utils;

namespace Grubs.Weapons.Base;

/// <summary>
/// A weapon capable of firing projectiles.
/// </summary>
public class ProjectileWeapon : GrubWeapon
{
	/// <summary>
	/// The base force at which the projectile will be propelled in the arc trace.
	/// </summary>
	protected virtual float ProjectileForceMultiplier => AssetDefinition.ProjectileForceMultiplier;

	/// <summary>
	/// Whether or not this projectile should bounce.
	/// </summary>
	protected virtual bool ProjectileShouldBounce => AssetDefinition.ProjectileShouldBounce;

	/// <summary>
	/// The max amount of bounces this projectile can do.
	/// <remarks>This will be unused if <see cref="ProjectileShouldBounce"/> is false.</remarks>
	/// </summary>
	protected virtual int ProjectileMaxBounces => AssetDefinition.ProjectileMaxBounces;

	/// <summary>
	/// The model of the projectile.
	/// </summary>
	protected virtual string ProjectileModel => AssetDefinition.ProjectileModel;

	/// <summary>
	/// The speed of the projectile while it is following the arc trace.
	/// </summary>
	protected virtual float ProjectileSpeed => AssetDefinition.ProjectileSpeed;

	/// <summary>
	/// The radius of the explosion created by the projectile.
	/// </summary>
	protected virtual float ProjectileExplosionRadius => AssetDefinition.ProjectileExplosionRadius;

	/// <summary>
	/// The amount of seconds before the projectile will automatically explode.
	/// <remarks>This will only work if the value is greater than 0.</remarks>
	/// </summary>
	protected virtual float ProjectileExplodeAfter => AssetDefinition.ProjectileExplodeAfter;

	/// <summary>
	/// The amount of seconds before the projectile will automatically explode after colliding.
	/// <remarks>This will only work if the value is greater than 0.</remarks>
	/// </summary>
	protected virtual float ProjectileCollisionExplosionDelay => AssetDefinition.ProjectileCollisionExplosionDelay;

	/// <summary>
	/// Whether or not this projectile should use a trace, or a physics impulse for its movement.
	/// </summary>
	protected virtual bool ProjectileShouldUseTrace => AssetDefinition.ProjectileShouldUseTrace;

	/// <summary>
	/// The idle loop sound for the projectile.
	/// </summary>
	protected virtual string ProjectileLoopSound => AssetDefinition.ProjectileLoopSound;

	/// <summary>
	/// The explosion sound for the projectile.
	/// </summary>
	protected virtual string ProjectileExplosionSound => AssetDefinition.ProjectileExplodeSound;

	protected new ProjectileWeaponAsset AssetDefinition => (base.AssetDefinition as ProjectileWeaponAsset)!;

	public ProjectileWeapon()
	{
	}

	public ProjectileWeapon( ProjectileWeaponAsset assetDefinition ) : base( assetDefinition )
	{
	}

	protected override async Task OnFire()
	{
		await base.OnFire();

		var arcTrace = new ArcTrace( Parent, Parent.EyePosition );
		var segments = ProjectileShouldBounce
			? arcTrace.RunTowardsWithBounces( Parent.EyeRotation.Forward.Normal, ProjectileForceMultiplier * Charge, 0, ProjectileMaxBounces )
			: arcTrace.RunTowards( Parent.EyeRotation.Forward.Normal, ProjectileForceMultiplier * Charge, 0 );

		var projectile = new Projectile()
			.WithGrub( (Parent as Grub)! )
			.WithModel( ProjectileModel )
			.WithPosition( Position )
			.WithSpeed( ProjectileSpeed )
			.WithExplosionRadius( ProjectileExplosionRadius );

		if ( ProjectileShouldUseTrace )
			projectile.MoveAlongTrace( segments );
		else
			projectile.UsePhysicsImpulse( Vector3.Up * 10f );

		if ( ProjectileCollisionExplosionDelay > 0 )
			projectile.WithCollisionExplosionDelay( ProjectileCollisionExplosionDelay );

		if ( ProjectileExplodeAfter > 0 )
			projectile.ExplodeAfterSeconds( 5f );

		projectile.Finish();

		SetupProjectile( projectile );
		GrubsCamera.SetTarget( projectile );
	}

	/// <summary>
	/// Called to handle any extra logic needed for the projectile.
	/// </summary>
	/// <param name="projectile">The projectile to edit.</param>
	protected virtual void SetupProjectile( Projectile projectile )
	{
		projectile.ExplosionSound = ProjectileExplosionSound;
		projectile.PlaySound( ProjectileLoopSound );
	}
}
