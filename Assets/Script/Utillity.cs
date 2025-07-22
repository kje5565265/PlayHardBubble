using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace PlayHard
{
    public static class DOTweenUniTaskExtensions
    {
        public static UniTask ToUniTask(this Tween tween)
        {
            var tcs = new UniTaskCompletionSource();
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetCanceled());

            return tcs.Task;
        }
    }
    
    public class Utillity : Singleton<Utillity>
    {
        public static Vector3 HexToWorld(float q, float r, float radius = 0.5f)
        {
            float width = radius * 2f;
            float height = Mathf.Sqrt(3f) * radius;

            float x = q * width * 0.5f;
            float y = r * height;

            return new Vector3(x, y, 0f);
        }
        
        public static (int q, int r) WorldToHex(Vector3 worldPos, float radius = 0.5f)
        {
            float width = radius * 2f;
            float height = Mathf.Sqrt(3f) * radius;

            float q = worldPos.x / (width * 0.5f);
            float r = worldPos.y / height;

            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);

            return (roundedQ, roundedR);
        }
        
        public static Vector3 GetNeighborPosition(float q, float r, Vector2 direction)
        {
            var hexDirs = new (Vector2 dir, float dq, float dr)[]
            {
                (new Vector2(1f, 0f), 1, 0),
                (new Vector2(0.5f, 0.866f), 0, 1),
                (new Vector2(-0.5f, 0.866f), -1, 1),
                (new Vector2(-1f, 0f), -1, 0),
                (new Vector2(-0.5f, -0.866f), 0, -1),
                (new Vector2(0.5f, -0.866f), 1, -1),
            };

            float maxDot = -Mathf.Infinity;
            (float dq, float dr) bestOffset = (0, 0);

            foreach (var (dir, dq, dr) in hexDirs)
            {
                float dot = Vector2.Dot(direction.normalized, dir.normalized);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestOffset = (dq, dr);
                }
            }

            float targetQ = q + bestOffset.dq;
            float targetR = r + bestOffset.dr;

            return Utillity.HexToWorld(targetQ, targetR, 0.5f);
        }

        public static (float q, float r) GetLandingQR(Bubble2D targetBubble, Vector2 impactDirection)
        {
            var hexDirs = new (Vector2 vec, float dq, float dr)[]
            {
                (new Vector2(1f, 0f), 1f, 0f),
                (new Vector2(0.5f, 0.866f), 0.5f, 0.5f),
                (new Vector2(-0.5f, 0.866f), -0.5f, 0.5f),
                (new Vector2(-1f, 0f), -1f, 0f),
                (new Vector2(-0.5f, -0.866f), -0.5f, -0.5f),
                (new Vector2(0.5f, -0.866f), 0.5f, -0.5f),
            };

            float maxDot = float.NegativeInfinity;
            float bestDQ = 0f, bestDR = 0f;

            foreach (var (vec, dq, dr) in hexDirs)
            {
                float dot = Vector2.Dot(impactDirection.normalized, vec.normalized);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestDQ = dq;
                    bestDR = dr;
                }
            }

            float newQ = targetBubble.q + bestDQ;
            float newR = targetBubble.r + bestDR;

            return (newQ, newR);
        }
        
        public static int HexDistance(float q1, float r1, float q2, float r2)
        {
            int x1 = Mathf.RoundToInt(q1);
            int z1 = Mathf.RoundToInt(r1);
            int y1 = -x1 - z1;

            int x2 = Mathf.RoundToInt(q2);
            int z2 = Mathf.RoundToInt(r2);
            int y2 = -x2 - z2;

            return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
        }
        
        int HexDistance(int q1, int r1, int q2, int r2)
        {
            int dx = q2 - q1;
            int dy = r2 - r1;
            int dz = -dx - dy;
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dz));
        }
    }
}
