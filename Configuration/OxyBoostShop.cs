using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LL = LethalLib.Modules;

namespace Oxygen.Configuration
{
    // Keep the item/prefab registered on every peer. Only shop availability follows the host.
    internal static class OxyBoostShop
    {
        private static Terminal terminal;
        private static TerminalNodesList nodes;
        private static TerminalKeyword[] removedKeywords;
        private static readonly Dictionary<TerminalKeyword, CompatibleNoun[]> removedNouns = new();
        private static bool disabled;

        internal static void Reset()
        {
            Restore();
            terminal = null;
            nodes = null;
            disabled = false;
            removedKeywords = null;
            removedNouns.Clear();
            var item = OxygenBase.Instance?.oxyBoost;
            var shop = LL.Items.shopItems.FirstOrDefault(entry => entry.item == item);
            if (shop != null) shop.wasRemoved = false;
        }

        private static void Restore()
        {
            if (!disabled) return;
            // Terminal nodes are shared assets and can survive the Terminal/lobby itself.
            if (nodes != null)
                nodes.allKeywords = nodes.allKeywords.Union(removedKeywords).ToArray();
            foreach (var pair in removedNouns)
                if (pair.Key != null) pair.Key.compatibleNouns = pair.Key.compatibleNouns.Union(pair.Value).ToArray();
            removedNouns.Clear();
            removedKeywords = null;
            disabled = false;
        }

        internal static void Update(Item item, bool enabled, int price)
        {
            if (item == null) return;
            var activeTerminal = Object.FindObjectOfType<Terminal>();
            if (activeTerminal == null || activeTerminal.terminalNodes?.allKeywords == null) return;
            if (terminal != activeTerminal) { Reset(); terminal = activeTerminal; nodes = terminal.terminalNodes; }
            var shop = LL.Items.shopItems.FirstOrDefault(entry => entry.item == item);
            if (shop == null) return;
            if (!enabled && !disabled)
            {
                var keywords = terminal.terminalNodes.allKeywords;
                var nouns = keywords.Where(keyword => keyword.compatibleNouns != null)
                    .ToDictionary(keyword => keyword, keyword => keyword.compatibleNouns);
                LL.Items.RemoveShopItem(item);
                removedKeywords = keywords.Except(terminal.terminalNodes.allKeywords).ToArray();
                foreach (var pair in nouns)
                    removedNouns[pair.Key] = pair.Value.Except(pair.Key.compatibleNouns).ToArray();
                disabled = true;
            }
            else if (enabled && disabled)
            {
                Restore();
                shop.wasRemoved = false;
            }
            LL.Items.UpdateShopItemPrice(item, price);
        }
    }
}
