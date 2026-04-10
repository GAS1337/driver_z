using Sandbox;
using Sandbox.Audio;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public sealed class WheelController : Component
{
	[Property] Rigidbody CarBody;
	RotationControl RotationControl;

	[Property] public WheelJoint FrontLeft;
	[Property] public WheelJoint FrontRight;
	[Property] public WheelJoint RearLeft;
	[Property] public WheelJoint RearRight;
	List<WheelJoint> WheelJointList;

	[Property] float Speed = 2000;
	[Property] float FullSpeedAdd = 2000;
	[Property] float Torque = 300;
	[Property] float SteeringAngle = 50;
	[Property] double JumpPower = 0.5f;
	[Property] int JumpMax = 100000;

	float TargetSpinSpeed;

	float originalTorque = 0;
	double JumpCharge;

	protected override void OnEnabled()
	{
		originalTorque = Torque;
		TargetSpinSpeed = 0;
		// Scene.TimeScale = 0.3f;
		//CarBody.InertiaTensor = 1000;
		// CarBody.EnhancedCcd = true;
		RotationControl = GameObject.GetComponent<RotationControl>();

		WheelJointList = new List<WheelJoint>();
		WheelJointList.Add( FrontLeft );
		WheelJointList.Add( FrontRight );
		WheelJointList.Add( RearLeft );
		WheelJointList.Add( RearRight );

		
		FrontLeft.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		FrontRight.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		RearLeft.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		RearRight.GetComponentInParent<Rigidbody>().EnhancedCcd = true;
		

	}

	protected override void OnUpdate()
	{
		// Log.Info( $"{Math.Round( FrontLeft.SpinSpeed, 0 )}  {Math.Round( FrontRight.SpinSpeed, 0 )}  {Math.Round( RearLeft.SpinSpeed, 0 )} {Math.Round( RearRight.SpinSpeed, 0 )}" );
		TurnMotorOn( false );
		TargetSpinSpeed = 0;
	}

	protected override void OnFixedUpdate()
	{
		SetSpeedAndTorque( TargetSpinSpeed );

		if ( Input.Down( "Jump" ) )
		{
			Brake();
		}
		if ( Input.Released( "Jump" ) )
		{
			JumpCharge = 0;
		}

		if ( Input.Down( "Left" ) )
		{
			foreach ( WheelJoint wheel in WheelJointList )
			{
				wheel.SteeringLimits = new Vector2 ( -SteeringAngle - 7, SteeringAngle + 7);
			}
			FrontLeft.TargetSteeringAngle = SteeringAngle + 3; FrontRight.TargetSteeringAngle = SteeringAngle;
			RearLeft.TargetSteeringAngle = -SteeringAngle / 10; RearRight.TargetSteeringAngle = -SteeringAngle / 10;
		}
		else if ( Input.Down( "Right" ) )
		{
			foreach ( WheelJoint wheel in WheelJointList )
			{
				wheel.SteeringLimits = new Vector2( -SteeringAngle - 7, SteeringAngle + 7 );
			}
			FrontLeft.TargetSteeringAngle = -SteeringAngle; FrontRight.TargetSteeringAngle =  -SteeringAngle - 3f;
			RearLeft.TargetSteeringAngle = SteeringAngle / 10; RearRight.TargetSteeringAngle = SteeringAngle / 10;
		}
		else
		{
			foreach ( WheelJoint wheel in WheelJointList )
			{
				wheel.TargetSteeringAngle = 0;
				wheel.SteeringLimits = new Vector2( 0, 0 );
				//wheel.EnableSteeringLimit = false;
			}
		}


	}

	private void SetSpeedAndTorque( float target)
	{
		FrontLeft.SpinMotorSpeed = FrontLeft.SpinMotorSpeed.LerpTo(target, 0.005f);
		FrontRight.SpinMotorSpeed = FrontRight.SpinMotorSpeed.LerpTo( target, 0.005f );
		RearLeft.SpinMotorSpeed = RearLeft.SpinMotorSpeed.LerpTo( target, 0.005f );
		RearRight.SpinMotorSpeed = RearRight.SpinMotorSpeed.LerpTo( target, 0.005f );

		FrontLeft.MaxSpinTorque = float.Clamp(originalTorque.LerpTo(originalTorque / (FrontLeft.SpinSpeed * 0.001f + 1), 1f), originalTorque / 2, originalTorque) ;
		FrontRight.MaxSpinTorque = float.Clamp( originalTorque.LerpTo( originalTorque / (FrontLeft.SpinSpeed * 0.001f + 1), 1f ), originalTorque / 2, originalTorque );
		RearLeft.MaxSpinTorque = float.Clamp( originalTorque.LerpTo(originalTorque / (FrontLeft.SpinSpeed * 0.001f + 1), 1f), originalTorque / 2, originalTorque );
		RearRight.MaxSpinTorque = float.Clamp( originalTorque.LerpTo(originalTorque / (FrontLeft.SpinSpeed * 0.001f + 1), 1f), originalTorque / 2, originalTorque );
	}

	void TurnMotorOn( bool status )
	{
		FrontRight.EnableSpinMotor = status;
		RearRight.EnableSpinMotor = status;
		FrontLeft.EnableSpinMotor = status;
		RearLeft.EnableSpinMotor = status;

	}

	void Brake()
	{
		TurnMotorOn( true );
		if ( RotationControl.groundCheck.Hit )
		{
			// Log.Info( "Brake" );
			RearLeft.MaxSpinTorque = Torque * 4; RearRight.MaxSpinTorque = Torque * 4;
			FrontLeft.MaxSpinTorque = Torque * 4; FrontRight.MaxSpinTorque = Torque * 4;

			RearLeft.SpinMotorSpeed = 0f; RearRight.SpinMotorSpeed = 0f;
			FrontLeft.SpinMotorSpeed = 0f; FrontRight.SpinMotorSpeed = 0f;
		}
		else { CarBody.SmoothRotate( Rotation.From( 0, CarBody.WorldRotation.Yaw(), 0 ), 1f, Time.Delta ); }
	}

	void Jump() 
	{
		// downward impulse to use suspension for jump
		Log.Info( "Jump" );
		JumpCharge = JumpCharge * 1.15f + JumpPower;
		JumpCharge = Math.Min(JumpCharge, JumpMax);

		CarBody.ApplyImpulse(CarBody.WorldRotation.Down * (float)JumpCharge);
	}




}
