using Sandbox;
using System;
using System.Collections.Generic;

namespace OpenGrass;

/// <summary>
/// Standalone GPU-instanced grass field for s&amp;box.
///
/// Drops grass blades by raycasting downward across a rectangular area, groups
/// them into spatial chunks, and renders each chunk via DrawModelInstanced
/// with distance-based LOD, frustum culling, and a simpler far mesh.
///
/// By default it builds an X-shaped two-quad mesh, but you can plug a list of
/// <see cref="CustomModels"/> (and optional <see cref="CustomModelsSimple"/> for
/// far distances) — each blade picks one deterministically from <see cref="Seed"/>,
/// giving variety. Author custom models 1 unit tall and 1 unit wide; they get
/// scaled per-blade by <see cref="Width"/> / <see cref="Height"/>.
///
/// Self-contained: no external dependencies. Drop into any s&amp;box project.
/// </summary>
public sealed class GrassField : Component
{
	// === Field ===

	/// <summary> Field size in world units (X, Y). Centered on the GameObject. </summary>
	[Property, Category( "Field" )]
	public Vector2 FieldSize { get; set; } = new( 45000, 45000 );

	/// <summary> Blades per square unit (before slope/tag/exclusion filtering). </summary>
	[Property, Category( "Field" ), Range( 0.1f, 10f )]
	public float Density { get; set; } = 2f;

	/// <summary> Random seed — same value gives the same layout. </summary>
	[Property, Category( "Field" )]
	public int Seed { get; set; } = 12345;

	// === Mesh ===

	/// <summary>
	/// Material used for the built-in mesh. Should support vertex colors and ideally
	/// alpha-test cutout (g_flAlphaTestReference). If null, a plain fallback is created
	/// so the component still renders something — assign your own for an actual grass look.
	/// </summary>
	[Property, Category( "Mesh" )]
	public Material GrassMaterial { get; set; }

	/// <summary>
	/// Optional custom meshes used in place of the built-in two-quad blade. If more than one
	/// is supplied, each blade picks one deterministically from <see cref="Seed"/>, giving you
	/// variety (different shapes / sizes / textures) at the cost of one extra DrawModelInstanced
	/// per used model per chunk. Empty list = built-in mesh for every blade.
	/// Author each model 1×1×1 units — they are scaled per-blade by Width/Height.
	/// </summary>
	[Property, Category( "Mesh" )]
	public List<Model> CustomModels { get; set; } = new();

	/// <summary>
	/// Optional simpler meshes for far-distance LOD, parallel to <see cref="CustomModels"/>.
	/// Index N here is the simple variant for index N of CustomModels. Holes (null entries
	/// or shorter list) fall back to the corresponding CustomModels entry, then the built-in.
	/// </summary>
	[Property, Category( "Mesh" )]
	public List<Model> CustomModelsSimple { get; set; } = new();

	// === Blade ===

	[Property, Category( "Blade" ), Range( 1f, 200f )]
	public float Height { get; set; } = 1f;

	[Property, Category( "Blade" ), Range( 1f, 100f )]
	public float Width { get; set; } = 1f;

	/// <summary> Per-blade ±variation of Height/Width (0 = uniform, 1 = up to ±100%). </summary>
	[Property, Category( "Blade" ), Range( 0f, 1f )]
	public float HeightVariation { get; set; } = 0.5f;

	/// <summary> Vertex color at the base of the built-in blade. Ignored when CustomModels is non-empty. </summary>
	[Property, Category( "Blade" )]
	public Color BottomColor { get; set; } = Color.White;

	/// <summary> Vertex color at the tip of the built-in blade. Ignored when CustomModels is non-empty. </summary>
	[Property, Category( "Blade" )]
	public Color TipColor { get; set; } = Color.White;

	/// <summary> Tilt blades to match the surface normal (slopes follow the ground). </summary>
	[Property, Category( "Blade" )]
	public bool AlignToSurface { get; set; } = true;

	// === Performance ===

	/// <summary> Hard cap on blade count — guards against absurd densities. </summary>
	[Property, Category( "Performance" ), Range( 100, 2_000_000 )]
	public int MaxGrassCount { get; set; } = 150_000;

	/// <summary> Spatial chunk size. Smaller = finer culling but more draw calls. </summary>
	[Property, Category( "Performance" ), Range( 50f, 500f )]
	public float ChunkSize { get; set; } = 500f;

	/// <summary> Chunks past this distance from the camera are not rendered. </summary>
	[Property, Category( "Performance" ), Range( 100f, 5000f )]
	public float RenderDistance { get; set; } = 4500f;

	[Property, Category( "Performance" )]
	public bool UseLOD { get; set; } = true;

	/// <summary> Within this distance, 100% of blades render. </summary>
	[Property, Category( "Performance" ), Range( 100f, 2000f )]
	public float LOD0Distance { get; set; } = 2000f;

	/// <summary> Up to this distance, ~50% of blades render. </summary>
	[Property, Category( "Performance" ), Range( 200f, 3000f )]
	public float LOD1Distance { get; set; } = 3000f;

	/// <summary> Past this distance, ~25% of blades render. </summary>
	[Property, Category( "Performance" ), Range( 300f, 4000f )]
	public float LOD2Distance { get; set; } = 4000f;

	/// <summary> Skip chunks behind the camera (with margin for peripheral vision). </summary>
	[Property, Category( "Performance" )]
	public bool UseFrustumCulling { get; set; } = true;

	/// <summary> Chunks closer than this never get frustum-culled (avoids popping when turning). </summary>
	[Property, Category( "Performance" ), Range( 100f, 2000f )]
	public float FrustumCullMinDistance { get; set; } = 800f;

	/// <summary> Past this distance, render the simpler (1-quad / CustomModelsSimple) variant. </summary>
	[Property, Category( "Performance" ), Range( 500f, 4000f )]
	public float SimpleMeshDistance { get; set; } = 3000f;

	/// <summary> Cast shadows. Very expensive at high blade counts — leave off unless you need it. </summary>
	[Property, Category( "Performance" )]
	public bool CastShadows { get; set; } = false;

	// === Filtering ===

	/// <summary> Required tag on the surface object. Empty = any object. </summary>
	[Property, Category( "Filtering" )]
	public string RequiredTag { get; set; } = "";

	/// <summary> Tags that block grass spawning AND are skipped during the trace. </summary>
	[Property, Category( "Filtering" )]
	public List<string> IgnoredTags { get; set; } = new();

	/// <summary> Tag for player colliders — skipped during the ground trace. Empty disables. </summary>
	[Property, Category( "Filtering" )]
	public string PlayerTag { get; set; } = "player";

	/// <summary> Maximum surface slope (degrees) that still spawns grass. </summary>
	[Property, Category( "Filtering" ), Range( 0f, 90f )]
	public float MaxSlopeAngle { get; set; } = 45f;

	/// <summary> Trigger volumes with any of these tags carve out "no grass" zones (paths, roads, ...). </summary>
	[Property, Category( "Filtering" )]
	public List<string> ExclusionZoneTags { get; set; } = new() { "nograss" };

	// === Trace ===

	/// <summary> Trace start height, relative to the GameObject's WorldPosition. </summary>
	[Property, Category( "Trace" ), Range( -4000f, 4000f )]
	public float TraceStartHeight { get; set; } = 0f;

	/// <summary> Trace end height, relative to the GameObject's WorldPosition. </summary>
	[Property, Category( "Trace" ), Range( -4000f, 4000f )]
	public float TraceEndHeight { get; set; } = -2000f;

	// === Buttons ===

	[Button( "Regenerate" )]
	public void Regenerate()
	{
		_isDirty = true;
		_gizmoTracesDirty = true;
	}

	[Button( "Refresh Gizmo Traces" ), Category( "Debug" )]
	public void RefreshGizmoTraces() => _gizmoTracesDirty = true;

	// === Public runtime API ===

	/// <summary>
	/// Apply a runtime tint via the material's g_vColorTint parameter.
	/// Useful for day/night, biome blends, or ambient zone effects driven by other systems.
	/// Only affects the auto-created fallback material — when you assign your own
	/// <see cref="GrassMaterial"/>, drive its parameters yourself.
	/// </summary>
	public void SetTint( Color tint )
	{
		_ownedMaterial?.Set( "g_vColorTint", tint );
	}

	// === Internals ===

	private Model _builtInModel;
	private Model _builtInModelSimple;
	private Material _ownedMaterial;
	private SceneCustomObject _sceneObject;
	private Transform[] _allTransforms;
	private readonly List<GrassChunk> _chunks = new();
	private readonly Dictionary<(int, int), int> _chunkGrid = new();
	private readonly List<CachedGrassData> _cachedGrassData = new();
	private bool _isDirty = true;

	private readonly List<GizmoTraceData> _gizmoTraces = new();
	private bool _gizmoTracesDirty = true;

	private struct CachedGrassData
	{
		public Vector3 Position;
		public Vector3 SurfaceNormal;
		public float Height;
		public float Width;
		public float Yaw;
		// 0 = renders on every LOD, 1 = LOD0+LOD1, 2 = LOD0 only.
		// Sorting blades by (ModelIndex, LodGroup) lets us draw each (model, LOD)
		// run as one contiguous instanced call.
		public byte LodGroup;
		public byte ModelIndex; // index into CustomModels, or 0 when using built-in
	}

	private struct ModelSegment
	{
		public int ModelIndex;
		public int StartIndex;
		public int Lod0Count; // all blades for this model — drawn at LOD0
		public int Lod1Count; // ~50% — drawn at LOD1
		public int Lod2Count; // ~25% — drawn at LOD2 and beyond
	}

	private struct GrassChunk
	{
		public Vector3 Center;
		public BBox Bounds;
		// One entry per ModelIndex used in this chunk. Each segment is one DrawModelInstanced.
		public ModelSegment[] Segments;
	}

	private struct GizmoTraceData
	{
		public Vector3 StartPos;
		public Vector3 HitPos;
		public Color Color;
		public string DebugInfo;
	}

	protected override void OnEnabled()
	{
		EnsureMeshes();
		CreateSceneObject();
		RegenerateGrass();
		_gizmoTracesDirty = true;
	}

	protected override void OnDisabled()
	{
		_sceneObject?.Delete();
		_sceneObject = null;
	}

	protected override void OnUpdate()
	{
		if ( _isDirty )
		{
			RegenerateGrass();
			_isDirty = false;
		}
	}

	private void EnsureMeshes()
	{
		// Always build the built-in pair — it's the universal fallback for null entries
		// or holes in CustomModels / CustomModelsSimple. Cheap (two tiny meshes).
		var material = GrassMaterial ?? CreateFallbackMaterial();
		_builtInModel = BuildBuiltInModel( material, twoQuads: true );
		_builtInModelSimple = BuildBuiltInModel( material, twoQuads: false );
	}

	private Material CreateFallbackMaterial()
	{
		_ownedMaterial = Material.Create( $"opengrass_fallback_{GetHashCode()}", "shaders/complex.shader" );
		_ownedMaterial.Set( "g_vColorTint", Color.White );
		return _ownedMaterial;
	}

	private Model BuildBuiltInModel( Material material, bool twoQuads )
	{
		const float w = 0.5f;
		const float h = 1.0f;

		var vb = new VertexBuffer();
		vb.Init( true );

		AddBuiltInQuad( vb, new Vector3( -w, 0, 0 ), new Vector3( w, 0, 0 ), new Vector3( w, 0, h ), new Vector3( -w, 0, h ) );
		if ( twoQuads )
			AddBuiltInQuad( vb, new Vector3( 0, -w, 0 ), new Vector3( 0, w, 0 ), new Vector3( 0, w, h ), new Vector3( 0, -w, h ) );

		var mesh = new Mesh( material );
		mesh.CreateBuffers( vb );
		return Model.Builder.AddMesh( mesh ).Create();
	}

	private void AddBuiltInQuad( VertexBuffer vb, Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		var normal = Vector3.Cross( b - a, d - a ).Normal;
		var tangent = (b - a).Normal;

		// Front face
		vb.Add( new Vertex( a, normal, tangent, new Vector2( 0, 1 ) ) { Color = BottomColor } );
		vb.Add( new Vertex( b, normal, tangent, new Vector2( 1, 1 ) ) { Color = BottomColor } );
		vb.Add( new Vertex( c, normal, tangent, new Vector2( 1, 0 ) ) { Color = TipColor } );
		vb.Add( new Vertex( d, normal, tangent, new Vector2( 0, 0 ) ) { Color = TipColor } );
		vb.AddTriangleIndex( 4, 3, 2 );
		vb.AddTriangleIndex( 2, 1, 4 );

		// Back face — blades shouldn't disappear from behind
		vb.Add( new Vertex( b, -normal, tangent, new Vector2( 1, 1 ) ) { Color = BottomColor } );
		vb.Add( new Vertex( a, -normal, tangent, new Vector2( 0, 1 ) ) { Color = BottomColor } );
		vb.Add( new Vertex( d, -normal, tangent, new Vector2( 0, 0 ) ) { Color = TipColor } );
		vb.Add( new Vertex( c, -normal, tangent, new Vector2( 1, 0 ) ) { Color = TipColor } );
		vb.AddTriangleIndex( 4, 3, 2 );
		vb.AddTriangleIndex( 2, 1, 4 );
	}

	private void CreateSceneObject()
	{
		if ( !Scene.IsValid() ) return;

		_sceneObject = new SceneCustomObject( Scene.SceneWorld )
		{
			RenderOverride = RenderGrass,
			Transform = WorldTransform,
		};
		_sceneObject.Flags.CastShadows = CastShadows;
		_sceneObject.Flags.IsOpaque = true;
		_sceneObject.Flags.IsTranslucent = false;
	}

	private void RegenerateGrass()
	{
		if ( !Scene.IsValid() ) return;

		var random = new Random( Seed );

		int potentialCount = (int)(FieldSize.x * FieldSize.y * Density);
		potentialCount = Math.Min( potentialCount, MaxGrassCount );

		float halfX = FieldSize.x * 0.5f;
		float halfY = FieldSize.y * 0.5f;
		float maxSlopeCos = MathF.Cos( MaxSlopeAngle * MathF.PI / 180f );

		_cachedGrassData.Clear();

		// 1 when no custom models — every blade uses the built-in mesh (ModelIndex 0).
		int modelCount = Math.Max( 1, CustomModels?.Count ?? 0 );

		for ( int i = 0; i < potentialCount; i++ )
		{
			float x = (float)(random.NextDouble() * 2 - 1) * halfX;
			float y = (float)(random.NextDouble() * 2 - 1) * halfY;

			var startPos = WorldPosition + new Vector3( x, y, TraceStartHeight );
			var endPos = WorldPosition + new Vector3( x, y, TraceEndHeight );

			var trace = BuildGroundTrace( startPos, endPos ).Run();
			if ( !trace.Hit ) continue;
			if ( !IsValidSurface( trace ) ) continue;

			float dotUp = Vector3.Dot( trace.Normal, Vector3.Up );
			if ( dotUp < maxSlopeCos ) continue;

			if ( IsInExclusionZone( trace.EndPosition ) ) continue;

			float scaleVar = 1f + (float)(random.NextDouble() * 2 - 1) * HeightVariation;
			float yaw = (float)(random.NextDouble() * 360);
			byte modelIndex = (byte)random.Next( modelCount );

			// 25% group 0 (LOD0+LOD1+LOD2), 25% group 1 (LOD0+LOD1), 50% group 2 (LOD0 only).
			byte lodGroup = (byte)(i % 4);
			if ( lodGroup >= 2 ) lodGroup = 2;

			_cachedGrassData.Add( new CachedGrassData
			{
				Position = trace.EndPosition,
				SurfaceNormal = trace.Normal,
				Height = Height * scaleVar,
				Width = Width * scaleVar,
				Yaw = yaw,
				LodGroup = lodGroup,
				ModelIndex = modelIndex,
			} );
		}

		BuildChunks();
	}

	private SceneTrace BuildGroundTrace( Vector3 from, Vector3 to )
	{
		var t = Scene.Trace.Ray( from, to );
		if ( !string.IsNullOrEmpty( PlayerTag ) )
			t = t.WithoutTags( PlayerTag );
		if ( IgnoredTags != null )
		{
			foreach ( var tag in IgnoredTags )
			{
				if ( !string.IsNullOrEmpty( tag ) )
					t = t.WithoutTags( tag );
			}
		}
		return t;
	}

	private bool IsValidSurface( SceneTraceResult trace )
	{
		if ( trace.GameObject == null ) return false;

		var tags = trace.GameObject.Tags;

		if ( IgnoredTags != null )
		{
			foreach ( var tag in IgnoredTags )
			{
				if ( !string.IsNullOrEmpty( tag ) && tags.Has( tag ) )
					return false;
			}
		}

		if ( !string.IsNullOrEmpty( RequiredTag ) )
			return tags.Has( RequiredTag );

		return true;
	}

	private bool IsInExclusionZone( Vector3 position )
	{
		if ( ExclusionZoneTags == null || ExclusionZoneTags.Count == 0 )
			return false;

		// Probe up and down a short distance to catch trigger volumes that cross the surface.
		if ( ExclusionZoneTrace( position, position + Vector3.Up * 50f ) ) return true;
		if ( ExclusionZoneTrace( position + Vector3.Up * 5f, position - Vector3.Up * 50f ) ) return true;
		return false;
	}

	private bool ExclusionZoneTrace( Vector3 from, Vector3 to )
	{
		var trace = Scene.Trace.Ray( from, to ).HitTriggers();
		if ( !string.IsNullOrEmpty( PlayerTag ) )
			trace = trace.WithoutTags( PlayerTag );

		var hit = trace.Run();
		if ( !hit.Hit || hit.GameObject == null ) return false;

		foreach ( var tag in ExclusionZoneTags )
		{
			if ( !string.IsNullOrEmpty( tag ) && hit.GameObject.Tags.Has( tag ) )
				return true;
		}
		return false;
	}

	private void BuildChunks()
	{
		_chunks.Clear();
		_chunkGrid.Clear();

		if ( _cachedGrassData.Count == 0 )
		{
			_allTransforms = Array.Empty<Transform>();
			return;
		}

		// Bucket blade indices by chunk grid cell.
		var chunkBuckets = new Dictionary<(int, int), List<int>>();
		for ( int i = 0; i < _cachedGrassData.Count; i++ )
		{
			var pos = _cachedGrassData[i].Position;
			int cx = (int)Math.Floor( (pos.x - WorldPosition.x) / ChunkSize );
			int cy = (int)Math.Floor( (pos.y - WorldPosition.y) / ChunkSize );
			if ( !chunkBuckets.TryGetValue( (cx, cy), out var list ) )
				chunkBuckets[(cx, cy)] = list = new List<int>();
			list.Add( i );
		}

		_allTransforms = new Transform[_cachedGrassData.Count];
		int currentIndex = 0;

		var segmentScratch = new List<ModelSegment>();

		foreach ( var (key, indices) in chunkBuckets )
		{
			// Sort blades by (ModelIndex, LodGroup) — gives us contiguous runs of
			// "same model, sorted by LOD group" so each segment's first N transforms
			// are exactly what we want to draw at a given LOD.
			indices.Sort( ( a, b ) =>
			{
				var da = _cachedGrassData[a];
				var db = _cachedGrassData[b];
				int byModel = da.ModelIndex.CompareTo( db.ModelIndex );
				return byModel != 0 ? byModel : da.LodGroup.CompareTo( db.LodGroup );
			} );

			var chunk = new GrassChunk();
			var bounds = new BBox();
			float cxSum = 0, cySum = 0, czSum = 0;

			segmentScratch.Clear();
			ModelSegment current = default;
			int lastModelIndex = -1;

			foreach ( var idx in indices )
			{
				var data = _cachedGrassData[idx];

				Rotation rot;
				if ( AlignToSurface && data.SurfaceNormal != Vector3.Up )
				{
					var surfaceRot = Rotation.FromToRotation( Vector3.Up, data.SurfaceNormal );
					var yawRot = Rotation.FromYaw( data.Yaw );
					rot = surfaceRot * yawRot;
				}
				else
				{
					rot = Rotation.FromYaw( data.Yaw );
				}

				var scale = new Vector3( data.Width, data.Width, data.Height );
				_allTransforms[currentIndex] = new Transform( data.Position, rot, scale );

				if ( data.ModelIndex != lastModelIndex )
				{
					if ( lastModelIndex != -1 )
						segmentScratch.Add( current );
					current = new ModelSegment { ModelIndex = data.ModelIndex, StartIndex = currentIndex };
					lastModelIndex = data.ModelIndex;
				}

				if ( data.LodGroup == 0 ) current.Lod2Count++;
				if ( data.LodGroup <= 1 ) current.Lod1Count++;
				current.Lod0Count++;

				bounds = bounds.AddPoint( data.Position );
				bounds = bounds.AddPoint( data.Position + new Vector3( 0, 0, Height ) );
				cxSum += data.Position.x;
				cySum += data.Position.y;
				czSum += data.Position.z;
				currentIndex++;
			}

			if ( lastModelIndex != -1 )
				segmentScratch.Add( current );

			chunk.Segments = segmentScratch.ToArray();
			chunk.Center = new Vector3( cxSum / indices.Count, cySum / indices.Count, czSum / indices.Count );
			chunk.Bounds = bounds;

			_chunkGrid[key] = _chunks.Count;
			_chunks.Add( chunk );
		}
	}

	private void RenderGrass( SceneObject obj )
	{
		if ( _allTransforms == null || _allTransforms.Length == 0 || _chunks.Count == 0 )
			return;

		var camera = Scene.Camera;
		if ( !camera.IsValid() ) return;

		var cameraPos = camera.WorldPosition;
		var cameraForward = camera.WorldRotation.Forward;

		float renderDistSq = RenderDistance * RenderDistance;
		float lod0DistSq = LOD0Distance * LOD0Distance;
		float lod1DistSq = LOD1Distance * LOD1Distance;
		float simpleMeshDistSq = SimpleMeshDistance * SimpleMeshDistance;
		float frustumMinDistSq = FrustumCullMinDistance * FrustumCullMinDistance;

		// Iterate only the chunks within RenderDistance of the camera (square around camera cell).
		int chunksInRadius = (int)MathF.Ceiling( RenderDistance / ChunkSize ) + 1;
		int cameraCx = (int)MathF.Floor( (cameraPos.x - WorldPosition.x) / ChunkSize );
		int cameraCy = (int)MathF.Floor( (cameraPos.y - WorldPosition.y) / ChunkSize );

		for ( int dx = -chunksInRadius; dx <= chunksInRadius; dx++ )
		{
			for ( int dy = -chunksInRadius; dy <= chunksInRadius; dy++ )
			{
				if ( !_chunkGrid.TryGetValue( (cameraCx + dx, cameraCy + dy), out int chunkIndex ) )
					continue;

				var chunk = _chunks[chunkIndex];

				float distSq = (chunk.Center - cameraPos).LengthSquared;
				if ( distSq > renderDistSq ) continue;

				if ( UseFrustumCulling && distSq > frustumMinDistSq )
				{
					var toChunk = (chunk.Center - cameraPos).Normal;
					// dot < -0.2 = clearly behind the camera (margin keeps peripheral chunks visible).
					if ( Vector3.Dot( cameraForward, toChunk ) < -0.2f )
						continue;
				}

				bool useSimple = distSq > simpleMeshDistSq;

				// One DrawModelInstanced per (model used in this chunk).
				foreach ( var seg in chunk.Segments )
				{
					int renderCount;
					if ( !UseLOD ) renderCount = seg.Lod0Count;
					else if ( distSq <= lod0DistSq ) renderCount = seg.Lod0Count;
					else if ( distSq <= lod1DistSq ) renderCount = seg.Lod1Count;
					else renderCount = seg.Lod2Count;

					if ( renderCount <= 0 ) continue;

					var model = useSimple ? PickSimpleModel( seg.ModelIndex ) : PickFullModel( seg.ModelIndex );
					if ( model == null ) continue;

					var transforms = _allTransforms.AsSpan( seg.StartIndex, renderCount );
					Graphics.DrawModelInstanced( model, transforms );
				}
			}
		}
	}

	private Model PickFullModel( int modelIndex )
	{
		if ( CustomModels != null && modelIndex < CustomModels.Count && CustomModels[modelIndex] != null )
			return CustomModels[modelIndex];
		return _builtInModel;
	}

	private Model PickSimpleModel( int modelIndex )
	{
		// Prefer the simple variant; if missing, drop down to the full custom mesh,
		// then to the built-in simple mesh.
		if ( CustomModelsSimple != null && modelIndex < CustomModelsSimple.Count && CustomModelsSimple[modelIndex] != null )
			return CustomModelsSimple[modelIndex];
		if ( CustomModels != null && modelIndex < CustomModels.Count && CustomModels[modelIndex] != null )
			return CustomModels[modelIndex];
		return _builtInModelSimple ?? _builtInModel;
	}

	// === Gizmos: visualize trace results when this component is selected. ===

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		if ( _gizmoTracesDirty )
		{
			RebuildGizmoTraces();
			_gizmoTracesDirty = false;
		}

		var cameraPos = Gizmo.Camera.Position;
		const float drawDistSq = 1000f * 1000f;
		const float textDistSq = 200f * 200f;

		Gizmo.Draw.LineThickness = 1f;

		foreach ( var trace in _gizmoTraces )
		{
			var worldPos = WorldPosition + trace.StartPos;
			var distSq = (worldPos - cameraPos).LengthSquared;
			if ( distSq > drawDistSq ) continue;

			Gizmo.Draw.Color = trace.Color;
			Gizmo.Draw.Line( trace.StartPos, trace.HitPos );

			// Show why a trace was rejected (red = filtered out by IsValidSurface).
			if ( trace.Color == Color.Red && distSq < textDistSq && !string.IsNullOrEmpty( trace.DebugInfo ) )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.ScreenText( trace.DebugInfo, WorldPosition + trace.HitPos, Vector2.Zero, "Roboto", 10 );
			}
		}
	}

	private void RebuildGizmoTraces()
	{
		_gizmoTraces.Clear();

		var random = new Random( Seed );
		int potentialCount = (int)(FieldSize.x * FieldSize.y * Density);
		potentialCount = Math.Min( potentialCount, MaxGrassCount );

		float halfX = FieldSize.x * 0.5f;
		float halfY = FieldSize.y * 0.5f;
		float maxSlopeCos = MathF.Cos( MaxSlopeAngle * MathF.PI / 180f );

		for ( int i = 0; i < potentialCount; i++ )
		{
			float x = (float)(random.NextDouble() * 2 - 1) * halfX;
			float y = (float)(random.NextDouble() * 2 - 1) * halfY;

			var startLocal = new Vector3( x, y, TraceStartHeight );
			var endLocal = new Vector3( x, y, TraceEndHeight );
			var trace = BuildGroundTrace( WorldPosition + startLocal, WorldPosition + endLocal ).Run();

			var hitLocal = trace.Hit ? trace.EndPosition - WorldPosition : endLocal;
			Color color;
			string debugInfo = "";

			if ( !trace.Hit )
			{
				color = Color.Gray.WithAlpha( 0.1f );
				debugInfo = "No hit";
			}
			else if ( !IsValidSurface( trace ) )
			{
				color = Color.Red;
				var go = trace.GameObject;
				if ( go == null )
				{
					debugInfo = "GameObject is NULL";
				}
				else
				{
					var tagList = string.Join( ", ", go.Tags.TryGetAll() );
					debugInfo = $"GO: {go.Name}, Tags: [{tagList}], Need: {RequiredTag}";
				}
			}
			else if ( Vector3.Dot( trace.Normal, Vector3.Up ) < maxSlopeCos )
			{
				color = Color.Orange;
			}
			else if ( IsInExclusionZone( trace.EndPosition ) )
			{
				color = Color.Blue;
			}
			else
			{
				color = Color.Green;
			}

			_gizmoTraces.Add( new GizmoTraceData
			{
				StartPos = startLocal,
				HitPos = hitLocal,
				Color = color,
				DebugInfo = debugInfo,
			} );
		}
	}
}
