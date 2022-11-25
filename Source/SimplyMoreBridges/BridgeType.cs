using System;
using RimWorld;
using Verse;

namespace SimplyMoreBridges
{
    public enum BridgeType
    {
        Wooden,
        Heavy,
        Deep
    }

    public static class Extension
    {
        public static string DefName(this BridgeType bridge)
        {
            switch (bridge)
            {
                case BridgeType.Wooden: return "WoodenBridge";
                case BridgeType.Heavy: return "HeavyBridge";
                case BridgeType.Deep: return "DeepBridge";
                default: throw new ArgumentOutOfRangeException(nameof(bridge), bridge, null);
            }
        }

        public static string Label(this BridgeType bridge)
        {
            switch (bridge)
            {
                case BridgeType.Wooden: return "Wooden Bridge ";
                case BridgeType.Heavy: return "Heavy Bridge ";
                case BridgeType.Deep: return "Deep Bridge ";
                default: throw new ArgumentOutOfRangeException(nameof(bridge), bridge, null);
            }
        }
    }
}