using UnityEngine;
using VRRehab.Data;

namespace VRRehab.Avatar
{
    /// <summary>
    /// Maps RPi joint keys to Unity <see cref="HumanBodyBones"/>.
    /// Right upper limb only — see design §3 (single-arm rehab MVP).
    /// </summary>
    public static class HumanoidJointMap
    {
        /// <summary>
        /// Returns the bone driven by a given joint key, or
        /// <see cref="HumanBodyBones.LastBone"/> if unmapped.
        /// </summary>
        public static HumanBodyBones BoneFor(string jointKey)
        {
            switch (jointKey)
            {
                case JointKeys.ShoulderFlexion:
                case JointKeys.ShoulderAbduction:
                case JointKeys.ShoulderRotation:
                    return HumanBodyBones.RightUpperArm;
                case JointKeys.ElbowFlexion:
                    return HumanBodyBones.RightLowerArm;
                case JointKeys.WristPronation:
                    return HumanBodyBones.RightHand;
                default:
                    return HumanBodyBones.LastBone;
            }
        }

        /// <summary>
        /// The local rotation axis (in the bone's local space) that the joint
        /// angle drives. Tuned for a Mixamo Y-Bot rig — re-verify if you swap
        /// to the Movement SDK sample avatar (axes may differ).
        /// </summary>
        public static Vector3 AxisFor(string jointKey)
        {
            switch (jointKey)
            {
                case JointKeys.ShoulderFlexion:   return Vector3.right;   // pitch
                case JointKeys.ShoulderAbduction: return Vector3.forward; // roll
                case JointKeys.ShoulderRotation:  return Vector3.up;      // yaw
                case JointKeys.ElbowFlexion:      return Vector3.right;
                case JointKeys.WristPronation:    return Vector3.up;
                default: return Vector3.zero;
            }
        }
    }
}
