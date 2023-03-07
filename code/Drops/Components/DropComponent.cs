﻿namespace Grubs;

public class DropComponent : EntityComponent<Drop>
{
	protected Drop Drop => Entity;

	public virtual void Simulate( IClient client ) {}

	public virtual void OnTouch( Entity other ) {}
}