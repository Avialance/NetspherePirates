using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netsphere.Database.Game;
using Netsphere.Network.Message.Game;

namespace Netsphere
{
    internal class BoostManager
    {
        private Player _player;
        PlayerItem _boost;

        internal BoostManager(Player plr)
        {
            _player = plr;
        }

        public void Equip(PlayerItem item, int slot)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!CanEquip(item, slot))
                throw new CharacterException($"Cannot equip item {item.ItemNumber} on slot {slot}");

            _boost = item;

            _player.Session.SendAsync(new SUseItemAckMessage
            {
                CharacterSlot = 0,
                ItemId = item.Id,
                Action = UseItemAction.Equip,
                EquipSlot = 0
            });
        }

        public void UnEquip(int slot)
        {
            if (_player.Room != null && _player.RoomInfo.State != PlayerState.Lobby) // Cant change items while playing
                throw new CharacterException("Can't change items while playing");

            if (_boost == null)
                throw new CharacterException("No boost equiped");

            _player.Session.SendAsync(new SUseItemAckMessage
            {
                CharacterSlot = 0,
                ItemId = _boost?.Id ?? 0,
                Action = UseItemAction.UnEquip,
                EquipSlot = (byte)slot
            });

            _boost = null;
        }

        public int GetBoostType()
        {
            if (_boost == null)
                return 0;

            var btype = 0;

            switch (_boost.ItemNumber)
            {
                case 5000001: // Pen PLUS 50
                case 5000002: // Pen PLUS 100
                    btype = 2;
                    break;
                case 5000003: // Exp PLUS 30
                    btype = 4;
                    break;
            }

            return btype;
        }

        public float GetExpRate()
        {
            if (_boost == null)
                return 0.0f;

            if (_boost.ItemNumber == 5000003)
                return 0.30f;

            return 0.0f;
        }

        public float GetPenRate()
        {
            if (_boost == null)
                return 0.0f;

            if (_boost.ItemNumber == 5000001)
                return 0.50f;

            if (_boost.ItemNumber == 5000002)
                return 1.00f;

            return 0.0f;
        }

        public PlayerItem GetItem(int slot)
        {
            switch (slot)
            {
                case 0:
                    return _boost;

                default:
                    throw new CharacterException("Invalid slot: " + slot);
            }
        }

        public IReadOnlyList<PlayerItem> GetItems()
        {
            return new List<PlayerItem> { _boost };
        }

        public bool CanEquip(PlayerItem item, int slot)
        {
            // ReSharper disable once UseNullPropagation
            if (item == null)
                return false;

            if (item.ItemNumber.Category != ItemCategory.Boost)
                return false;

            if (slot != 0)
                return false;

            //if (_boost != null) // Slot needs to be empty
            //    return false;

            if (_player.Room != null && _player.RoomInfo.State != PlayerState.Lobby) // Cant change items while playing
                return false;

            return true;
        }
    }
}
