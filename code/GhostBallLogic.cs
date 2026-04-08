using Sandbox;
using System;

public sealed class GhostBallLogic : Component, Component.ITriggerListener
{
	[Property] ModelRenderer ExplosionModel;
	[Property] TemporaryEffect temporaryEffect;

	Random random;

	protected override void OnStart()
	{
		random = new Random();
	}
	public void OnTriggerEnter( GameObject other ) 
	{ 
		if (other.Tags.HasAny("enemy", "wheel" )) return;
		Explode();
	}

	void Explode() 
	{
		SceneTraceResult sceneTrace = Scene.Trace.Sphere( 64, WorldPosition, WorldPosition + GetComponent<Rigidbody>().Velocity )
			.IgnoreGameObjectHierarchy(GameObject)
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
		GetComponent<Rigidbody>().Velocity = Vector3.Zero;
		ExplosionModel.Enabled = true;
		temporaryEffect.Enabled = true;
	}

}
