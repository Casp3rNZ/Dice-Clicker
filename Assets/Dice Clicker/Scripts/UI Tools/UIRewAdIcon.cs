using System.Runtime.Serialization;
using UnityEngine.EventSystems;
using UnityEngine;

namespace MyGame
{
    public class UIRewAdIcon : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] 
        private UIRewAdConfirmWindow adConfirmWindow;
        private CanvasGroup canvasGroup;

        private void Start()
        {
            if (adConfirmWindow == null)
            {
                Debug.LogError("UIRewAdIcon: Ad confirmation window reference is not set.", this);
            }
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogError("UIRewAdIcon: CanvasGroup reference is not set and not found on GameObject.", this);
            }
        }
        
        // Listener for UI click events
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Rewarded ad icon clicked. Attempting to show confirmation window.");
            adConfirmWindow?.Open();
        }

        public void HideIcon()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void ShowIcon()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
