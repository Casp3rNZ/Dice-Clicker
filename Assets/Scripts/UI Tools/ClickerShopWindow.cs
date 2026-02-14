using UnityEngine;

/// <summary>
/// Clicker (auto-click) shop window. Inherits slide-in/out and
/// mutual-exclusivity from <see cref="UIWindow"/>.
/// </summary>
public class ClickerShopWindow : UIWindow
{
    // The window open/close behaviour is handled entirely by UIWindow.
    // Auto-click shop content is managed by UIAutoClickShopHandler (separate component).
}
