using Sandbox;
using System;
using System.Threading.Tasks;
using static MonsterSpawner;

public sealed class Blitz : Component, MonsterSpawner.IMonsterSpawnerEvent
{
	[Property] LineRenderer BlitzLine;
	[Property] public MonsterSpawner MonsterSpawner;

	TimeSince SinceLightningStrike;
	TimeSince SinceSpawn;

	Random random = new();

	void IMonsterSpawnerEvent.OnMonsterSpawn()
	{
		LightningStrike();
		SinceLightningStrike = random.Float( 0, 2 );
	}

	protected override void OnStart()
	{
		BlitzLine.Color = MonsterSpawner.LineRenderer.Color;
		LightningStrike();
	}

	protected override void OnFixedUpdate()
	{
		if ( SinceLightningStrike > 8 )
		{
			LightningStrike();
			SinceLightningStrike = random.Float(0, 2);
		}
	}

	async Task LightningStrike()
	{
		Sound.Play( "sounds/blitz/thunderclap.sound", WorldPosition );

		BlitzLine.Enabled = true; await Task.DelayRealtimeSeconds( 0.3f );
		BlitzLine.Enabled = false; await Task.DelayRealtimeSeconds( 0.2f );
		BlitzLine.Enabled = true; await Task.DelayRealtimeSeconds( 0.3f );
		BlitzLine.Enabled = false;await Task.DelayRealtimeSeconds( 0.3f );
		BlitzLine.Enabled = true;await Task.DelayRealtimeSeconds( 0.1f );
		
		BlitzLine.Enabled = false;
	}
}
