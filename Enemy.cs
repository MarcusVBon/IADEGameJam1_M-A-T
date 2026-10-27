using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _3dgame;

public class Enemy
{
    public Vector3 Position;                 // position of the enemy's FEET
    private  Texture2D texture;
    private  AlphaTestEffect _effect;
    private  VertexPositionTexture[] quad;
    private  float Width;
    private  float height;
    
    public Enemy(GraphicsDevice device, Texture2D nTexture, Vector3 position, float nWidth, float nHeight = 2f)
    {
        texture = nTexture;
        Position = position;
        height = nHeight;
        Width = nWidth;

        // AlphaTestEffect = hard cut-out transparency (pixels below the threshold are discarded).
        // Needs no draw sorting and writes depth correctly. Ideal for sprites.
        _effect = new AlphaTestEffect(device)
        {
            AlphaFunction = CompareFunction.Greater,
            ReferenceAlpha = 128
        };

        // Quad in LOCAL space: bottom-centre at the origin, lying in the XY plane, facing +Z.
        float hw = Width / 2f;
        var bl = new VertexPositionTexture(new Vector3(-hw, 0, 0), new Vector2(0, 1));
        var br = new VertexPositionTexture(new Vector3(hw, 0, 0), new Vector2(1, 1));
        var tr = new VertexPositionTexture(new Vector3(hw, height, 0), new Vector2(1, 0));
        var tl = new VertexPositionTexture(new Vector3(-hw, height, 0), new Vector2(0, 0));
        quad = new[] { bl, br, tr, bl, tr, tl };
    }

    /// <summary>Radius used for blocking the player and (roughly) for hit detection.</summary>
    public float Radius => Width * 0.4f;

    public void Update(float dt)
    {
        
    }

    /// <summary>Ray vs. an axis-aligned box around the enemy. Returns distance along the ray, or null on a miss.</summary>
    public float? Intersect(Ray ray)
    {
        float r = Radius;
        var box = new BoundingBox(
            new Vector3(Position.X - r, Position.Y, Position.Z - r),
            new Vector3(Position.X + r, Position.Y + height, Position.Z + r));

        return ray.Intersects(box);
    }

    /// <summary>Stops a circle (the player) from walking through this enemy.</summary>
    public void PushOut(ref Vector3 feetPosition, float otherRadius)
    {

        float dx = feetPosition.X - Position.X;
        float dz = feetPosition.Z - Position.Z;
        float minDist = Radius + otherRadius;
        float distSq = dx * dx + dz * dz;
        if (distSq >= minDist * minDist) return;

        float dist = MathF.Sqrt(distSq);
        if (dist < 0.0001f) { dx = 1f; dz = 0f; dist = 1f; }

        feetPosition.X = Position.X + dx / dist * minDist;
        feetPosition.Z = Position.Z + dz / dist * minDist;
    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection, Vector3 cameraPosition)
    {

        // Direction from the enemy to the camera, flattened onto the ground plane.
        float dx = cameraPosition.X - Position.X;
        float dz = cameraPosition.Z - Position.Z;

        // Local +Z (the quad's front) must point at (dx, dz). Rotating +Z by yaw around Y gives
        // (sin(yaw), cos(yaw)), so yaw = atan2(dx, dz). Only this one axis is ever rotated.
        float yaw = MathF.Atan2(dx, dz);

        _effect.World = Matrix.CreateRotationY(yaw) * Matrix.CreateTranslation(Position);
        _effect.View = view;
        _effect.Projection = projection;
        _effect.Texture = texture;

        // PointClamp = crisp pixel art. Use SamplerState.LinearClamp for smooth/hi-res sprites.
        device.SamplerStates[0] = SamplerState.PointClamp;

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
        }
    }
}
