using Sandbox;

public sealed class WheelController : Component
{
	[Property] WheelJoint FrontLeft;
	[Property] WheelJoint FrontRight;
	[Property] WheelJoint RearLeft;
	[Property] WheelJoint RearRight;


	[Property] bool FourWheelDrive = true;
	[Property] int Speed = 2000;
	[Property] int FullSpeedAdd = 2000;
	[Property] int Torque = 300;
	[Property] int SteeringAngle = 50;

	protected override void OnUpdate()
	{
		RearLeft.MaxSpinTorque = Torque;
		RearRight.MaxSpinTorque = Torque;
		FrontLeft.MaxSpinTorque = Torque;
		FrontRight.MaxSpinTorque = Torque;

		if ( Input.Down( "Forward" ) )
		{
			Log.Info( "Test" );
			RearLeft.SpinMotorSpeed = Speed;
			RearRight.SpinMotorSpeed = Speed;
			if ( Input.Down( "Run" ) ) 
			{
				RearLeft.SpinMotorSpeed += FullSpeedAdd;
				RearRight.SpinMotorSpeed += FullSpeedAdd;
			}
			if ( !FourWheelDrive ) return;
			
			FrontLeft.SpinMotorSpeed = Speed;
			FrontRight.SpinMotorSpeed = Speed;
			if ( Input.Down( "Run" ) )
			{
				FrontLeft.SpinMotorSpeed += FullSpeedAdd;
				FrontRight.SpinMotorSpeed += FullSpeedAdd;
			}
			
		}
		else if ( Input.Down( "Backward" ) )
		{
			RearLeft.SpinMotorSpeed = -Speed;
			RearRight.SpinMotorSpeed = -Speed;
			if ( FourWheelDrive )
			{
				FrontLeft.SpinMotorSpeed = -Speed;
				FrontRight.SpinMotorSpeed = -Speed;
			}
		}
		else { RearLeft.SpinMotorSpeed = 0f; RearRight.SpinMotorSpeed = 0f; 
			FrontLeft.SpinMotorSpeed = 0f; FrontRight.SpinMotorSpeed = 0f; }

		if ( Input.Down( "Left" ) ) { FrontLeft.TargetSteeringAngle = SteeringAngle; FrontRight.TargetSteeringAngle = SteeringAngle; }
		else if ( Input.Down( "Right" ) ) { FrontLeft.TargetSteeringAngle = -SteeringAngle; FrontRight.TargetSteeringAngle = -SteeringAngle; }
		else { FrontLeft.TargetSteeringAngle = 0f; FrontRight.TargetSteeringAngle = 0f; }


	}
}
