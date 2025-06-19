#nullable enable
using Godot;
using GodotUtils;
using Project.Components;

namespace Project;

/// The player character controller.
/// This class mainly handles player movement.
/// As well as acting as a puppet for other classes (game related or not)
public partial class FirstPersonCharacter : CharacterBody3D {
	[ExportGroup("Local")]
	[Export] public required Node3D Head;
	[Export] public required Node3D HeadInner;
	[Export] public required Camera3D Camera;
	[Export] public required CollisionShape3D Collision;
	[Export] public required RayCast3D InteractRay;
	[Export] public required Node3D BodyPivot;
	[Export] public required FootstepManager FootstepManager;
	[Export] public required AudioStreamPlayer FearAudioPlayer;
	[Export] public AnimationTree ModelAnimTree;

	// Settings
	const float CrouchHeightModifier = 0.6f;
	const float Speed = 5.0f;
	const float Acceleration = 5.0f;
	public float MouseSensitivity = 1.0f;
	public bool Sprinting = false;
	public bool Crouching = false;
	public bool CanMove = true;

	// Variables
	float gravity = (float) ProjectSettings.GetSetting("physics/3d/default_gravity");
	bool jumping = false;
	float speed = Speed;
	float heartbeatZoom = 0;

    public float Fear = 0.0f;
    public float Zoom = 1.0f;
    public bool ClickAndPointMode = false;

	public Transform3D InitialTransform;
	Vector2 inputMouse;
	Vector2 inputMouseSmooth;
	IPlayerHoverable? currentlyHovered = null;
	IPlayerInteractable? currentlyInteracted = null;
	Vector3 initialHeadPosLocal;
	float initialCollisionHeight;
	float initialFov;

	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		InitialTransform = GlobalTransform;
		initialHeadPosLocal = Head.Position;
		initialCollisionHeight = (Collision.Shape as CapsuleShape3D)!.Height;
		initialFov = Camera.Fov;

		// Interaction
		if (ClickAndPointMode) {
			GetTree().PhysicsFrame += ProcessClickAndPointInteraction;
		} else {
			GetTree().ProcessFrame += Process3dInteraction;
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (Rotation != Vector3.Zero) {
			GD.PushWarning($"Player rotation should be achieved with {nameof(BodyPivot)}! Rotation was set on {Name}");
			Rotation = Vector3.Zero;
		}
		if (CanMove && !ClickAndPointMode) {
			inputMouseSmooth = inputMouseSmooth.Lerp(inputMouse, 14f * (float)delta);
			ApplyMovement(delta);
			inputMouse = Vector2.Zero;
		} else {
			FootstepManager.SetWalking(0f);
			FootstepManager.SetCameraMoving(0f);
		}
	}

	public override void _Process(double delta) {
		// Applying the fear value
		// TODO: Make it zoom on each individual fear heartbeat
		const float maxVolumeDb = 0f;
		float fearVolume = Mathf.Remap(Fear, 0f, 1f, -80f, maxVolumeDb);
		FearAudioPlayer.VolumeDb = Mathf.Lerp(FearAudioPlayer.VolumeDb, fearVolume, 4f * (float)delta);
		heartbeatZoom = Mathf.Lerp(heartbeatZoom, (Fear * 5f) * (float) GD.RandRange(0f, 1.2f), 12f * (float)delta);

		// Zoom
		Camera.Fov = Mathf.Lerp(Camera.Fov, (initialFov - heartbeatZoom) - ((Zoom - 1.0f) * 5f), 1.0f * (float) delta);
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventMouseMotion motion) {
			inputMouse = (motion.Relative * 0.002f);
		} else if (Input.IsActionJustPressed("escape")) {
			Input.MouseMode = (Input.MouseMode != Input.MouseModeEnum.Captured)
				? Input.MouseModeEnum.Captured
				: Input.MouseModeEnum.Visible;
		}
	}

	public void ApplyMovement(double delta) {
		// Gravity
		if (!IsOnFloor())
			Velocity = Velocity.WithY(Velocity.Y - (gravity * delta));

		// Input
		var inputDir = Input.GetVector("walk_left", "walk_right", "walk_forward", "walk_backward");
		var direction = (BodyPivot.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		// Camera
		var mouseInput = inputMouseSmooth * MouseSensitivity;
		BodyPivot.RotateY(-mouseInput.X);
		Head.RotateX(-mouseInput.Y);
		var rot = Head.Rotation;
		rot.X = Mathf.Clamp(Head.Rotation.X, -Mathf.Pi / 3, Mathf.Pi / 3);
		Head.Rotation = rot;

		// Sprinting
		if (Input.IsActionJustPressed("sprint") && !inputDir.IsZeroApprox() && !Crouching) {
			Sprinting = true;
			speed = Speed * 1.5f;
		} else if (Input.IsActionJustReleased("sprint")) {
			Sprinting = false;
			speed = Speed;
		}
		if (Sprinting) {
			Camera.Fov = Mathf.Lerp(initialFov, Camera.Fov + 15f, 8f * (float)delta);
		}

		// Crouching
		if (Input.IsActionJustPressed("crouch") && !Sprinting) {
			Crouching = true;
			speed = Speed * 0.5f;

			float crouchHeight = initialCollisionHeight * CrouchHeightModifier;
			(Collision.Shape as CapsuleShape3D)!.Height = crouchHeight;
			Collision.Position = Vector3.Up * (crouchHeight / 2);
		} else if (Input.IsActionJustReleased("crouch")) {
			Crouching = false;
			speed = Speed;

			Collision.Position = Vector3.Up * (initialCollisionHeight / 2);
			(Collision.Shape as CapsuleShape3D)!.Height = initialCollisionHeight;
		}

		// Overall view animation modifiers
		{
			// Head position
			Vector3 headPosMod = Crouching
				? initialHeadPosLocal * CrouchHeightModifier
				: initialHeadPosLocal;
			Head.Position = Head.Position.Lerp(headPosMod, 8f * (float)delta);

			// Camera FoV
			float fovMod = Sprinting
				? 15f
				: (Crouching ? -15f : 0f);
			Camera.Fov = Mathf.Lerp(initialFov, Camera.Fov + fovMod, 8f * (float)delta);
		}

		// Movement
		Velocity = Velocity.WithX(Mathf.Lerp(Velocity.X, direction.X * speed, Acceleration * delta));
		Velocity = Velocity.WithZ(Mathf.Lerp(Velocity.Z, direction.Z * speed, Acceleration * delta));
		MoveAndSlide();

		// Animation
		{
			bool walking = (!inputDir.IsZeroApprox()) && !Sprinting;
			bool idle = (!walking && !Sprinting);
			ModelAnimTree.Set("parameters/Movement/conditions/idle", idle);
			ModelAnimTree.Set("parameters/Movement/conditions/walking", walking);
			ModelAnimTree.Set("parameters/Movement/conditions/sprinting", Sprinting);
		}

		// Footstep manager
		float footstepSpeed = IsOnFloor()
			? (Velocity / (direction * speed).Max(Vector3.One)).Length()
			: 0f;
		FootstepManager.SetCameraMoving(inputMouseSmooth.Length());
		FootstepManager.SetWalking(footstepSpeed, Sprinting, Crouching);
	}

	// TODO: Switch all things that use this over to use the Click & Point player controller instead
	public void ProcessClickAndPointInteraction() {
		var spaceState = GetWorld3D().DirectSpaceState;
		var mousePos = GetViewport().GetMousePosition();
		var camera = GetViewport().GetCamera3D();

		var rayOrigin = camera.ProjectRayOrigin(mousePos);
		var rayEnd = rayOrigin + camera.ProjectRayNormal(mousePos) * camera.Far;

		var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		query.CollideWithAreas = true;
		if (spaceState.IntersectRay(query) is {} dict && dict.Count != 0) {
			#region Error handling
			if (!dict.TryGetValue("collider", out Variant vCollider) || vCollider.Obj is not Node3D collider) {
				GD.PushError($"Failed to get the collider for interactable ({vCollider.Obj})");
				return;
			}
			if (!dict.TryGetValue("position", out Variant vPosition) || vPosition.Obj is not Vector3 position) {
				GD.PushError($"Failed to get the position for interactable '{collider.Name}'");
				return;
			}
			if (!dict.TryGetValue("normal", out Variant vNormal) || vNormal.Obj is not Vector3 normal) {
				GD.PushError($"Failed to get the normal for interactable '{collider.Name}'");
				return;
			}
			#endregion
			var raycastData = new RaycastData(collider, position, normal);

			// Relaying interaction from children to the parent
			if (collider.GetChildCount() > 0 && collider.GetChildOrNull<PlayerInteractionRelay>(0) is { } relay) {
				collider = relay.Target;
			}

			// Hover detection
			if (collider is IPlayerHoverable hoverable) {
				if (currentlyHovered != hoverable) {  // Changing the hovered object
					currentlyHovered?.HoverEnd(this);
					hoverable.HoverBegin(this, raycastData);
					currentlyHovered = hoverable;
				}

				if (Input.IsActionJustPressed("flashlight"))
					currentlyHovered?.HoverEnd(this);
				else if (Input.IsActionJustReleased("flashlight"))
					currentlyHovered?.HoverBegin(this, raycastData);

				hoverable.HoverHold(this, raycastData);
			}
			if (currentlyHovered != collider) { // Removing the hover (for unrelated StaticBody3Ds and whatnot)
				currentlyHovered?.HoverEnd(this);
				currentlyHovered = null;
			}

			// Interact detection
			var state = Option<InteractState>.None();
			if (Input.IsActionJustPressed("interact"))
				state = Option<InteractState>.Some(InteractState.Press);
			else if (Input.IsActionJustReleased("interact"))
				state = Option<InteractState>.Some(InteractState.Release);
			if (state.LetSome(out var stateOut)) {
				switch (collider) {
					case IPlayerInteractable interactable: {
						interactable.Interact(this, raycastData, stateOut);
						break;
					}
					case Area3D or StaticBody3D:
						currentlyHovered?.HoverEnd(this);
						currentlyHovered = null;
						//GD.PushWarning(
						//	$"Tried interacting with '{collider.Name}' but it doesn't implement an interactable interface.\nThis could be by design, but if it isn't then put a {nameof(PlayerInteractionRelay)} under it to relay it to its parent");
						break;
				}
			}
		} else if (currentlyHovered != null) {
			currentlyHovered.HoverEnd(this);
			currentlyHovered = null;
		}
	}

	public void Process3dInteraction() {
		if (InteractRay.GetCollider() is Node3D collider) {
			var raycastData = new RaycastData(collider, Vector3.Zero, Vector3.Zero);
			
			// Hover
			if (collider is IPlayerHoverable hoverable) {
				if (currentlyHovered != hoverable) {
					currentlyHovered?.HoverEnd(this);
					currentlyHovered = hoverable;
					currentlyHovered.HoverBegin(this, raycastData);
				} else {
					currentlyHovered?.HoverHold(this, raycastData);
				}
			} else {
				currentlyHovered?.HoverEnd(this);
				currentlyHovered = null;
			}
			
			// Interaction
			if (collider is IPlayerInteractable interactable) {
				if (Input.IsActionJustPressed("interact") && CanMove) {
					interactable.Interact(this, raycastData, InteractState.Press);
					currentlyInteracted = interactable;
				}
			}

			// Releasing interaction
			if (Input.IsActionJustReleased("interact")) {
				currentlyInteracted?.Interact(this, raycastData, InteractState.Release);
				currentlyInteracted = null;
			}
		} else {
			currentlyHovered?.HoverEnd(this);
			currentlyHovered = null;
			if (currentlyInteracted != null) {
				currentlyInteracted?.Interact(this, null, InteractState.Release);
				currentlyInteracted = null;
			}
		}
	}

	// Usually used for UI
	public void UnlockMouse() {
		Input.MouseMode = Input.MouseModeEnum.Visible;
		CanMove = false;
	}
	public void LockMouse() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		CanMove = true;
	}

	public void SetZoom(float value) {
		var tween = CreateTween();
		tween.TweenProperty(Head, Node3D.PropertyName.Position.ToString(), initialHeadPosLocal + (Head.Transform.Basis * new Vector3(0f, 0f, (value - 1.0f) * -0.3f)), 0.5f);
		tween.TweenCallback(Callable.From(() => tween.Kill()));
	}
}
