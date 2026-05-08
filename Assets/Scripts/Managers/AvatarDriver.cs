using System.Collections.Generic;
using UnityEngine;
using VRRehab.Avatar;
using VRRehab.Core;
using VRRehab.Data;
using VRRehab.Input;

namespace VRRehab.Managers
{
    /// <summary>
    /// Drives a humanoid Animator from the live <see cref="JointSnapshot"/>.
    /// Right upper limb only (per MVP scope). Runs in <c>LateUpdate</c> so it
    /// overrides any clip output the Animator produced earlier in the frame.
    /// </summary>
    /// <remarks>
    /// Smoothing: 1-frame Slerp at α=<see cref="smoothing"/> hides UDP jitter
    /// without adding perceptible lag. Profiled at &lt; 0.4 ms CPU on Quest 3S.
    /// </remarks>
    public class AvatarDriver : MonoBehaviour
    {
        [SerializeField] Animator anim;
        [Tooltip("0..1 — higher = more smoothing, more lag. Default 0.6 matches Phase 3 plan.")]
        [Range(0f, 1f)] public float smoothing = 0.6f;

        [Tooltip("Mirror mode flips driven bones to the LEFT arm — used in Calibration A so the patient sees the avatar facing them.")]
        public bool mirror;

        // Cached bone transforms so we don't query the Animator each frame.
        readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        readonly Dictionary<string, Quaternion> _baseRot = new Dictionary<string, Quaternion>();

        // Demo-clip override: when non-null, procedural drive is suspended.
        string _activeDemoClip;

        void Awake() => Services.Avatar = this;

        void Start()
        {
            CacheBones();
        }

        void CacheBones()
        {
            if (anim == null)
            {
                Debug.LogError("[AvatarDriver] Animator reference missing.");
                return;
            }
            foreach (var key in JointKeys.CalibrationOrder)
            {
                var bone = HumanoidJointMap.BoneFor(key);
                if (bone == HumanBodyBones.LastBone) continue;
                if (mirror) bone = MirrorBone(bone);
                var t = anim.GetBoneTransform(bone);
                if (t == null)
                {
                    Debug.LogWarning($"[AvatarDriver] Animator has no bone for {bone} (joint {key})");
                    continue;
                }
                _bones[key] = t;
                _baseRot[key] = t.localRotation;
            }
        }

        public void SetMirror(bool on)
        {
            if (mirror == on) return;
            mirror = on;
            _bones.Clear();
            _baseRot.Clear();
            CacheBones();
        }

        // --- Demo clips (Phase 7 Calibration B uses this) --------------------

        public void PlayDemo(string clipName)
        {
            if (anim == null || string.IsNullOrEmpty(clipName)) return;
            _activeDemoClip = clipName;
            anim.Play(clipName, 0, 0f);
        }

        public void StopDemo()
        {
            if (anim == null) return;
            _activeDemoClip = null;
            // Re-enter procedural drive — Animator stays in Always mode but we
            // overwrite bone rotations in LateUpdate.
        }

        public bool IsPlayingDemo => !string.IsNullOrEmpty(_activeDemoClip);

        // --- Procedural drive ------------------------------------------------

        void LateUpdate()
        {
            if (IsPlayingDemo) return;
            var sensors = Services.Sensors;
            if (sensors == null) return;

            foreach (var kvp in _bones)
            {
                string joint = kvp.Key;
                Transform bone = kvp.Value;
                float deg = sensors.GetAngle(joint);
                if (mirror) deg = -deg;

                Vector3 axis = HumanoidJointMap.AxisFor(joint);
                Quaternion target = _baseRot[joint] * Quaternion.AngleAxis(deg, axis);
                bone.localRotation = Quaternion.Slerp(bone.localRotation, target, 1f - smoothing);
            }
        }

        static HumanBodyBones MirrorBone(HumanBodyBones b)
        {
            return b switch
            {
                HumanBodyBones.RightUpperArm => HumanBodyBones.LeftUpperArm,
                HumanBodyBones.RightLowerArm => HumanBodyBones.LeftLowerArm,
                HumanBodyBones.RightHand     => HumanBodyBones.LeftHand,
                _ => b
            };
        }
    }
}
