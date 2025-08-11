using UnityEngine;

namespace Combat {
    [RequireComponent(typeof(Collider2D))]
    public class Pushbox : MonoBehaviour {
        private void Reset() {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = false; // pushboxes should collide
        }
    }
}