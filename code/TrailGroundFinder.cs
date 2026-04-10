using Sandbox;

public sealed class TrailGroundFinder : Component
{
	protected override void OnUpdate()
	{
		SceneTraceResult groundCheck = Scene.Trace.Ray( GameObject.WorldPosition + Vector3.Up * 50, GameObject.WorldPosition + Vector3.Down * 100 ) // 48 is radius
			.Radius( 10 )
			.WithoutTags("enemy", "player")
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		GameObject.WorldPosition = GameObject.Parent.WorldPosition.WithZ(groundCheck.HitPosition.z);
		GameObject.WorldRotation = GameObject.WorldRotation.Angles().WithRoll( 0 ).WithPitch( 75 );
	}
}
