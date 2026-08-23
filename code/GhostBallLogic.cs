using Sandbox;
using System;

public sealed class GhostBallLogic : Component, Component.ITriggerListener
{
	[Property] ModelRenderer ExplosionModel;
	[Property] TemporaryEffect temporaryEffect;
	[Property] GameObject ExplosionEffect;

	public SoundHandle attackSoundHandle;

	Random random;

	protected override void OnStart()
	{
		random = new Random();

		attackSoundHandle.Parent = GameObject; 
		attackSoundHandle.FollowParent = true;
	}
	public void OnTriggerEnter( GameObject other ) 
	{
		if ( !other.Tags.HasAny( "enemy", "ghost-ball", "nograss", "dead" ) )
		{
			Log.Info( $"Hit {other.Name}" );
			Explode();
		}
	}

	void Explode() 
	{
		SceneTraceResult sceneTrace = Scene.Trace.Sphere( 64, WorldPosition, WorldPosition + GetComponent<Rigidbody>().Velocity )
			.IgnoreGameObjectHierarchy(this.GameObject)
			.WithTag("carbody")
			.Run();

		// DebugOverlay.Trace(sceneTrace);
		// Explosion sound, Particle Effekt
		if ( sceneTrace.Hit ) 
		{
			sceneTrace.GameObject.GetComponentInParent<HealthSystem>().Damage( 500 );
			Log.Info( "Hit Player" );
			// sceneTrace.GameObject.GetComponent<Rigidbody>().ApplyImpulse((WorldPosition - sceneTrace.GameObject.WorldPosition).Normal * 1000);
			// sceneTrace.GameObject.GetComponent<Rigidbody>().AngularVelocity += random.VectorInSphere( 10 );
		}

		attackSoundHandle.Volume = 0;

		GetComponent<TemporaryEffect>().DestroyAfterSeconds = 0.1f;
		GameObject _newExplosion = ExplosionEffect.Clone(WorldTransform);
	}

}
