#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class OptionsUISetup : EditorWindow
{
    [MenuItem("Tools/Setup Volume Sliders")]
    public static void SetupSliders()
    {
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in scene!");
            return;
        }

        // Get the options menu panel
        // We use reflection because optionsMenu is private/serialized
        var field = typeof(UIManager).GetField("optionsMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        GameObject optionsPanel = field?.GetValue(uiManager) as GameObject;

        if (optionsPanel == null)
        {
            Debug.LogError("Options Menu panel not assigned in UIManager!");
            return;
        }

        // Create Master Volume Slider
        CreateSlider(optionsPanel.transform, "MasterVolumeSlider", "Master Volume");
        
        // Create SFX Volume Slider
        CreateSlider(optionsPanel.transform, "SFXVolumeSlider", "SFX Volume");

        Debug.Log("Volume Sliders added to Options Panel. Re-run 'Setup Sliders' or refresh the UI to see changes.");
    }

    private static void CreateSlider(Transform parent, string name, string labelText)
    {
        // Check if already exists
        if (parent.Find(name) != null) return;

        GameObject sliderObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObj.name = name;
        sliderObj.transform.SetParent(parent, false);

        // Add Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(sliderObj.transform, false);
        
        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 20; // Medium font
        tmp.alignment = TextAlignmentOptions.Left; // Left align text
        tmp.color = Color.white;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); // Anchor to top-left of slider
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(0f, 5f); // Slightly above slider
        rt.sizeDelta = new Vector2(300f, 40f);

        // Position Slider (Medium size)
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);
    }
}
#endif
