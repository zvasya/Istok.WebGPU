using System.Numerics;

namespace Istok.Mathematics;

public struct Rect : IEquatable<Rect>
{
    /// <summary>
    /// The origin.
    /// </summary>
    public Vector2 Origin;

    /// <summary>
    /// The size.
    /// </summary>
    public Vector2 Size;

    /// <summary>
    /// Constructs a Rectangle from an origin and a size
    /// </summary>
    /// <param name="origin">The origin of the rect.</param>
    /// <param name="size">The size of the rect.</param>
    public Rect(Vector2 origin, Vector2 size)
    {
        Origin = origin;
        Size = size;
    }

    /// <summary>
    /// Constructs a Rectangle from an origin and components of a size
    /// </summary>
    /// <param name="origin">The origin of the rect.</param>
    /// <param name="sizeX">The X component of the size of the rect.</param>
    /// <param name="sizeY">The Y component of the size of the rect.</param>
    public Rect(Vector2 origin, float sizeX, float sizeY)
        : this(origin, new Vector2(sizeX, sizeY))
    {
    }

    /// <summary>
    /// Constructs a Rectangle from components of an origin and a size
    /// </summary>
    /// <param name="originX">The X component of the origin of the rect.</param>
    /// <param name="originY">The Y component of the origin of the rect.</param>
    /// <param name="size">The size of the rect.</param>
    public Rect(float originX, float originY, Vector2 size)
        : this(new Vector2(originX, originY), size)
    {
    }

    /// <summary>
    /// Constructs a Rectangle from components of an origin and components of a size
    /// </summary>
    /// <param name="originX">The X component of the origin of the rect.</param>
    /// <param name="originY">The Y component of the origin of the rect.</param>
    /// <param name="sizeX">The X component of the size of the rect.</param>
    /// <param name="sizeY">The Y component of the size of the rect.</param>
    public Rect(float originX, float originY, float sizeX, float sizeY)
        : this(new Vector2(originX, originY), new Vector2(sizeX, sizeY))
    {
    }
    
    /// <summary>
    /// The X of this rectangle.
    /// </summary>
    public float X => Origin.X;
    /// <summary>
    /// The Y of this rectangle.
    /// </summary>
    public float Y => Origin.Y;
    
    /// <summary>
    /// Width of this rectangle.
    /// </summary>
    public float Width => Size.X;
    
    /// <summary>
    /// Height of this rectangle.
    /// </summary>
    public float Height => Size.Y;

    /// <summary>
    /// The center of this rectangle.
    /// </summary>
    public Vector2 Center => Origin + HalfSize;

    /// <summary>
    /// The Minimum point of this Rectangle.
    /// </summary>
    public Vector2 Min => Origin;
    
    /// <summary>
    /// The Maximum point of this Rectangle.
    /// </summary>
    public Vector2 Max => Origin + Size;

    /// <summary>
    /// Half the size of this rectangle.
    /// </summary>
    public Vector2 HalfSize => Size / 2.0f;

    /// <summary>
    /// Calculates whether this rectangle contains a point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>True if this rectangle contains the point; False otherwise.</returns>
    /// <remarks>This does consider a point on the edge contained.</remarks>
    public bool Contains(Vector2 point)
    {
        var max = Max;
        return point.X >= Origin.X 
               && point.Y >= Origin.Y
               && point.X <= max.X
               && point.Y <= max.Y;
    }

    /// <summary>
    /// Calculates whether this rectangle contains another rectangle
    /// </summary>
    /// <param name="other">The rectangle.</param>
    /// <returns>True if this rectangle contains the given rectangle; False otherwise.</returns>
    /// <remarks>This does consider a rectangle that touches the edge contained.</remarks>
    public bool Contains(Rect other)
    {
        var tMax = this.Max;
        var oMax = other.Max;
        return other.Origin.X >= this.Origin.X
               && other.Origin.Y >= this.Origin.Y
               && oMax.X <= tMax.X
               && oMax.Y <= tMax.Y;
    }

    /// <summary>
    /// Calculates the distance to the nearest edge from the point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>The distance.</returns>
    public float GetDistanceToNearestEdge(Vector2 point)
    {
        var max = Max;
        var dx = float.Max(float.Max((Origin.X - point.X), 0), (point.X - max.X));
        var dy = float.Max(float.Max((Origin.Y - point.Y), 0), (point.Y - max.Y));
        return float.Sqrt(((dx * dx) + (dy * dy)));
    }

    /// <summary>
    /// Calculates a new rectangle translated by a given distance.
    /// </summary>
    /// <param name="distance">The distance.</param>
    /// <returns>The calculated rectangle.</returns>
    public Rect GetTranslated(Vector2 distance)
    {
        return new(Origin + distance, Size);
    }

    /// <summary>
    /// Calculates a new rectangle scaled by the given scale around the given anchor.
    /// </summary>
    /// <param name="scale">The scale.</param>
    /// <param name="anchor">The anchor.</param>
    /// <returns>The calculated rectangle.</returns>
    public Rect GetScaled(Vector2 scale, Vector2 anchor)
    {
        var min = (scale * (Origin - anchor)) + anchor;
        var max = (scale * (Max - anchor)) + anchor;
        return new(min, max - min);
    }

    /// <summary>
    /// Calculates a rectangle inflated to contain the given point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>The calculated rectangle.</returns>
    public Rect GetInflated(Vector2 point)
    {
        var min = Vector2.Min(Origin, point);
        var max = Vector2.Max(Max, point);
        return new(min, max - min);
    }

    /// <summary>Returns a boolean indicating whether the given Rectangle is equal to this Rectangle instance.</summary>
    /// <param name="other">The Rectangle to compare this instance to.</param>
    /// <returns>True if the other Rectangle is equal to this instance; False otherwise.</returns>
    public bool Equals(Rect other)
    {
        return Origin.Equals(other.Origin) && Size.Equals(other.Size);
    }

    /// <summary>Returns a boolean indicating whether the given Object is equal to this Rectangle instance.</summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Rectangle; False otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Origin, Size);
    }

    /// <summary>Returns a boolean indicating whether the two given Rectangles are equal.</summary>
    /// <param name="value1">The first Rectangle to compare.</param>
    /// <param name="value2">The second Rectangle to compare.</param>
    /// <returns>True if the Rectangles are equal; False otherwise.</returns>
    public static bool operator ==(Rect value1, Rect value2)
    {
        return value1.Equals(value2);
    }

    /// <summary>Returns a boolean indicating whether the two given Rectangles are not equal.</summary>
    /// <param name="value1">The first Rectangle to compare.</param>
    /// <param name="value2">The second Rectangle to compare.</param>
    /// <returns>True if the Rectangles are not equal; False if they are equal.</returns>
    public static bool operator !=(Rect value1, Rect value2)
    {
        return !value1.Equals(value2);
    }
}