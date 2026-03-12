using Sandbox;
using static ZombieBrain;

public sealed class RocketLogic : Component, Component.ITriggerListener
{
	[Property] GameObject BurstParticle;
	[Property] int KnockbackPower;


	public void OnTriggerEnter( Collider other )
	{
		// Wenn Player disable model und body
		if ( other.GameObject.Tags.Has( "player" ) ) { return; }

		var ExplosionTrace = Scene.Trace.Sphere( 400, GameObject.WorldPosition, GameObject.WorldPosition )
			.IgnoreGameObjectHierarchy( this.GameObject )
			.IgnoreGameObject( this.GameObject )
			.WithoutTags("ignoreplayer")
			.WithTag("enemy")	
			.RunAll();

		foreach (SceneTraceResult hit in ExplosionTrace)
		{
			hit.GameObject.GetComponent<ZombieBrain>().CurrentState = ZombieState.Staggered;

			Rigidbody hitBody = hit.GameObject.GetComponentInParent<Rigidbody>();
			Vector3 targetDir = hitBody.WorldPosition - GameObject.WorldPosition;
			hitBody.ApplyImpulse( (targetDir.Normal + Vector3.Up) * KnockbackPower );

			DebugOverlay.Trace( hit );
			Log.Info( hit.GameObject.Name +" - "+ hit.GameObject.Tags.Has( "enemy" ) );
			hit.GameObject.GetComponent<HealthSystem>().Damage(75f);
		}

		GameObject.GetComponent<Rigidbody>().Enabled = false;
		GameObject.GetComponentInChildren<ModelRenderer>().Enabled = false;
		foreach ( CapsuleCollider collider in GameObject.GetComponents<CapsuleCollider>() ) 
		{
			collider.Enabled = false;
		}

		Sound.Play( "sounds/mediumexplosion.sound", GameObject.WorldPosition );
		BurstParticle.Enabled = true;
	}
}
