using Sandbox;

public sealed class UIHint : Component
{
	TimeSince SinceStart = 0;
	bool IsPressed = false;


	protected override void OnUpdate()
	{
		if (Input.Pressed( "forward" ))
		{
			IsPressed = true;
		}
		if ( !IsPressed ) 
		{ 
			SinceStart = 0;
		}


		/*
		if ( Scene.Camera is null )
			return;

		var hud = Scene.Camera.Hud;

		if ( SinceStart < 5 )
		{
			hud.DrawText( new TextRendering.Scope( "This is your health!", Color.Red, 32, "Poppins", 800 ), new Vector2( Screen.Width * 0.05f, Screen.Height * 0.65f ) );
			hud.DrawText( new TextRendering.Scope( "This is your score \nand multiplier!", Color.Red, 32, "Poppins", 800 ), new Vector2( Screen.Width * 0.8f, Screen.Height * 0.3f ) );
		}
		else { GameObject.Enabled = false; } 
		*/
	}
}
