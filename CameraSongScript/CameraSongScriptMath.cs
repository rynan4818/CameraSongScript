using UnityEngine;

namespace CameraSongScript
{
    internal static class CameraSongScriptMath
    {
        internal static void FindShortestDelta(ref Vector3 from, ref Vector3 to)
        {
            if (Mathf.DeltaAngle(from.x, to.x) < 0f)
                from.x += 360f;
            if (Mathf.DeltaAngle(from.y, to.y) < 0f)
                from.y += 360f;
            if (Mathf.DeltaAngle(from.z, to.z) < 0f)
                from.z += 360f;
        }

        internal static Vector3 LerpVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(
                Mathf.Lerp(from.x, to.x, percent),
                Mathf.Lerp(from.y, to.y, percent),
                Mathf.Lerp(from.z, to.z, percent));
        }

        internal static Vector3 LerpVector3Angle(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(
                Mathf.LerpAngle(from.x, to.x, percent),
                Mathf.LerpAngle(from.y, to.y, percent),
                Mathf.LerpAngle(from.z, to.z, percent));
        }

        internal static float Ease(float p, bool useEase)
        {
            if (!useEase)
                return p;

            if (p < 0.5f)
                return 4f * p * p * p;

            float f = (2f * p) - 2f;
            return 0.5f * f * f * f + 1f;
        }

        internal static Vector3 ApplyHeightOffset(Vector3 position, int offsetCm)
        {
            if (offsetCm != 0)
                position.y += offsetCm / 100f;

            return position;
        }
    }
}
