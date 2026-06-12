using System.Numerics;
using System.Runtime.CompilerServices;

namespace Istok.Mathematics;

public static class MathExt
{
    public const double DegToRad = Math.PI / 180.0;
    public const double RadToDeg = 180.0 / Math.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Radians(float deg)
    {
        return (float)(deg * DegToRad);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Radians(double deg)
    {
        return deg * DegToRad;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Degrees(float rad)
    {
        return (float)(rad * RadToDeg);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Degrees(double rad)
    {
        return rad * RadToDeg;
    }
    
    public static float InverseLerp(float a, float b, float value)
    {
        return InverseLerp((double)a, b, value);
    }
    
    public static float InverseLerp(double a, double b, double value)
    {
        return Math.Abs(a - b) > float.Epsilon ? float.Clamp(((float) ((value - a) / (b - a))),0,1) : 0.0f;
    }
    
    public static float Angle(Vector2 from, Vector2 to)
    {
        from = Vector2.Normalize(from);
        to = Vector2.Normalize(to);
    
        double ratio = Vector2.Dot(from, to);
    
        double theta;
            
        if (ratio < 0)
        {
            theta = Math.PI - 2.0 * Math.Asin((-from - to).Length() / 2.0);
        }
        else
        {
            theta = 2.0 * Math.Asin((from - to).Length() / 2.0);
        }
    
        return (float)Degrees(theta);
    }
    
    public static float SignedAngle(Vector2 from, Vector2 to)
    {
       return Angle(from, to) * Math.Sign(from.X * to.Y - from.Y * to.X);
    }
    
    public static (float startValue, float endValue, float startTangent, float endTangent) GetHermiteCoefficients(double amount)
    {
        if (amount <= 0.0)
            return (1, 0, 0, 0);

        if (amount >= 1.0)
            return (0,1,0,0);

        double squared = amount * amount;
        double cubed = squared * amount;

        // https://en.wikipedia.org/wiki/Cubic_Hermite_spline#Unit_interval_[0,_1]
        double endTangent = cubed -  squared;                  // t^3 - t^2
        double startTangent = endTangent - squared + amount;   // t^3 - 2*t^2 + t
        double endValue = squared - 2.0 * endTangent;          // - 2*t^3 + 3*t^2
        double startValue = 1.0 - endValue;                    // 2*t^3 - 3*t^2 + 1

        return ((float)startValue, (float)endValue, (float)startTangent, (float)endTangent);
    }
    
    public static Vector2 Perpendicular(Vector2 vector) => new Vector2(-vector.Y, vector.X);
}
