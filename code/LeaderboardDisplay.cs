using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class LeaderboardDisplay : Component, LeaderboardDisplay.ILeaderboardEvent
{
	[Property] TextRenderer boardText;

	public interface ILeaderboardEvent : ISceneEvent<ILeaderboardEvent>
	{
		void OnGlobalHit();
		void OnFriendsHit();
		void OnCenterMeHit();
	}

	void ILeaderboardEvent.OnGlobalHit()
	{
		Sound.Play( "sounds/medikitsound.sound" );
		DisplayLeaderboard();
		Log.Info( "Displaying global leaderboard..." );
	}
	void ILeaderboardEvent.OnFriendsHit()
	{
		Sound.Play( "sounds/medikitsound.sound" );
		DisplayLeaderboard( onlyFriends: true );
		Log.Info( "Displaying friends leaderboard..." );
	}
	void ILeaderboardEvent.OnCenterMeHit()
	{
		Sound.Play( "sounds/medikitsound.sound" );
		DisplayLeaderboard( centerMe: true );
		Log.Info( "Centering on me..." );
	}

	protected override void OnStart()
	{
		DisplayLeaderboard();
	}

	protected override void OnUpdate()
	{

	}

	public struct ScoreMetadata
	{
		public double time { get; set; }
	}

	public async Task DisplayLeaderboard(bool onlyFriends = false, bool centerMe = false)
	{
		var _scoreBoard = Sandbox.Services.Leaderboards.GetFromStat( "straightgas.graveyard_gunners", "LeaderboardTest" );
		_scoreBoard.SetAggregationMax();
		_scoreBoard.SetSortDescending();
		_scoreBoard.MaxEntries = 15;
		if ( onlyFriends ) { _scoreBoard.SetFriendsOnly(onlyFriends); }
		if ( centerMe ) { _scoreBoard.CenterOnMe(); }

		boardText.Text = "LOADING...";
		await _scoreBoard.Refresh();
		boardText.Text = "";

		foreach ( var entry in _scoreBoard.Entries )
		{
			/*
			DateTimeOffset timeStamp = entry.Timestamp;
			var _timeBoard = Sandbox.Services.Leaderboards.GetFromStat( "straightgas.graveyard_gunners", "TimeTest" );
			_timeBoard.SetDatePeriod( timeStamp.UtcDateTime );
			await _timeBoard.Refresh();
			Log.Info( _timeBoard.Entries[0].ToString() );
			*/
			boardText.Text += $"\n{entry.CountryCode} #{entry.Rank} {entry.DisplayName}: {Math.Round( entry.Value, 0 )}";
			// Log.Info( $"#{entry.Rank} {entry.DisplayName}: {entry.Value}" );
		}
	}


}
