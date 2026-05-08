using System.Collections.Generic;

namespace VRRehab.Input
{
    /// <summary>
    /// Most-recent angle for every tracked joint. Updated in place by the
    /// active <see cref="ISensorInputAdapter"/> on the main thread, and read
    /// each frame by the avatar driver and active mini-game.
    /// </summary>
    public class JointSnapshot
    {
        readonly Dictionary<string, float> _angles = new Dictionary<string, float>();
        readonly Dictionary<string, long>  _timestamps = new Dictionary<string, long>();

        public IEnumerable<string> Joints => _angles.Keys;

        public void Set(string joint, float angleDeg, long tsMs)
        {
            _angles[joint] = angleDeg;
            _timestamps[joint] = tsMs;
        }

        public float Get(string joint, float fallback = 0f)
        {
            return _angles.TryGetValue(joint, out var v) ? v : fallback;
        }

        public long TimestampOf(string joint)
        {
            return _timestamps.TryGetValue(joint, out var t) ? t : 0L;
        }

        /// <summary>Clears every joint — used when the active adapter switches.</summary>
        public void Clear()
        {
            _angles.Clear();
            _timestamps.Clear();
        }
    }
}
