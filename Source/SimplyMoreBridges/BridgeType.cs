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
            return bridge switch
            {
                BridgeType.Wooden => "WoodenBridge",
                BridgeType.Heavy => "HeavyBridge",
                BridgeType.Deep => "DeepBridge",
                _ => throw new ArgumentOutOfRangeException(nameof(bridge), bridge, null)
            };
        }

        public static string Label(this BridgeType bridge)
        {
            return bridge switch
            {
                BridgeType.Wooden => "Wooden Bridge ",
                BridgeType.Heavy => "Heavy Bridge ",
                BridgeType.Deep => "Deep Bridge ",
                _ => throw new ArgumentOutOfRangeException(nameof(bridge), bridge, null)
            };
        }
    }
}