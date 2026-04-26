using Sandbox;
using System.Threading.Tasks;
using static MonsterSpawner;

public sealed class Blitz : Component, MonsterSpawner.IMonsterSpawnerEvent
{
	[Property] LineRenderer BlitzLine;
	[Property] MonsterSpawner MonsterSpawner;

	TimeSince SinceFlicker;
	TimeSince SinceSpawn;

	void IMonsterSpawnerEvent.OnMonsterSpawn()
	{
		LightningStrike();
	}

	protected override void OnStart()
	{
		BlitzLine.Enabled = false;
		BlitzLine.Color = MonsterSpawner.LineRenderer.Color;
	}

	protected override void OnFixedUpdate()
	{

	}

	async Task LightningStrike()
	{
		Sound.Play( "sounds/blitz/thunderclap.sound", WorldPosition );

		BlitzLine.Enabled = true;
		await Task.DelayRealtimeSeconds( 0.15f );
		BlitzLine.Enabled = false;
		await Task.DelayRealtimeSeconds( 0.1f ); 
		BlitzLine.Enabled = true;
		await Task.DelayRealtimeSeconds( 0.2f );
		BlitzLine.Enabled = false;
		await Task.DelayRealtimeSeconds( 0.2f );
		BlitzLine.Enabled = true;
		await Task.DelayRealtimeSeconds( 0.1f );
		BlitzLine.Enabled = false;
	}
}
