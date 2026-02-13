using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem_", menuName = "Shop/Item")]
public class ShopItem : ScriptableObject
{
    public int Id;
    public string Name;
    public string Description;
    public int multiplier;
    public Material diceMaterial;
    public Material pipMaterial;
    public int price;
    [Tooltip("Multiplicative growth rate per purchase (e.g. 1.15 = 15% more expensive each time).")]
    public float priceGrowthRate = 1.15f;
}
