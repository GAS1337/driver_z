using Sandbox;

public sealed class AchievementTriggerzone : Component, Component.ITriggerListener
{
	[Property, Description( "Lowercase letters, numbers and . _ - / only. \nNo spaces or other special characters." )] string AchievementName { get; set; }
	[Property] bool SendTime { get; set; } = false;
	HighscoreManager highscoreManager;


	protected override void OnStart()
	{
		if ( SendTime ) 
		{ 
			highscoreManager = Scene.Get<HighscoreManager>();
		}
	}

	public void OnTriggerEnter( GameObject other )
	{
		if ( other.Tags.Has( "player" ) )
		{
			if ( AchievementName == null ) { Log.Error( $"{GameObject}: AchievementName is null" ); return; }
			Sandbox.Services.Achievements.Unlock( AchievementName );
			Log.Info( $"{AchievementName} unlocked!" );

			if (SendTime)
			{
				Sandbox.Services.Stats.SetValue(AchievementName+"leaderboard", highscoreManager.TimeSpanSinceGameStart.Seconds);
			}
		}
	}
}
