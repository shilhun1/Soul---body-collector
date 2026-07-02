namespace SmilingEclipse.STMImporter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [Serializable]
    public class UnlockedInfo
    {
        [SerializeField] public bool isUnlocked;
        [HideInInspector] public bool initiallyUnlocked;
        [HideInInspector] public float unlockRequisit = 1f;

        public UnityEvent OnUnlocked;
        public UnityEvent OnLocked;

        public void Setup(bool isInitiallyUnlock, float unlockRequisit)
        {
            initiallyUnlocked = isInitiallyUnlock;
            this.unlockRequisit = unlockRequisit;
            if (isInitiallyUnlock) { UnlockMe(); }
            else { LockMe(); }


        }
        public bool TryUnlock(float value)
        {
            if (value >= unlockRequisit)
            {
                UnlockMe();
                return true;
            }
            LockMe();
            return false;
        }
        public virtual void UnlockMe()
        {
            isUnlocked = true;
            OnUnlocked?.Invoke();
        }
        public virtual void LockMe()
        {
            isUnlocked = false;
            OnLocked?.Invoke();
        }
    }
}