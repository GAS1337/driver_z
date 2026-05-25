using Sandbox;
using System;

public sealed class HighscoreManager : Component
{
	[Property] Rigidbody CarBody;
	[Property] RotationControl RotationControl;

	public float CurrentScore;
	float LatestScore;
	public float LastGainedScore;

	public TimeSince SinceGameStart;
	public TimeSpan TimeSpanSinceGameStart;


	protected override void OnEnabled()
	{
		SinceGameStart = 0;
	}

	protected override void OnUpdate()
	{
		// Log.Info( "Current Score: " + CurrentScore + " Last score: " + LatestScore );
		TimeSpanSinceGameStart = TimeSpan.FromSeconds( SinceGameStart );
	}

	public void IncreaseScore(float amount)
	{
		amount *= CalculateMultiplier();
		amount = (float)Math.Round(amount, 0);
		CurrentScore += amount;
		LastGainedScore = amount;
		Log.Info("Score increased by " + amount + ". Current score: " + CurrentScore + " Last Gained Score: " + LastGainedScore);
	}

	public float CalculateMultiplier()
	{
		float multiplier = 1f;

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
		ResetScore();
	}
}
