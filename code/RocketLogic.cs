using Sandbox;

public sealed class RocketLogic : Component, Component.ITriggerListener
{
	[Property] GameObject BurstParticle;

	public void OnTriggerEnter( Collider other )
	{
		// Wenn Player disable model und body
		if ( other.GameObject.Tags.Has( "player" ) ) { return; }

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
