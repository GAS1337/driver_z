using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class LeaderboardDisplay : Component
{
	[Property] TextRenderer boardText;

	protected override void OnStart()
	{
		DisplayLeaderboard();
	}

	protected override void OnUpdate()
	{

	}

	public async Task DisplayLeaderboard()
	{
		var board = Sandbox.Services.Leaderboards.GetFromStat( "straightgas.graveyard_gunners", "LeaderboardTest" );
		board.SetAggregationMax();
		board.SetSortDescending();
		board.MaxEntries = 15;

		await board.Refresh();
		boardText.Text = "👑 Leaderboard 👑";

		foreach ( var entry in board.Entries )
		{
			boardText.Text += $"\n#{entry.Rank} {entry.DisplayName}: {Math.Round( entry.Value, 3 )}";
			// Log.Info( $"#{entry.Rank} {entry.DisplayName}: {entry.Value}" );
		}
	}
}
