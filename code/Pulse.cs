using Sandbox;

public sealed class Pulse : Component
{
	[Doo.ArgumentHint<SpriteRenderer>( "RedCircle" )]
	[Property] public Doo OnEnabledDoo { get; set; }
	
	[Property] public SpriteRenderer PulseRenderer;

	protected override void OnUpdate()
	{

	}
}
