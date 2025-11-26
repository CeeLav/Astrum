using UnityEngine;

namespace Astrum.Client.UI.Core
{
    public abstract class UIBase
    {
        protected UIRefs uiRefs;
        public bool IsInitialized => uiRefs != null;

        public virtual void Initialize(UIRefs refs) 
        { 
            uiRefs = refs; 
            OnInitialize(); 
        }

        public virtual void Show() 
        { 
            if (uiRefs != null) 
            { 
                uiRefs.gameObject.SetActive(true); 
                OnShow(); 
            } 
        }

        public virtual void Hide() 
        { 
            if (uiRefs != null) 
            { 
                OnHide(); 
                uiRefs.gameObject.SetActive(false); 
            } 
        }

        public virtual void Update() 
        { 
            if (IsInitialized && uiRefs.gameObject.activeInHierarchy) 
            { 
                OnUpdate(); 
            } 
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnUpdate() { }
    }
}


