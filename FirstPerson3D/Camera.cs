using Microsoft.Xna.Framework;
using System;

namespace FirstPersonGameExample
{
    class Camera : GameComponent
    {
        private Vector3 cameraLookAt;
        public Vector3 Position { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Matrix Projection { get; protected set; }
        public Matrix View { get { return Matrix.CreateLookAt(Position, cameraLookAt, Vector3.Up); } }

        public Camera(Game game) : base(game)
        {
            this.cameraLookAt = Vector3.Zero;
            this.Position = Vector3.Zero;
            this.Rotation = Vector3.Zero;
            this.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, Game.GraphicsDevice.Viewport.AspectRatio,
                0.05f, 1000.0f);
        }

        private void UpdateLookAt()
        {
            Matrix rotationMatrix = Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y);
            Vector3 lookAtOffset = Vector3.Transform(Vector3.UnitZ, rotationMatrix);

            cameraLookAt = Position + lookAtOffset;
        }

        public Vector3 PreviewMove(Vector3 movement)
        {
            Matrix rotate = Matrix.CreateRotationY(Rotation.Y);

            movement = Vector3.Transform(movement, rotate);

            return Position + movement;
        }

        public void Move(Vector3 position)
        {
            Position = position;

            UpdateLookAt();
        }

        public void SetPosition(Vector3 position)
        {
            Vector3 cameraView = PreviewMove(position);

            Move(cameraView);
        }

        public void SetRotation(Vector3 rotation)
        {
            this.Rotation = rotation;

            UpdateLookAt();
        }

        /*public void rotate(double rSpeed)
        {
			//both camera direction and camera plane must be rotated
			double oldDirX = dir.X;
			dir.X = (float)(dir.X * Math.Cos(rSpeed) - dir.Y * Math.Sin(rSpeed));
			dir.Y = (float)(oldDirX * Math.Sin(rSpeed) + dir.Y * Math.Cos(rSpeed));
			double oldPlaneX = plane.X;
			plane.X = (float)(plane.X * Math.Cos(rSpeed) - plane.Y * Math.Sin(rSpeed));
			plane.Y = (float)(oldPlaneX * Math.Sin(rSpeed) + plane.Y * Math.Cos(rSpeed));
        } */
    }
}