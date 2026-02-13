using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ImageAligner : MonoBehaviour
{
    public RawImage targetImage;
    public float padding = 10f; // Space between text and image

    private TextMeshProUGUI _tmpText;

    void Start()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();
        AlignImage();
    }

    public void AlignImage()
    {
        if (_tmpText == null || targetImage == null) return;

        // Force TMP to generate text mesh info for the current text
        _tmpText.ForceMeshUpdate();

        // Check if any characters were rendered
        if (_tmpText.textInfo.characterCount > 0)
        {
            // Get the info for the FIRST visible character
            TMP_CharacterInfo firstChar = _tmpText.textInfo.characterInfo[0];

            // Calculate the left-most edge (BL = bottom-left, TR = top-right)
            float textLeftEdge = firstChar.bottomLeft.x;

            // Get the RectTransforms
            RectTransform textRect = _tmpText.rectTransform;
            RectTransform imageRect = targetImage.rectTransform;

            // Convert the local text position to a position inside the parent
            Vector3 imageLocalPos = imageRect.localPosition;
            // Position the image's right edge at the text's left edge - padding
            imageLocalPos.x = textLeftEdge - (padding * textRect.localScale.x);
            // Keep the image's original Y position (centered based on its pivot)
            imageRect.localPosition = imageLocalPos;
        }
    }
}