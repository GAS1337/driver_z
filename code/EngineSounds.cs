using Sandbox;
using Sandbox.Audio;
using static WheelController;

public sealed class EngineSounds : Component
{
	[Property] Rigidbody CarBody;
	WheelController WheelController;

	[Property] SoundPointComponent IdleSound { get; set; }
	[Property] SoundPointComponent GasSound { get; set; }
	[Property] SoundPointComponent VollGasSound { get; set; }
	[Property] public SoundPointComponent ReifenSound { get; set; }

	private SoundHandle idleHandle;
	private SoundHandle gasHandle;
	private SoundHandle vollGasHandle;
	private SoundHandle reifenHandle;

	float reifenSpeed;

	TimeSince SinceSoundRestart;

	protected override void OnStart()
	{
		WheelController = GameObject.GetComponent<WheelController>();

		/*
		// Alle Sounds geloopt starten, aber initial stumm
		idleHandle = Sound.Play( IdleSound, CarBody.WorldPosition );
		gasHandle = Sound.Play( GasSound, WorldPosition );
		vollGasHandle = Sound.Play( VollGasSound, WorldPosition );
		reifenHandle = Sound.Play( ReifenSound, WorldPosition );

		idleHandle.Parent = CarBody.GameObject.Children.First();
		gasHandle.Parent = CarBody.GameObject.Children.First();
		vollGasHandle.Parent = CarBody.GameObject.Children.First();
		reifenHandle.Parent = CarBody.GameObject.Children.First();

		idleHandle.FollowParent = true;
		gasHandle.FollowParent = true;
		vollGasHandle.FollowParent = true;
		reifenHandle.FollowParent = true;

		idleHandle.Volume = 0;
		gasHandle.Volume = 0;
		vollGasHandle.Volume = 0;
		reifenHandle.Volume = 0;
		*/
	}

	protected override void OnFixedUpdate()
	{
		if (SinceSoundRestart > 30f)
		{
			Log.Info( "Restarting Engine Sounds" );

			IdleSound.Enabled = false;
			// GasSound.Enabled = false;
			VollGasSound.Enabled = false;
			ReifenSound.Enabled = false;

			IdleSound.Enabled = true;
			// GasSound.Enabled = true;
			VollGasSound.Enabled = true;
			ReifenSound.Enabled = true;

			SinceSoundRestart = 0f;
		}


		// Reifen Speed Durchschitt
		reifenSpeed = (WheelController.RearLeft.SpinSpeed + WheelController.RearRight.SpinSpeed + WheelController.FrontLeft.SpinSpeed + WheelController.FrontRight.SpinSpeed) * 0.25f;

		IdleSound.Volume = CarBody.Velocity.Length.Remap(0, 3600, 0.2f, 0.4f);
		//gasHandle.Volume = reifenSpeed.Remap( 100, 3000, 0.1f, 1 );
		VollGasSound.Volume = CarBody.Velocity.Length.Remap( 2000, 3200, 0, 0.6f );
		ReifenSound.Volume = CarBody.Velocity.Length.Remap( 100, 3000, 0, 0.5f );

		IdleSound.Pitch = CarBody.Velocity.Length.Remap( 1000, 3600, 0.8f, 1.6f );
		// gasHandle.Pitch = reifenSpeed.Remap( 100, 3000, 0.1f, 1 );
		VollGasSound.Pitch = CarBody.Velocity.Length.Remap( 2000, 3200, 0.8f, 1.2f );
		// reifenHandle.Pitch = reifenSpeed.Remap( 100, 6000, 0, 1 );
	}

}
