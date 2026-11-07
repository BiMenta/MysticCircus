using System.Collections.Generic;
using UnityEngine;

namespace MysticCircus
{
    // F3「镜中慢半拍」: a MirrorOnly body that replays where the player stood a moment ago.
    // Records pose history instead of caching mirror frames, so it costs almost nothing.
    // Stretch() makes every reflection lag much further for a moment, e.g. as a door opens.
    public sealed class DelayedMirrorBody : MonoBehaviour
    {
        const int Capacity = 256;
        public FirstPersonPlayer Player;
        [Tooltip("Seconds behind the player. 0.25 is subtle: players may not notice it at first.")]
        public float Delay = 0.25f;

        struct Pose { public float Time; public Vector3 Position; public float Yaw; }
        readonly Pose[] history = new Pose[Capacity];
        int newest = -1, count;

        static float stretchStart = -100, stretchPeak, stretchRamp, stretchHold, stretchRecover;

        // Every delayed body jumps to peak seconds of lag, holds, then eases back to its own Delay.
        public static void Stretch(float peak = 0.6f, float ramp = 0.3f, float hold = 1.2f, float recover = 2.5f)
        {
            stretchStart = Time.time;
            stretchPeak = peak;
            stretchRamp = Mathf.Max(0.01f, ramp);
            stretchHold = hold;
            stretchRecover = Mathf.Max(0.01f, recover);
        }

        // 0..1 weight of the current stretch at the given time since it began.
        public static float StretchWeight(float elapsed, float ramp, float hold, float recover)
        {
            if (elapsed < 0) return 0;
            if (elapsed < ramp) return elapsed / ramp;
            if (elapsed < ramp + hold) return 1;
            return Mathf.Clamp01(1 - (elapsed - ramp - hold) / recover);
        }

        public float EffectiveDelay
        {
            get
            {
                float weight = StretchWeight(Time.time - stretchStart, stretchRamp, stretchHold, stretchRecover);
                return Mathf.Lerp(Delay, Mathf.Max(Delay, stretchPeak), weight);
            }
        }

        void LateUpdate()
        {
            if (Player == null) return;
            newest = (newest + 1) % Capacity;
            history[newest] = new Pose { Time = Time.time, Position = Player.transform.position, Yaw = Player.transform.eulerAngles.y };
            count = Mathf.Min(count + 1, Capacity);
            var pose = Sample(Time.time - EffectiveDelay);
            transform.SetPositionAndRotation(pose.Position, Quaternion.Euler(0, pose.Yaw, 0));
        }

        // Walks back from the newest pose; blends the two poses around the wanted moment.
        Pose Sample(float at)
        {
            var later = history[newest];
            for (int i = 1; i < count; i++)
            {
                var earlier = history[(newest - i + Capacity) % Capacity];
                if (earlier.Time <= at)
                {
                    float span = later.Time - earlier.Time;
                    float t = span > 0 ? (at - earlier.Time) / span : 0;
                    return new Pose
                    {
                        Time = at,
                        Position = Vector3.Lerp(earlier.Position, later.Position, t),
                        Yaw = Mathf.LerpAngle(earlier.Yaw, later.Yaw, t),
                    };
                }
                later = earlier;
            }
            return later;
        }
    }
}
