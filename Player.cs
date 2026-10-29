using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace _3dgame {
    public class Player : GameObject {
        public float ForwardDirection { get; set; }
        private bool jumpStarted = false;
        private float jumpStartY;
        private float movementDirection = 0;
        private bool reachedApex = false;
        private bool reachedBottom = true;
        private Vector3 startPosition = new Vector3(0, GameConstants.HeightOffset, 0);
        public Player() : base() {
            ForwardDirection = 0.0f;
            Position = startPosition;
        }

        public void ResetToStart() {
            Position = startPosition;
            jumpStarted = false;
            reachedApex = false;
            reachedBottom = true;
            movementDirection = 0;
            BoundingSphere = new BoundingSphere(Position, 1f);
        }

        public void Update(KeyboardState keyboardState, float cameraYaw, IReadOnlyList<BoundingBox> colliders) {
            Vector3 futurePosition = Position;
            Vector3 movement = Vector3.Zero;
            
            BoundingSphere = new BoundingSphere(Position, 1f);

            if (keyboardState.IsKeyDown(Keys.A)) {
                movement.X = -1;
            }
            else if (keyboardState.IsKeyDown(Keys.D)) {
                movement.X = 1;
            }
            if (keyboardState.IsKeyDown(Keys.W)) {
                movement.Z = -1;
            }
            else if (keyboardState.IsKeyDown(Keys.S)) {
                movement.Z = 1;
            }
            if (keyboardState.IsKeyDown(Keys.Space) && !jumpStarted) {
                jumpStarted = true;
                jumpStartY = Position.Y;
            }

            if (jumpStarted) {
                movement.Y = PlayerJump();
            }

            Vector2 horizontal = new Vector2(movement.X, movement.Z);

            if (horizontal != Vector2.Zero){
                horizontal.Normalize();
            }

            movement.X = horizontal.X;
            movement.Z = horizontal.Y;

            ForwardDirection = cameraYaw;
            Matrix orientationMatrix = Matrix.CreateRotationY(ForwardDirection);

            Vector3 speed = Vector3.Transform(movement, orientationMatrix);
            speed *= GameConstants.Velocity;

            futurePosition.Y += speed.Y;

            if (!jumpStarted && futurePosition.Y != jumpStartY) {
                futurePosition.Y = jumpStartY;
            }

            Vector3 futureX = Position + new Vector3(speed.X, 0, 0);
            if (ValidateMovement(futureX, colliders)) {
                futurePosition.X = futureX.X;
            }

            Vector3 futureZ = futurePosition + new Vector3(0, 0, speed.Z);
            if (ValidateMovement(futureZ, colliders)) {
                futurePosition.Z = futureZ.Z;
            }
            Position = futurePosition;
            BoundingSphere = new BoundingSphere(Position, 1f);
        }

        private bool ValidateMovement(Vector3 futurePosition, IReadOnlyList<BoundingBox> colliders) {
            // do not allow off-terrain movement
            if ((Math.Abs(futurePosition.X) > GameConstants.MaxRange) || (Math.Abs(futurePosition.Z) > GameConstants.MaxRange)) {
                return false;
            }

            // future bounding sphere
            BoundingSphere futureSphere = new BoundingSphere(futurePosition, BoundingSphere.Radius);

            // collision with every box (houses, walls, ...)
            foreach (BoundingBox box in colliders) {
                if (futureSphere.Intersects(box)) {
                    return false;
                }
            }

            return true;
        }

        private float PlayerJump() {
            Vector3 position = Position;
            if (reachedApex == false && reachedBottom == true) {
                movementDirection = 0.8f;
            }
            if (position.Y >= jumpStartY + GameConstants.JumpHeight && reachedApex == false) {
                reachedApex = true;
                reachedBottom = false;
            }
            if (reachedApex == true) {
                movementDirection = -0.9f;
            }
            if (position.Y <= jumpStartY && reachedBottom == false) {
                jumpStarted = false;
                reachedBottom = true;
                reachedApex = false;
                position.Y = jumpStartY;
                Position = position;
                movementDirection = 0;
            }
            return movementDirection;
        }
    }
}