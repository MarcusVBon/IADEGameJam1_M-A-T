using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _3dgame;

/// <summary>
/// Builds axis-aligned collision boxes from the real vertices of an imported FBX model.
///
/// Replaces GameObject.CalculateBoundingBox(), which derives a box from the bounding SPHERE and then
/// shifts it with hand-tuned constants (-15 / +35 / -35) that only suit house.fbx.
///
/// Two ways to get collision, chosen automatically per model:
///   1. The FBX contains objects whose name starts with "col_"  -> one box per such object, and those
///      objects are NOT drawn. Use several boxes for L-shaped buildings, doorways, fences, etc.
///   2. No "col_" objects -> one tight box around the whole model.
///
/// The boxes are computed from the vertices AFTER the .mgcb Scale/Rotation settings were applied
/// (ModelProcessor bakes those into the vertices), so they follow the model automatically.
///
/// Like Game1.DrawTextures, this ignores the FBX node (bone) transforms, so the boxes line up with
/// what is drawn when you draw with  effect.World = world.
/// </summary>
public static class ModelCollision
{
    public const string ColliderPrefix = "col_";

    /// <summary>True for meshes that exist only to provide collision. Skip these when drawing.</summary>
    public static bool IsCollider(ModelMesh mesh)
    {
        return mesh.Name != null && mesh.Name.StartsWith(ColliderPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Collision boxes for a model placed in the world with the given matrix.</summary>
    /// <param name="perMesh">With no "col_" objects, give every mesh its own box instead of one merged box.
    /// Use for models built from box-shaped pieces you can walk between (walls, corridors).</param>
    public static List<BoundingBox> Build(Model model, Matrix world, bool perMesh = false)
    {
        var colliders = new List<BoundingBox>();
        var everything = new List<BoundingBox>();

        foreach (ModelMesh mesh in model.Meshes)
        {
            if (mesh.MeshParts.Count == 0)
                continue;

            BoundingBox box = GetMeshBounds(mesh, world);
            if (IsCollider(mesh)) colliders.Add(box);
            else everything.Add(box);
        }

        if (colliders.Count > 0)
            return colliders;
        if (perMesh)
            return everything;

        var result = new List<BoundingBox>();
        if (everything.Count > 0)
        {
            BoundingBox merged = everything[0];
            for (int i = 1; i < everything.Count; i++)
                merged = BoundingBox.CreateMerged(merged, everything[i]);
            result.Add(merged);
        }
        return result;
    }

    /// <summary>Tight box around the actual vertices of one mesh, transformed by <paramref name="world"/>.</summary>
    public static BoundingBox GetMeshBounds(ModelMesh mesh, Matrix world)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (ModelMeshPart part in mesh.MeshParts)
        {
            if (part.VertexBuffer == null || part.NumVertices == 0)
                continue;

            VertexDeclaration declaration = part.VertexBuffer.VertexDeclaration;
            int stride = declaration.VertexStride;

            int positionOffset = -1;
            foreach (VertexElement element in declaration.GetVertexElements())
            {
                if (element.VertexElementUsage == VertexElementUsage.Position && element.UsageIndex == 0)
                {
                    if (element.VertexElementFormat != VertexElementFormat.Vector3)
                        throw new NotSupportedException("Model positions are not stored as Vector3: " + element.VertexElementFormat);
                    positionOffset = element.Offset;
                    break;
                }
            }
            if (positionOffset < 0)
                throw new InvalidOperationException("Mesh '" + mesh.Name + "' has no position data.");

            // Read this part's vertices as raw bytes (stride 1 = plain copy), then pull the positions out.
            // This works for any vertex layout, not just VertexPositionNormalTexture.
            var bytes = new byte[part.NumVertices * stride];
            part.VertexBuffer.GetData(part.VertexOffset * stride, bytes, 0, bytes.Length, 1);

            for (int i = 0; i < part.NumVertices; i++)
            {
                int at = i * stride + positionOffset;
                var local = new Vector3(
                    BitConverter.ToSingle(bytes, at),
                    BitConverter.ToSingle(bytes, at + 4),
                    BitConverter.ToSingle(bytes, at + 8));

                Vector3 p = Vector3.Transform(local, world);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        return new BoundingBox(min, max);
    }
}
