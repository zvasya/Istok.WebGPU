using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Istok.Mathematics;

public class Frustum(in Matrix4x4 matrix)
{
    public Plane LeftPlane = Plane.Normalize(new Plane(
        matrix.M14 + matrix.M11,
        matrix.M24 + matrix.M21,
        matrix.M34 + matrix.M31,
        matrix.M44 + matrix.M41));

    public Plane RightPlane = Plane.Normalize(new Plane(
        matrix.M14 - matrix.M11,
        matrix.M24 - matrix.M21,
        matrix.M34 - matrix.M31,
        matrix.M44 - matrix.M41));

    public Plane TopPlane = Plane.Normalize(new Plane(
        matrix.M14 - matrix.M12,
        matrix.M24 - matrix.M22,
        matrix.M34 - matrix.M32,
        matrix.M44 - matrix.M42));

    public Plane BottomPlane = Plane.Normalize(new Plane(
        matrix.M14 + matrix.M12,
        matrix.M24 + matrix.M22,
        matrix.M34 + matrix.M32,
        matrix.M44 + matrix.M42));

    public Plane NearPlane = Plane.Normalize(new Plane(
        matrix.M13,
        matrix.M23,
        matrix.M33,
        matrix.M43
    ));

    public Plane FarPlane = Plane.Normalize(new Plane(
        matrix.M14 - matrix.M13,
        matrix.M24 - matrix.M23,
        matrix.M34 - matrix.M33,
        matrix.M44 - matrix.M43
    ));

    public bool Intersect(in BoundingBox boundingBoxExt)
    {
        ref Plane plane = ref Unsafe.AsRef(in LeftPlane);
        for (int i = 0; i < 6; ++i)
        {
            var absNormal = Vector128.Abs(plane.Normal.AsVector128()).AsVector3();
            if (Vector3.Dot(boundingBoxExt.Center, plane.Normal) + Vector3.Dot(boundingBoxExt.Extent, absNormal) <= -plane.D)
                return false;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return true;
    }
    
    public bool Contains(in BoundingBox boundingBoxExt)
    {
        ref Plane plane = ref Unsafe.AsRef(in LeftPlane);
        for (int i = 0; i < 6; ++i)
        {
            // var absNormal = Vector128.Abs(plane.Normal.AsVector128()).AsVector3();
            var absNormal = new Vector3(Math.Abs(plane.Normal.X), Math.Abs(plane.Normal.Y), Math.Abs(plane.Normal.Z));
            if (Vector3.Dot(boundingBoxExt.Center, plane.Normal) - Vector3.Dot(boundingBoxExt.Extent, absNormal) <= -plane.D)
                return false;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return true;
    }
}