using Sandbox;

public sealed class HighscoreManager : Component
{
	public float CurrentScore;
	float LatestScore;
	float LastGainedScore;

	protected override void OnUpdate()
	{
		// Log.Info( "Currenbt Score: " + CurrentScore + " Last score: " + LatestScore );

	}

	public void IncreaseScore(float amount)
	{
		CurrentScore += amount;
		LastGainedScore = amount;
		Log.Info("Score increased by " + amount + ". Current score: " + CurrentScore + " Last Gained Score: " + LastGainedScore);
	}

	public void ResetScore()
	{
		LatestScore = CurrentScore;
		Log.Info( "Score reset. Last score: " + LatestScore );
		CurrentScore = 0;
		LastGainedScore = 0;
	}

	public void WriteToLeaderboard() 
	{
		// add score to leaderboard
		Sandbox.Services.Stats.SetValue("LeaderboardTest", CurrentScore );
	}
}
