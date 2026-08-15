using Sandbox;
using static HealthSystem;

public sealed class DestructableProp : Component, HealthSystem.IHealthEvent
{
	[Property] GameObject DestroyedPrefab;

	public void OnDeath()
	{
		GameObject _deadClone = DestroyedPrefab.Clone(WorldPosition, Rotation.Identity, Vector3.One);
		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{

	}
}
