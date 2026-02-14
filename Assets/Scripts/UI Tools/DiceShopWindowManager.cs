using UnityEngine;

/// <summary>
/// Dice shop window. Inherits slide-in/out and mutual-exclusivity from UIWindow.
/// Keeps legacy method names so existing button OnClick references still work.
/// </summary>
public class DiceShopWindowManager : UIWindow
{
    // Legacy convenience methods — existing button OnClick references keep working.
    public void OpenShop(bool instant = false) => Open(instant);
    public void CloseShop(bool instant = false) => Close(instant);
    public void ToggleShop(bool instant = false) => Toggle(instant);
}
