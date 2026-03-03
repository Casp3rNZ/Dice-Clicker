using UnityEngine;
using System.Collections.Generic;

namespace MyGame
{
    /// <summary>
    /// Drives automatic dice rolls for every dice tier that has purchased auto-click upgrades.
    /// 
    /// On startup it reads save data to build a list of active tiers and their CPS.
    /// Each frame it accumulates time per tier and fires rolls through
    /// <see cref="DiceManager.RollNextDiceOfType"/> when enough time has elapsed,
    /// round-robining within each type independently.
    ///
    /// The manager is notified of new purchases via <see cref="RefreshTier"/>,
    /// keeping a zero-allocation hot path in Update().
    /// </summary>
    public class AutoClickerManager : MonoBehaviour
    {
        public static AutoClickerManager Instance { get; private set; }

        [SerializeField] private DiceManager diceManager;
        [SerializeField] private SaveManager saveManager;

        [Tooltip("Hard cap on rolls per tier per frame to prevent runaway catch-up after long pauses.")]
        [SerializeField] private int maxRollsPerTierPerFrame = 5;

        // Per-tier runtime state.
        // Uses CPS-based fractional accumulation: each frame adds cps * dt,
        // and a roll fires when accumulator >= 1.0.  This keeps intermediate
        // values small regardless of CPS, avoiding float-precision drift that
        // can occur when accumulating tiny dt values toward a large interval.
        private struct TierState
        {
            public int diceTypeId;
            public float cps;          // rolls per second
            public float accumulator;  // fractional rolls banked (fires at >= 1.0)
        }

        private readonly List<TierState> _tiers = new List<TierState>();
        private readonly Dictionary<int, int> _tierIndex = new Dictionary<int, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (diceManager == null)
            {
                Debug.LogError("AutoClickerManager: diceManager is not assigned.", this);
                return;
            }
            if (saveManager == null)
            {
                Debug.LogError("AutoClickerManager: saveManager is not assigned.", this);
                return;
            }

            BuildTiersFromSave();
        }

        /// <summary>
        /// Scans all shop items and builds the active tier list from save data.
        /// Called once at startup.
        /// </summary>
        private void BuildTiersFromSave()
        {
            ShopItem[] items = DiceShopManager.Instance != null
                ? DiceShopManager.Instance.ShopItems
                : null;

            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                ShopItem item = items[i];
                if (item == null) continue;

                int purchased = saveManager.GetAutoClickPurchaseCount(item.Id);
                if (purchased <= 0) continue;

                float cps = item.autoClicksPerSecond * purchased;
                AddOrUpdateTier(item.Id, cps);
            }
        }

        /// <summary>
        /// Call after a purchase to add or update a tier's CPS at runtime.
        /// </summary>
        public void RefreshTier(int diceTypeId, float cps)
        {
            if (cps <= 0f)
            {
                RemoveTier(diceTypeId);
                return;
            }
            AddOrUpdateTier(diceTypeId, cps);
        }

        private void AddOrUpdateTier(int diceTypeId, float cps)
        {
            if (_tierIndex.TryGetValue(diceTypeId, out int idx))
            {
                // Update existing — preserve accumulated fractional rolls
                var t = _tiers[idx];
                t.cps = cps;
                _tiers[idx] = t;
            }
            else
            {
                _tierIndex[diceTypeId] = _tiers.Count;
                _tiers.Add(new TierState
                {
                    diceTypeId = diceTypeId,
                    cps = cps,
                    accumulator = 0f
                });
            }
        }

        private void RemoveTier(int diceTypeId)
        {
            if (!_tierIndex.TryGetValue(diceTypeId, out int idx)) return;

            int last = _tiers.Count - 1;
            if (idx != last)
            {
                // Swap-remove: move last element into the gap
                var moved = _tiers[last];
                _tiers[idx] = moved;
                _tierIndex[moved.diceTypeId] = idx;
            }
            _tiers.RemoveAt(last);
            _tierIndex.Remove(diceTypeId);
        }

        private void Update()
        {
            int tierCount = _tiers.Count;
            if (tierCount == 0) return;

            float dt = Time.deltaTime;

            for (int i = 0; i < tierCount; i++)
            {
                var t = _tiers[i];
                t.accumulator += t.cps * dt;

                int rolls = 0;
                while (t.accumulator >= 1f && rolls < maxRollsPerTierPerFrame)
                {
                    // Only subtract the roll if the dice actually rolled.
                    // DiceController.Roll() is a no-op when the die is already
                    // mid-air, so we keep the accumulated credit and retry next frame.
                    if (diceManager.RollNextDiceOfType(t.diceTypeId))
                    {
                        t.accumulator -= 1f;
                        rolls++;
                    }
                    else
                    {
                        // No available die could be rolled — stop trying this frame
                        break;
                    }
                }

                // Clamp so we never bank more than maxRollsPerTierPerFrame rolls
                if (t.accumulator > maxRollsPerTierPerFrame)
                    t.accumulator = maxRollsPerTierPerFrame;

                _tiers[i] = t;
            }
        }
    }
}
