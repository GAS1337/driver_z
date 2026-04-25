using Sandbox;
using System;
using static ZombieBrain;

public sealed class RocketLogic : Component, Component.ITriggerListener
{
	[Property] GameObject BurstParticle;
	[Property] int KnockbackPower;
	[Property] float Damage = 250f;
	Rigidbody RocketBody;

	protected override void OnStart()
	{
		RocketBody = GetComponent<Rigidbody>();
	}


	public void OnTriggerEnter( Collider other )
	{
		// Wenn Player disable model und body
		if ( other.GameObject.Tags.Has( "player" ) ) { return; }

		var ExplosionTrace = Scene.Trace.Sphere( 400, GameObject.WorldPosition, GameObject.WorldPosition )
			.IgnoreGameObjectHierarchy( this.GameObject )
			.WithoutTags("ignoreplayer")
			.WithAnyTags("enemy", "carbody")	
			.RunAll();

		foreach ( SceneTraceResult hit in ExplosionTrace )
		{
			if ( hit.GameObject.Tags.Has( "enemy" ) )
			{
				// DebugOverlay.Trace( hit );
				Log.Info( hit.GameObject.Name + " - " + hit.GameObject.Tags.Has( "enemy" ) );

				// Damage
				hit.GameObject.GetComponent<HealthSystem>().Damage( Damage );

				// Zombie Stagger
				if ( hit.GameObject.GetComponent<ZombieBrain>() != null ) 
				{
					hit.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;
					hit.GameObject.GetComponent<ZombieBrain>().KnockBack = Math.Max( 1f, hit.GameObject.GetComponent<ZombieBrain>().KnockBack + 1f );
				}
				else if ( hit.GameObject.GetComponent<VampireBrain>() != null )
				{
					hit.GameObject.GetComponent<VampireBrain>().CurrentState = VampireState.Staggered;
					hit.GameObject.GetComponent<VampireBrain>().UntilKnockBack = Math.Max( 1f, hit.GameObject.GetComponent<VampireBrain>().UntilKnockBack + 1f );

					hit.GameObject.GetComponent<VampireBrain>().TargetPosition += (hit.GameObject.WorldPosition - GameObject.WorldPosition).Normal * 300;
					// Rotation?
				}
				else if ( hit.GameObject.GetComponent<GhostBrain>() != null )
				{
					hit.GameObject.GetComponent<GhostBrain>().CurrentState = GhostState.Staggered;
					hit.GameObject.GetComponent<GhostBrain>().UntilKnockBack = Math.Max( 1f, hit.GameObject.GetComponent<GhostBrain>().UntilKnockBack + 1f );

					hit.GameObject.GetComponent<GhostBrain>().TargetPosition += (hit.GameObject.WorldPosition - GameObject.WorldPosition).Normal * 300;
					// Rotation?
				}
				// Rigidbody Impulse
				if ( hit.GameObject.GetComponent<Rigidbody>() != null )
				{
					Rigidbody hitBody = hit.GameObject.GetComponentInParent<Rigidbody>();
					Vector3 targetDir = hitBody.WorldPosition + Vector3.Up * 400 - GameObject.WorldPosition;
					hitBody.ApplyImpulse( (targetDir.Normal + Vector3.Up) * KnockbackPower * hitBody.Mass );
				}
			}
			else if ( hit.GameObject.Tags.Has( "carbody" ) )
			{
				if ( !hit.GameObject.IsValid ) return;

				Rigidbody hitBody = hit.GameObject.GetComponent<Rigidbody>();
				Vector3 targetDir = hitBody.WorldPosition + Vector3.Up * 150 - GameObject.WorldPosition;
				hitBody.ApplyImpulse( (targetDir.Normal + Vector3.Up) * (KnockbackPower * hitBody.Mass * 0.5f) );
				hit.GameObject.GetComponentInParent<HealthSystem>().Damage( 0f );
			}

		}

		RocketBody.Enabled = false;
		GameObject.GetComponentInChildren<ModelRenderer>().Enabled = false;
		foreach ( CapsuleCollider collider in GameObject.GetComponents<CapsuleCollider>() ) 
		{
			collider.Enabled = false;
		}

		Sound.Play( "sounds/mediumexplosion.sound", GameObject.WorldPosition );
		BurstParticle.Enabled = true;
	}
}
