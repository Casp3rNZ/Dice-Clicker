using UnityEngine;
using System;
using System.Numerics;

/// <summary>
/// ScriptableObject defining a single dice type.
/// Contains dice shop pricing, visual data, and auto-clicker upgrade metadata.
/// </summary>
[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Dice Item")]
public class ShopItem : ScriptableObject
{
    public int Id;
    public string Name;
    public string Description;
    public int price;
    [Tooltip("Multiplicative growth rate per purchase (e.g. 1.15 = 15% more expensive each time).")]
    public float priceGrowthRate = 1.15f;

    [Header("Dice")]
    public int multiplier;
    public Material diceMaterial;
    public Material pipMaterial;

    [Header("Auto-Clicker")]
    [Tooltip("Base price for the first auto-click upgrade for this dice tier.")]
    public int autoClickBasePrice = 100;

    [Tooltip("Price growth rate per auto-click upgrade purchase.")]
    public float autoClickPriceGrowthRate = 1.25f;

    [Tooltip("Automatic rolls per second each purchase adds for this dice tier.")]
    public float autoClicksPerSecond = 1f;

    // ─────────────── Shared Price Helpers ───────────────

    /// <summary>
    /// Calculates incremental price: basePrice × growthRate ^ totalPurchased.
    /// </summary>
    public static BigInteger CalculatePrice(int basePrice, float growthRate, int totalPurchased)
    {
        if (totalPurchased <= 0) return new BigInteger(basePrice);
        return new BigInteger(basePrice * Math.Pow(growthRate, totalPurchased));
    }

    /// <summary>
    /// Dice-shop price for this item given how many have been purchased.
    /// </summary>
    public BigInteger GetPrice(int totalPurchased)
    {
        return CalculatePrice(price, priceGrowthRate, totalPurchased);
    }

    /// <summary>
    /// Auto-click upgrade price for this dice tier given how many have been purchased.
    /// </summary>
    public BigInteger GetAutoClickPrice(int totalPurchased)
    {
        return CalculatePrice(autoClickBasePrice, autoClickPriceGrowthRate, totalPurchased);
    }
}
