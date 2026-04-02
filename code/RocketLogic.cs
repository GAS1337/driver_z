using Sandbox;
using System;
using static ZombieBrain;

public sealed class RocketLogic : Component, Component.ITriggerListener
{
	[Property] GameObject BurstParticle;
	[Property] int KnockbackPower;
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
			if (hit.GameObject.Tags.Has( "enemy" ))
			{ 
				hit.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;

				DebugOverlay.Trace( hit );
				Log.Info( hit.GameObject.Name + " - " + hit.GameObject.Tags.Has( "enemy" ) );
				hit.GameObject.GetComponent<ZombieBrain>().KnockBack = Math.Max( 1f, hit.GameObject.GetComponent<ZombieBrain>().KnockBack + 1f );

				Rigidbody hitBody = hit.GameObject.GetComponentInParent<Rigidbody>();
				Vector3 targetDir = hitBody.WorldPosition + Vector3.Up * 150 - GameObject.WorldPosition;
				hitBody.ApplyImpulse( (targetDir.Normal + Vector3.Up) * KnockbackPower );

				hit.GameObject.GetComponent<HealthSystem>().Damage( 75f );
			}
			else if ( hit.GameObject.Tags.Has( "carbody" ) )
			{
				if ( !hit.GameObject.IsValid ) return;

				Rigidbody hitBody = hit.GameObject.GetComponent<Rigidbody>();
				Vector3 targetDir = hitBody.WorldPosition + Vector3.Up * 150 - GameObject.WorldPosition;
				hitBody.ApplyImpulse( (targetDir.Normal + Vector3.Up) * (KnockbackPower / 2) );
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
