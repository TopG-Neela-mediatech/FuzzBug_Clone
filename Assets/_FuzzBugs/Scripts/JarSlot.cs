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

                    // Check for Existing Jar -> Swap Logic
                    JarController existingJar = GetComponentInChildren<JarController>();
                    if (existingJar != null && existingJar != jar)
                    {
                        Transform otherSlot = draggable.OriginalParent;
                        if (otherSlot != null)
                        {
                            Debug.Log($"Swapping Jars: {existingJar.name} to {otherSlot.name}");
                            existingJar.transform.SetParent(otherSlot);
                            existingJar.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
                            
                            // Note: If otherSlot is a JarSlot, we should ideally trigger its logic or ensure clean state,
                            // but simpler reparenting usually works if IsOccupied is just a logic flag not a blocker.
                        }
                    }

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
