using Microsoft.Xna.Framework;

namespace _3dgame; 

public class Camera {
    public Vector3 AvatarHeadOffset { get; set; }
    public Vector3 TargetOffset { get; set; }
    public Matrix ViewMatrix { get; set; }
    public Matrix ProjectionMatrix { get; set; }

    public Camera() {
        //CAMERA OOFSET HERE
        AvatarHeadOffset = new Vector3(0, 7, 0);
        TargetOffset = new Vector3(1, 5, 0);
        ViewMatrix = Matrix.Identity;
        ProjectionMatrix = Matrix.Identity;
    }

    public void Update(float cameraYaw, float cameraPitch, Vector3 position, float aspectRatio) {
        Matrix rotation = Matrix.CreateRotationX(cameraPitch) * Matrix.CreateRotationY(cameraYaw);

        Vector3 cameraPosition = position + AvatarHeadOffset;

        Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);

        Vector3 cameraTarget = cameraPosition + forward;
        
        ViewMatrix = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(GameConstants.ViewAngle),
            aspectRatio, GameConstants.NearClip, GameConstants.FarClip);
    }
}