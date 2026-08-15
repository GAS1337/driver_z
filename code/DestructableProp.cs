using Sandbox;
using static HealthSystem;

public sealed class DestructableProp : Component, HealthSystem.IHealthEvent
{
	[Property] GameObject DestroyedPrefab;

	public void OnDeath()
	{
		GameObject _deadClone = DestroyedPrefab.Clone(WorldPosition, WorldRotation, WorldScale);
		Rigidbody _rb = _deadClone.GetComponent<Rigidbody>();
		_rb.ApplyImpulse( Vector3.Up * 300 * _rb.Mass );

		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{

	}
}
