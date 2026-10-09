using Sandbox;
using System;

public sealed class HighscoreManager : Component
{
	[Property] public LobbyManager LobbyManager { get; set; }
	[Property] public GameObject Screen { get; set; }
	Rigidbody CarBody;
	RotationControl RotationControl;

	[Sync] public float CurrentScore { get; set; } = 0;
	float LatestScore;
	public float LastGainedScore;

	public TimeSince SinceGameStart;
	public TimeSpan TimeSpanSinceGameStart;


	protected override void OnEnabled()
	{
		var hud = Screen?.Components.Get<HUD>() ?? Scene.GetAllComponents<HUD>().FirstOrDefault();
		if ( hud.IsValid() )
		{
			hud.HighscoreManager = this;
		}

		TryFindLocalCar();

		SinceGameStart = 0;
	}

	protected override void OnUpdate()
	{
		if ( RotationControl == null || CarBody == null )
		{
			TryFindLocalCar();
		}

		var hud = Screen?.Components.Get<HUD>() ?? Scene.GetAllComponents<HUD>().FirstOrDefault();
		if ( hud.IsValid() && hud.HighscoreManager != this )
		{
			hud.HighscoreManager = this;
		}

		TimeSpanSinceGameStart = TimeSpan.FromSeconds( SinceGameStart );
	}

	private void TryFindLocalCar()
	{
		if ( !LobbyManager.IsValid() )
		{
			LobbyManager = Scene.GetAllComponents<LobbyManager>().FirstOrDefault();
		}

		if ( LobbyManager.IsValid() && LobbyManager.ConnectedPlayers != null && Connection.Local != null )
		{
			if ( LobbyManager.ConnectedPlayers.TryGetValue( Connection.Local.SteamId, out var localPlayer ) && localPlayer.IsValid() )
			{
				RotationControl = localPlayer.Components.Get<RotationControl>();
				CarBody = RotationControl?.CarBody;
			}
		}
	}

	[Rpc.Broadcast]
	public void IncreaseScore(float amount)
	{
		if ( IsProxy ) return;
		amount *= CalculateMultiplier();
		amount = (float)Math.Round(amount, 0);
		CurrentScore += amount;
		Sandbox.Services.Stats.SetValue( "points_scored", CurrentScore );
		switch (CurrentScore)
		{
			case >= 1000000:
				Sandbox.Services.Achievements.Unlock( "million_points" );
				break;
			case >= 666666:
				Sandbox.Services.Achievements.Unlock( "six_hundred_thousand_points" );
				break;
			case >= 333333:
				Sandbox.Services.Achievements.Unlock( "three_hundred_thousand_points" );
				break;
			case >= 100000:
				Sandbox.Services.Achievements.Unlock( "hundred_thousand_points" );
				break;
			case >= 50000:
				Sandbox.Services.Achievements.Unlock( "fifty_thousand_points" );
				break;
			case >= 10000:
				Sandbox.Services.Achievements.Unlock( "ten_thousand_points" );
				break;
			
			default:
				break;

		}
		LastGainedScore = amount;
		Log.Info("Score increased by " + amount + ". Current score: " + CurrentScore + " Last Gained Score: " + LastGainedScore);
	}

	public float CalculateMultiplier()
	{
		float multiplier = 1f;

		if ( !CarBody.IsValid() || !RotationControl.IsValid() )
			return multiplier;

		multiplier += CarBody.Velocity.Length.Remap( 1000, 3500, 0, 0.5f );
		if (!RotationControl.IsGrounded) { multiplier += 0.5f; }
		multiplier = (float)Math.Round(multiplier, 2);

		return multiplier;
	}

	[Button]
	public void ResetScore()
	{
		LatestScore = CurrentScore;
		Log.Info( "Score reset. Last score: " + LatestScore );
		CurrentScore = 0;
		LastGainedScore = 0;
	}


	[Button]
	public void WriteToLeaderboard() 
	{
		// add score to leaderboard
		Sandbox.Services.Stats.SetValue("LeaderboardTest", CurrentScore );
		Sandbox.Services.Achievements.Unlock( "first_leaderboard_entry" );
		Sandbox.Services.Stats.SetValue( "TimeTest", SinceGameStart );
		LatestScore = CurrentScore;
	}
}
