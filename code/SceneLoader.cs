using Sandbox;
using System.Threading.Tasks;

public sealed class SceneLoader : Component, Component.ITriggerListener
{
	[Property] float Timer = 3f;

	[Property] public SceneFile MainScene;
	[Property] public SceneFile LobbyScene;

	public SceneLoadOptions SceneLoadOptions;

	int triggerId = 0;


	protected override void OnStart()
	{
		SceneLoadOptions = new SceneLoadOptions();
		SceneLoadOptions.SetScene(MainScene);
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( "carbody" ) ) { return; }
		triggerId++;
		StartCountdown( triggerId, 3f );
		Log.Info(other.Name + triggerId );
	}
	public void OnTriggerExit( GameObject other )
	{
		if ( !other.Tags.Has( "carbody" ) ) { return; }
		triggerId++;
		Log.Info( other.Name + "left" + triggerId );
	}
	public async Task StartCountdown(int id, float cd) 
	{ 
		await Task.DelayRealtimeSeconds( cd );
		if ( triggerId != id ) { return; }
		Game.ChangeScene( SceneLoadOptions );
	}

}
