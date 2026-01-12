using Sandbox;
using System.Runtime.CompilerServices;

public sealed class WheelController : Component
{
	[Property] Rigidbody CarBody;

	[Property] WheelJoint FrontLeft;
	[Property] WheelJoint FrontRight;
	[Property] WheelJoint RearLeft;
	[Property] WheelJoint RearRight;


	[Property] bool FourWheelDrive = true;
	[Property] int Speed = 2000;
	[Property] int FullSpeedAdd = 2000;
	[Property] int Torque = 300;
	[Property] int SteeringAngle = 50;

	int originalTorque = 0;

	protected override void OnEnabled()
	{
		originalTorque = Torque;

		FrontLeft.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		FrontRight.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		RearLeft.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		RearRight.GetComponentInParent<Rigidbody>().EnhancedCcd = true;

	}

	protected override void OnUpdate()
	{
		RearLeft.MaxSpinTorque = originalTorque;
		RearRight.MaxSpinTorque = originalTorque;
		FrontLeft.MaxSpinTorque = originalTorque;
		FrontRight.MaxSpinTorque = originalTorque;

		int slowingfactor = 500;

		if ( Input.Down( "Forward" ) )
		{
			RearLeft.SpinMotorSpeed = (RearLeft.SpinMotorSpeed + (Speed / slowingfactor)).Clamp(0, Speed);
			RearRight.SpinMotorSpeed = (RearRight.SpinMotorSpeed + (Speed / slowingfactor)).Clamp( 0, Speed );
			if ( Input.Down( "Run" ) ) 
			{
				RearLeft.SpinMotorSpeed += FullSpeedAdd;
				RearRight.SpinMotorSpeed += FullSpeedAdd;
			}
			if ( !FourWheelDrive ) return;

			FrontLeft.SpinMotorSpeed = (FrontLeft.SpinMotorSpeed + (Speed / slowingfactor)).Clamp( 0, Speed );
			FrontRight.SpinMotorSpeed = (FrontRight.SpinMotorSpeed + (Speed / slowingfactor)).Clamp( 0, Speed );
			if ( Input.Down( "Run" ) )
			{
				FrontLeft.SpinMotorSpeed += FullSpeedAdd;
				FrontRight.SpinMotorSpeed += FullSpeedAdd;
			}
			
		}
		else if ( Input.Down( "Backward" ) )
		{
			RearLeft.SpinMotorSpeed = (RearLeft.SpinMotorSpeed - (Speed / slowingfactor)).Clamp( -Speed, 0 );
			RearRight.SpinMotorSpeed = (RearRight.SpinMotorSpeed - (Speed / slowingfactor)).Clamp( -Speed, 0 );
			if ( FourWheelDrive )
			{
				FrontLeft.SpinMotorSpeed = (FrontLeft.SpinMotorSpeed - (Speed / slowingfactor)).Clamp( -Speed, 0 );
				FrontRight.SpinMotorSpeed = (FrontRight.SpinMotorSpeed - (Speed / slowingfactor)).Clamp( -Speed, 0 );
			}
		}
		else {

			int factor = 1;
			if ( RearLeft.SpinMotorSpeed > 0 ) 
			{
				
				RearLeft.SpinMotorSpeed -= factor; 
				RearRight.SpinMotorSpeed -= factor; 
				FrontLeft.SpinMotorSpeed -= factor; 
				FrontRight.SpinMotorSpeed -= factor;
			
			}
			else if ( RearLeft.SpinMotorSpeed < 0 )
			{

				RearLeft.SpinMotorSpeed += factor;
				RearRight.SpinMotorSpeed += factor;
				FrontLeft.SpinMotorSpeed += factor;
				FrontRight.SpinMotorSpeed += factor;

			}

		}

		if ( Input.Down( "Jump" ) ) 
		{
			Brake();
		} 

		if ( Input.Down( "Left" ) ) 
		{ 
			FrontLeft.TargetSteeringAngle = SteeringAngle; FrontRight.TargetSteeringAngle = SteeringAngle;
			CarBody.AngularVelocity += CarBody.WorldRotation.Up * 0.25f;
			Rotation rot = Rotation.From(CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() + 170, CarBody.WorldRotation.Roll() );
			// CarBody.SmoothRotate(rot, 1f, Time.Delta); 
		}
		else if ( Input.Down( "Right" ) ) 
		{ 
			FrontLeft.TargetSteeringAngle = -SteeringAngle; FrontRight.TargetSteeringAngle = -SteeringAngle;
			CarBody.AngularVelocity += CarBody.WorldRotation.Down * 0.25f;
			Rotation rot = Rotation.From( CarBody.WorldRotation.Pitch(), CarBody.WorldRotation.Yaw() - 170, CarBody.WorldRotation.Roll() );
			// CarBody.SmoothRotate( rot, 1f, Time.Delta );
		}
		else { FrontLeft.TargetSteeringAngle = 0f; FrontRight.TargetSteeringAngle = 0f; }


	}

	void Brake() 
	{
		// no idea
		Log.Info( "Brake" );
		RearLeft.MaxSpinTorque = Torque * 4; RearRight.MaxSpinTorque = Torque * 4;
		FrontLeft.MaxSpinTorque = Torque * 4; FrontRight.MaxSpinTorque = Torque * 4;

		RearLeft.SpinMotorSpeed = 0f; RearRight.SpinMotorSpeed = 0f;
		FrontLeft.SpinMotorSpeed = 0f; FrontRight.SpinMotorSpeed = 0f;


	}
}
