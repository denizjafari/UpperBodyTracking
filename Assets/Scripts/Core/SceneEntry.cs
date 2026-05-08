using UnityEngine;

namespace VRRehab.Core
{
    /// <summary>
    /// Base class every additive (non-Core) scene's root GameObject must use.
    /// SceneFlowManager calls <see cref="OnEnter"/> after the scene loads
    /// and <see cref="OnExit"/> before unload — this is where you wire up
    /// or tear down event subscriptions and coroutines.
    /// </summary>
    /// <remarks>
    /// Rule: an additive scene must contain exactly one SceneEntry. It must
    /// NOT instantiate cameras, OVRRig prefabs, or audio listeners — those
    /// belong to the Core scene only.
    /// </remarks>
    public abstract class SceneEntry : MonoBehaviour
    {
        public abstract string SceneKey { get; }

        public virtual void OnEnter() { }
        public virtual void OnExit()  { }
    }
}
