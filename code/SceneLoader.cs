using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class SceneLoader : Component, Component.ITriggerListener
{
	[Property] float Timer = 3f;
	[Property] TextRenderer CountdownText;

	[Property] public SceneFile MainScene;
	[Property] public SceneFile LobbyScene;

	public SceneLoadOptions SceneLoadOptions;

	int triggerId = 0;
	bool UpdateCountdown = false;

	TimeUntil TimeUntilStarting;


	protected override void OnStart()
	{
		SceneLoadOptions = new SceneLoadOptions();
		SceneLoadOptions.SetScene(MainScene);

		CountdownText.Text = "Enter to Start!";
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( !other.Tags.Has( "carbody" ) ) { return; }

		triggerId++;
		Log.Info( other.Name + triggerId );

		// Start Countdown
		StartCountdown( triggerId, Timer );
		// Update Text
		UpdateCountdown = true;
		TimeUntilStarting = Timer;
	}
	public void OnTriggerExit( GameObject other )
	{
		if ( !other.Tags.Has( "carbody" ) ) { return; }

		triggerId++;
		Log.Info( other.Name + "left" + triggerId );
		//Reset Text
		CountdownText.Text = "Enter to Start!";
		UpdateCountdown = false;
	}

	protected override void OnUpdate()
	{
		if ( !UpdateCountdown ) { return; }
		SetCountdownText();
	}

	public async Task StartCountdown(int id, float cd) 
	{
		await Task.DelayRealtimeSeconds( cd );
		if ( triggerId != id ) 
		{
			return; 
		}
		Game.ChangeScene( SceneLoadOptions );
	}

	void SetCountdownText() 
	{
		CountdownText.Text = $"Starting in {Math.Round( TimeUntilStarting.Relative.Clamp(0, Timer), 0)}";
	}

}
