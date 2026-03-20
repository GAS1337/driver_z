using Sandbox;
using Sandbox.Audio;
using static WheelController;

public sealed class EngineSounds : Component
{
	[Property] Rigidbody CarBody;
	WheelController WheelController;

	[Property] SoundEvent IdleSound { get; set; }
	[Property] SoundEvent GasSound { get; set; }
	[Property] SoundEvent VollGasSound { get; set; }
	[Property] public SoundEvent ReifenSound { get; set; }

	private SoundHandle idleHandle;
	private SoundHandle gasHandle;
	private SoundHandle vollGasHandle;
	private SoundHandle reifenHandle;

	float reifenSpeed;

	protected override void OnStart()
	{
		WheelController = GameObject.GetComponent<WheelController>();

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
	}

	protected override void OnUpdate()
	{
		// Reifen Speed Durchschitt
		reifenSpeed = (WheelController.RearLeft.SpinSpeed + WheelController.RearRight.SpinSpeed + WheelController.FrontLeft.SpinSpeed + WheelController.FrontRight.SpinSpeed) * 0.25f;

		idleHandle.Volume = reifenSpeed.Remap(0, 6000, 0.1f, 0.2f);
		//gasHandle.Volume = reifenSpeed.Remap( 100, 3000, 0.1f, 1 );
		vollGasHandle.Volume = reifenSpeed.Remap( 3500, 6000, 0, 0.5f );
		reifenHandle.Volume = reifenSpeed.Remap( 100, 3000, 0, 0.2f );

		idleHandle.Pitch = reifenSpeed.Remap( 1000, 6000, 0.8f, 1.6f );
		// gasHandle.Pitch = reifenSpeed.Remap( 100, 3000, 0.1f, 1 );
		vollGasHandle.Pitch = reifenSpeed.Remap( 3500, 6000, 0.8f, 1.2f );
		// reifenHandle.Pitch = reifenSpeed.Remap( 100, 6000, 0, 1 );
	}
}
