using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace TMKOC.FuzzBugClone
{
    public class JarSlot : MonoBehaviour, IDropHandler
    {
        public bool IsOccupied { get; private set; } = false;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                // We are looking for a JarController being dragged via Draggable script
                JarController jar = eventData.pointerDrag.GetComponent<JarController>();
                Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();

                if (jar != null && draggable != null)
                {
                    Debug.Log($"Dropped Jar {jar.name} into Slot {name}");
                    
                    // Logic to snap jar to this slot
                    // 1. Consume the draggable event so it doesn't return to start
                    draggable.Consume();

                    // 2. Parent Jar to this Slot (or just move position)
                    // If we parent, we might mess up scale if slot has diff scale. 
                    // Safer to keep parent as is or move to a common container?
                    // User said "Jar Slots will be a child of a gameObject...". 
                    // Let's assume parenting to Slot is fine for now, or just setting position.
                    
                    // Move Jar to Slot Position (Smooth Snap)
                    jar.transform.SetParent(transform); // Parent to slot for logic tracking
                    jar.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
                    
                    IsOccupied = true;
                    
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnJarDroppedInSlot();
                    }
                }
            }
        }
    }
}
