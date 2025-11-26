using UnityEngine;
using Astrum.Client.UI.Core;

namespace Astrum.Client.UI.Core
{
    /// <summary>
    /// Base class for all UI Views.
    /// Provides lifecycle management and UIRefs handling.
    /// </summary>
    public abstract class UIBase
    {
        protected UIRefs uiRefs;

        /// <summary>
        /// Gets the associated GameObject.
        /// </summary>
        public GameObject GameObject => uiRefs?.gameObject;

        /// <summary>
        /// Gets a value indicating whether this UI is visible.
        /// </summary>
        public bool IsVisible => uiRefs != null && uiRefs.gameObject.activeInHierarchy;

        /// <summary>
        /// Gets a value indicating whether this UI is initialized.
        /// </summary>
        public bool IsInitialized => uiRefs != null;

        /// <summary>
        /// Initializes the UI with the specified references.
        /// </summary>
        /// <param name="refs">The UI references.</param>
        public virtual void Initialize(UIRefs refs)
        {
            uiRefs = refs;
            OnInitialize();
        }

        /// <summary>
        /// Shows the UI.
        /// </summary>
        public virtual void Show()
        {
            if (uiRefs != null)
            {
                uiRefs.gameObject.SetActive(true);
                OnShow();
            }
        }

        /// <summary>
        /// Hides the UI.
        /// </summary>
        public virtual void Hide()
        {
            if (uiRefs != null)
            {
                OnHide();
                uiRefs.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the UI. Called by UIManager.
        /// </summary>
        public virtual void Update()
        {
            if (IsVisible)
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Destroys the UI.
        /// </summary>
        public virtual void Destroy()
        {
            OnDestroy();
            if (uiRefs != null)
            {
                // Note: UIManager usually destroys the GameObject, but we clear refs here.
                // If this is called manually, we might want to destroy the GO.
                // Assuming UIManager handles the actual Object.Destroy of the prefab instance.
            }
            uiRefs = null;
        }

        // Lifecycle hooks for subclasses
        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnDestroy() { }
    }
}
