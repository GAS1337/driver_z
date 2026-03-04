using Sandbox;

public sealed class GunControl : Component
{
	[Property] SkinnedModelRenderer TurretRenderer;
	[Property] CameraComponent MainCamera;
	[Property] BeamEffect ShootEffect;

	protected override void OnUpdate()
	{
		// Wo man hinaimed
		Ray CameraRay = Scene.Camera.ScreenPixelToRay( new Vector2( Screen.Width * 0.5f, Screen.Height * 0.5f ) );
		SceneTraceResult ShootTrace = Scene.Trace.Ray( CameraRay, 10000f )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
		DebugOverlay.Trace( ShootTrace );

		// Turret Yaw mit Camera Yaw mitdrehen
		TurretRenderer.GetBoneObject( 1 ).WorldRotation = Rotation.LookAt( MainCamera.WorldRotation.Right, Vector3.Up );

		if ( Input.Down( "attack1" ) )
		{
			Log.Info( "Shooting" );

			// Wenn Maus1 dann ShootEffect Enablen und  TargetPos setzen 
			ShootEffect.Enabled = true;
			if ( ShootTrace.Hit ) { ShootEffect.TargetPosition = ShootTrace.HitPosition; }
			else 
			{ 
				//ShootEffect.TargetPosition = TurretRenderer.GetBoneObject( 1 ).WorldPosition + TurretRenderer.GetBoneObject( 1 ).WorldRotation.Left * 10000f; 
				ShootEffect.TargetPosition = ShootTrace.EndPosition;
			}
		}
		else 
		{ 
			ShootEffect.Enabled = false;
		}
	}
}
