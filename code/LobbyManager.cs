using Microsoft.AspNetCore.Components;
using Sandbox.Diagnostics;
using Sandbox.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Sandbox.Internal.GlobalGameNamespace;
namespace Sandbox;

/// <summary>
/// Stolen NetworkHelper from Facepunch
/// </summary>
[Title( "LobbyManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class LobbyManager : Component, Component.INetworkListener
{
	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<GameObject> SpawnPoints { get; set; }
	[Property] public GameObject Screen { get; set; }

	[Sync] public NetDictionary<SteamId, GameObject> ConnectedPlayers { get; set; } = new();

	Network.LobbyConfig lobbyConfig = new();

	protected override void OnStart()
	{
		lobbyConfig.DestroyWhenHostLeaves = false;
		lobbyConfig.Privacy = LobbyPrivacy.Public;
		lobbyConfig.Hidden = true;
		lobbyConfig.MaxPlayers = 8;
	}

	protected override void OnUpdate()
	{
		UpdateLocalPlayerHUD();
	}

	private void UpdateLocalPlayerHUD()
	{
		var hud = Screen?.Components.Get<HUD>() ?? Scene.GetAllComponents<HUD>().FirstOrDefault();
		if ( hud.IsValid() && (!hud.PlayerHealth.IsValid() || !hud.PlayerGun.IsValid()) )
		{
			if ( Connection.Local != null && ConnectedPlayers != null && ConnectedPlayers.TryGetValue( Connection.Local.SteamId, out var localPlayer ) && localPlayer.IsValid() )
			{
				hud.PlayerHealth = localPlayer.Components.Get<HealthSystem>();
				hud.PlayerGun = localPlayer.Components.Get<GunControl>();
			}
		}
	}

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby( lobbyConfig );
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		if ( !PlayerPrefab.IsValid() )
			return;

		//
		// Find a spawn location for this player
		//
		var startLocation = FindSpawnLocation().WithScale( 1 );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.Name}" );
		player.NetworkSpawn( channel );
		ConnectedPlayers.Add( channel.SteamId, player );
		Log.Info( ConnectedPlayers[channel.SteamId].ToString() + " added to ConnectedPlayers" );

		if ( channel == Connection.Local )
		{
			AssignPlayerToHUD( player );
		}
	}

	public void AssignPlayerToHUD( GameObject player )
	{
		if ( !player.IsValid() ) return;

		var hud = Screen?.Components.Get<HUD>() ?? Scene.GetAllComponents<HUD>().FirstOrDefault();
		if ( hud.IsValid() )
		{
			hud.PlayerHealth = player.Components.Get<HealthSystem>();
			hud.PlayerGun = player.Components.Get<GunControl>();
		}
	}

	public void OnDisconnected( Connection channel )
	{
		CleanConnectedPlayers( channel );
		Log.Info( $"Client {channel.Name} disconnected from the server" );
	}

	[Rpc.Host]
	private void CleanConnectedPlayers( Connection channel )
	{
		if ( channel != null && ConnectedPlayers.Remove( channel.SteamId ) )
		{
			Log.Info( $"Player {channel.Name} ({channel.SteamId}) removed from ConnectedPlayers" );
		}
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	Transform FindSpawnLocation()
	{
		//
		// If they have spawn point set then use those
		//
		if ( SpawnPoints is not null && SpawnPoints.Count > 0 )
		{
			return Random.Shared.FromList( SpawnPoints, default ).WorldTransform;
		}

		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).WorldTransform;
		}

		//
		// Failing that, spawn where we are
		//
		return WorldTransform;
	}
}
