﻿using BlubLib.Serialization;

namespace Netsphere.Network.Data.Game
{
    [BlubContract]
    public class RandomShopItemDto
    {
        [BlubMember(0)]
        public uint Unk1 { get; set; }

        [BlubMember(1)]
        public uint ItemNumber { get; set; }

        [BlubMember(2)]
        public uint Effect { get; set; }

        [BlubMember(3)]
        public uint Unk4 { get; set; }

        [BlubMember(4)]
        public ItemPeriodType PeriodType { get; set; }

        [BlubMember(5)]
        public ushort Period { get; set; }
    }
}
