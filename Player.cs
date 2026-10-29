using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace _3dgame {
    public class Player : GameObject {

        public float ForwardDirection { get; set; }
        private bool jumpStarted = false;
        private float jumpStartY;
        private float movementDirection = 0;
        private Vector3 currentVelocity ;
        private bool reachedApex = false;
        private bool reachedBottom = true;
        private Vector3 startPosition = new Vector3(30, GameConstants.HeightOffset, 125);
        public readonly double maxHealthTimer = 20 ;
        public readonly double crunchTime = 0.75 ;
        public double handCrunchTimer = -1 ;
        public readonly double healthRegen = 5 ;
        public double healthTimer = 15;
        public int handState = 0 ;
        public SoundEffectInstance SpineCrushSound { get; set; }
        public SoundEffectInstance LiquidsSound {get; set; }
        public SoundEffectInstance PlayerDeathSound {get; set; }
        public int score = 0;

        public bool isPlayerDying = false;

        public bool playerDead = false ;

        public Player() : base() {
            ForwardDirection = 0.0f;
            Position = startPosition;
            healthTimer = maxHealthTimer ;
            handCrunchTimer = -1 ;
            handState = 0 ;
            playerDead = false ;
        }

        public void switchHandState(int newState)
        {
            if(handState == 4)
            {
                handCrunchTimer = -1 ;
                handState = newState ;
                healthTimer += healthRegen ;
                if (maxHealthTimer < healthTimer)
                {
                    healthTimer =  maxHealthTimer ;
                }
            }

            if(newState == 1)
            {
                LiquidsSound.Play();
                score += 5;
            }

            if(newState == 2)
            {
                SpineCrushSound.Play();
                score += 5;
            }

            if (newState == 1 && handState == 2 || newState == 2 && handState == 1)
            {
                handState = 4 ;
                handCrunchTimer = crunchTime ;
            } else
            {
                handState = newState ;
            }
        }

        public void Update(double dt, KeyboardState keyboardState, float cameraYaw, IReadOnlyList<BoundingBox> colliders) {
            healthTimer = healthTimer - dt ;
            //Console.WriteLine("HandState: " + handState) ;
            if (handCrunchTimer != -1)
            {
                Console.WriteLine("CRUNCH TIME: " + handCrunchTimer) ;
                handCrunchTimer = handCrunchTimer - dt ;
                if (handCrunchTimer < 0)
                {
                    handCrunchTimer = -1 ;
                    handState = 0 ;
                    healthTimer += healthRegen ;
                    if (maxHealthTimer < healthTimer)
                    {
                        healthTimer =  maxHealthTimer ;
                    }
                }

            }

            if (healthTimer < 0)
            {
                if(handState == 4)
                {
                    handCrunchTimer = -1 ;
                    handState = 0 ;
                    healthTimer += healthRegen ;
                    if (maxHealthTimer < healthTimer)
                    {
                        healthTimer =  maxHealthTimer ;
                    }
                } else
                {
                    healthTimer = 0 ;
                    playerDead = true ;
                }

                if(isPlayerDying == false)
                {
                    PlayerDeathSound.Play();
                    isPlayerDying = true;
                }
                healthTimer = 0 ;
                Position = new Vector3 (900, 0, 900) ;
            }

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

            if (horizontal != Vector2.Zero) {
                horizontal.Normalize();
                currentVelocity.X += GameConstants.acceleration * movement.X;
                currentVelocity.Z += GameConstants.acceleration * movement.X;
            } else
            {
                currentVelocity.X -= GameConstants.acceleration * movement.X;
                currentVelocity.Z-= GameConstants.acceleration * movement.X;
            }

            if (currentVelocity.X * movement.X > GameConstants.maxVelocity * movement.X )
            {
                currentVelocity.X = movement.X * GameConstants.maxVelocity ;
            }
            if (currentVelocity.Z * movement.Z > GameConstants.maxVelocity * movement.Z )
            {
                currentVelocity.Z = movement.Z * GameConstants.maxVelocity ;
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