using System;
using System.Collections.Generic;
using Acrofobia.Player;
using UnityEngine;

namespace Acrofobia.UI
{
    /// <summary>
    /// Tracks which panels currently need the mouse. While any is open the cursor is free and the
    /// player rig ignores look and movement; when all close, the cursor is locked again.
    /// </summary>
    public sealed class ModalFocus : MonoBehaviour
    {
        [SerializeField] private PlayerRig playerRig;

        private readonly List<object> owners = new();

        public event Action Changed;

        public bool AnyOpen => owners.Count > 0;

        private void Start() => ApplyCursor();

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyCursor();
        }

        public void Open(object owner)
        {
            if (owners.Contains(owner)) return;
            owners.Add(owner);
            playerRig.Block(this);
            ApplyCursor();
            Changed?.Invoke();
        }

        public void Close(object owner)
        {
            if (!owners.Remove(owner)) return;
            if (owners.Count == 0) playerRig.Unblock(this);
            ApplyCursor();
            Changed?.Invoke();
        }

        public bool IsOpen(object owner) => owners.Contains(owner);

        /// <summary>The most recently opened panel receives keyboard confirmation and value steps.</summary>
        public bool IsTop(object owner) => owners.Count > 0 && ReferenceEquals(owners[^1], owner);

        private void ApplyCursor()
        {
            Cursor.lockState = AnyOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = AnyOpen;
        }
    }
}
