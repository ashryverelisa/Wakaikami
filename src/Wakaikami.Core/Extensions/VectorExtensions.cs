using System.Numerics;

namespace Wakaikami.Core.Extensions;

public static class VectorExtensions
{
    extension(Vector2 position)
    {
        public double GetDistance2D(Vector2 otherPosition) => Vector2.Distance(position, otherPosition);

        public bool IsInCircle(Vector2 otherPosition, float circleRadius)
        {
            var delta = otherPosition - position;
            return delta.LengthSquared() <= circleRadius * circleRadius;
        }
    }

    public static float FiestaAngle(Vector2 from, Vector2 to)
    {
        var angle = Math.Atan2(-(to.Y - from.Y), to.X - from.X) * (180.0 / Math.PI) + 90.0;

        if (angle < 0.0)
            angle += 360.0;

        return (float)angle;
    }

    public static byte DirectionToByte(float direction) => Convert.ToByte(direction / 2.0);
}
