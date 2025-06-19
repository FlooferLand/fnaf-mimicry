#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Project.Components;

namespace Project;
using Godot;

public partial class MimicViewCone : Area3D {
    [Signal] public delegate void FoundTargetEventHandler(FirstPersonCharacter target);
    [Signal] public delegate void LostTargetEventHandler();

    [ExportGroup("Nodes")]
    [Export] public required MimicNavComponent NavComponent;
    [Export] public required CollisionShape3D ParentCollisionShape;
    [Export] public required RayCast3D ViewRayCast;
    [Export] public required MeshInstance3D EditorConeMesh;
    [Export] public required CollisionShape3D ConeCollision;

    public FirstPersonCharacter? permanentPlayer = null;
    public FirstPersonCharacter? Target = null;
    Dictionary<ulong, Node3D> bodiesInCone = new();

    bool wasPlayerSeen = false;

    public override void _EnterTree() {
        // Generating collision
        ConeCollision.Shape = EditorConeMesh.Mesh.CreateTrimeshShape();
        ConeCollision.Transform = EditorConeMesh.Transform;
        if (!EditorConeMesh.Visible) EditorConeMesh.QueueFree();

        // Checking view cone
        BodyShapeEntered += (rid, body, index, shapeIndex) => {
            bodiesInCone.Add(body.GetInstanceId(), body);
            if (body is FirstPersonCharacter player) {
                permanentPlayer = player;
            }
        };
        BodyShapeExited += (rid, body, index, shapeIndex) => {
            bodiesInCone.Remove(body.GetInstanceId());
        };
    }

    public override void _PhysicsProcess(double delta) {
        var player = Target ?? permanentPlayer;
        if (player == null) return;

        if (IsTargetInSight(player) && !wasPlayerSeen) {
            Target = player;
            NavComponent.Target = player;
            wasPlayerSeen = true;
            EmitSignalFoundTarget(player);
        }

        if (!IsTargetInSight(player, true) && wasPlayerSeen) {
            Target = null;
            EmitSignalLostTarget();
            wasPlayerSeen = false;
        }
    }

    // TODO: Track the player better!!
    //       Stop losing track of them constantly
    public bool IsTargetInSight(FirstPersonCharacter player, bool checkLess = false) {
        if (!checkLess) {
            if (!bodiesInCone.ContainsKey(player.GetInstanceId()))
                return false;
        }

        ViewRayCast.TargetPosition = ViewRayCast.ToLocal(player.Head.GlobalPosition);
        ViewRayCast.ForceRaycastUpdate();
        bool seesPlayer = (ViewRayCast.IsColliding() && ViewRayCast.GetCollider() is FirstPersonCharacter);
        return seesPlayer;
    }
}
