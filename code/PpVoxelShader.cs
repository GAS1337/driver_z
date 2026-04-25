using Sandbox;
using Sandbox.Rendering;

[Title( "Voxelizer" )]
[Category( "Post Processing" )]
[Icon( "Layers" )]
public sealed class Voxelizer : BasePostProcess<Voxelizer>
{
	[Property, Range( 1, 100 )]
	public float VoxelSize { get; set; } = 10.0f;

	[Property, Range( 0, 1 )]
	public float EdgeDarkening { get; set; } = 0.5f;

	// Pfad zu deinem kompilierten Shader
	private static Material VoxelMaterial = Material.FromShader( "shader/pp_voxel.shader" );

	public override void Render()
	{
		// Werte basierend auf Post-Process-Gewichtung abrufen
		float voxelSize = GetWeighted( x => x.VoxelSize );
		float edgeDarkening = GetWeighted( x => x.EdgeDarkening );

		// Attribute an den Shader senden
		Attributes.Set( "VoxelSize", voxelSize );
		Attributes.Set( "EdgeDarkening", edgeDarkening );

		// Blit ausführen: 
		// Wir nutzen den Backbuffer, rendern nach dem Post-Processing (Stage.AfterPostProcess)
		var blit = BlitMode.WithBackbuffer( VoxelMaterial, Stage.AfterPostProcess, 100, true );
		Blit( blit, "Voxelizer" );
	}
}
