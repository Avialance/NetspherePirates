using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BlubLib.DotNetty.Handlers.MessageHandling;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using ProudNet.Handlers;
using Serilog;
using Serilog.Core;
using ExpressMapper.Extensions;
using Netsphere.Database.Auth;
using Netsphere.Database;
using Dapper.FastCrud;

namespace Netsphere.Network.Services
{
    internal class InventoryService : ProudMessageHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(InventoryService));

        [MessageHandler(typeof(CUseItemReqMessage))]
        public void UseItemHandler(GameSession session, CUseItemReqMessage message)
        {
            var plr = session.Player;
            var @char = plr.CharacterManager[message.CharacterSlot];
            var item = plr.Inventory[message.ItemId];

            if (@char == null || item == null || (plr.Room != null && plr.RoomInfo.State != PlayerState.Lobby))
            {
                session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask));
                return;
            }

            try
            {
                switch (message.Action)
                {
                    case UseItemAction.Equip:
                        @char.Equip(item, message.EquipSlot);
                        break;

                    case UseItemAction.UnEquip:
                        @char.UnEquip(item.ItemNumber.Category, message.EquipSlot);
                        break;
                }
            }
            catch (CharacterException ex)
            {
                Logger.ForAccount(session)
                    .Error(ex, "Unable to use item");
                session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask));
            }
        }

        [MessageHandler(typeof(CRepairItemReqMessage))]
        public async Task RepairItemHandler(GameSession session, CRepairItemReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();

            foreach (var id in message.Items)
            {
                var item = session.Player.Inventory[id];
                if (item == null)
                {
                    Logger.ForAccount(session)
                        .Error("Item {id} not found", id);
                    session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.Error0 });
                    return;
                }
                if (item.Durability == -1)
                {
                    Logger.ForAccount(session)
                        .Error("Item {item} can not be repaired", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                    session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.Error1 });
                    return;
                }

                var cost = item.CalculateRepair();
                if (session.Player.PEN < cost)
                {
                    session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.NotEnoughMoney });
                    return;
                }

                var price = shop.GetPrice(item);
                if (price == null)
                {
                    Logger.ForAccount(session)
                        .Error("No shop entry found for {item}", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                    session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.Error4 });
                    return;
                }
                if (item.Durability >= price.Durability)
                {
                    await session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.OK, ItemId = item.Id });
                    continue;
                }

                item.Durability = price.Durability;
                session.Player.PEN -= cost;

                await session.SendAsync(new SRepairItemAckMessage { Result = ItemRepairResult.OK, ItemId = item.Id });
                await session.SendAsync(new SRefreshCashInfoAckMessage { PEN = session.Player.PEN, AP = session.Player.AP });
            }
        }

        [MessageHandler(typeof(CRefundItemReqMessage))]
        public void RefundItemHandler(GameSession session, CRefundItemReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();

            var item = session.Player.Inventory[message.ItemId];
            if (item == null)
            {
                Logger.ForAccount(session)
                    .Error("Item {itemId} not found", message.ItemId);
                session.SendAsync(new SRefundItemAckMessage { Result = ItemRefundResult.Failed });
                return;
            }

            var price = shop.GetPrice(item);
            if (price == null)
            {
                Logger.ForAccount(session)
                    .Error("No shop entry found for {item}", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                session.SendAsync(new SRefundItemAckMessage { Result = ItemRefundResult.Failed });
                return;
            }
            if (!price.CanRefund)
            {
                Logger.ForAccount(session)
                    .Error("Cannot refund {item}", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                session.SendAsync(new SRefundItemAckMessage { Result = ItemRefundResult.Failed });
                return;
            }

            session.Player.PEN += item.CalculateRefund();
            session.Player.Inventory.Remove(item);

            session.SendAsync(new SRefundItemAckMessage { Result = ItemRefundResult.OK, ItemId = item.Id });
            session.SendAsync(new SRefreshCashInfoAckMessage { PEN = session.Player.PEN, AP = session.Player.AP });
        }

        [MessageHandler(typeof(CDiscardItemReqMessage))]
        public void DiscardItemHandler(GameSession session, CRefundItemReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();

            var item = session.Player.Inventory[message.ItemId];
            if (item == null)
            {
                Logger.ForAccount(session)
                    .Error("Item {itemId} not found", message.ItemId);
                session.SendAsync(new SDiscardItemAckMessage { Result = 2 });
                return;
            }

            var shopItem = shop.GetItem(item.ItemNumber);
            if (shopItem == null)
            {
                Logger.ForAccount(session)
                    .Error("No shop entry found for item {item}", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                session.SendAsync(new SDiscardItemAckMessage { Result = 2 });
                return;
            }

            if (shopItem.IsDestroyable)
            {
                Logger.ForAccount(session)
                    .Error("Cannot discord {item}", new { item.ItemNumber, item.PriceType, item.PeriodType, item.Period });
                session.SendAsync(new SDiscardItemAckMessage { Result = 2 });
                return;
            }

            session.Player.Inventory.Remove(item);
            session.SendAsync(new SDiscardItemAckMessage { Result = 0, ItemId = item.Id });
        }

        [MessageHandler(typeof(CUseCapsuleReqMessage))]
        public void UseCapsuleReq(GameSession session, CUseCapsuleReqMessage message)
        {
            var ItemBags = GameServer.Instance.ResourceCache.GetItemRewards();
            var plr = session.Player;
            var item = plr.Inventory[message.ItemId];

            if (!ItemBags.ContainsKey(item.ItemNumber))
            {
                session.SendAsync(new SServerResultInfoAckMessage(ServerResult.DBError));
                return;
            }

            item.Count--;
            if (item.Count <= 0)
                plr.Inventory.Remove(item);
            else
                session.SendAsync(new SInventoryActionAckMessage(InventoryAction.Update, item.Map<PlayerItem, ItemDto>()));

            var ItemBag = ItemBags[item.ItemNumber];

            var Rewards = (from bag in ItemBag.Bags
                          let reward = bag.Take()
                          select new CapsuleRewardDto
                          {
                              RewardType = reward.Type,
                              ItemNumber = reward.Item,
                              PriceType = reward.PriceType,
                              PeriodType = reward.PeriodType,
                              Period = reward.Period,
                              PEN = reward.PEN,
                              Effect = reward.Effect,
                              Unk = (byte)reward.Color
                          }).ToArray();

            foreach (var it in Rewards)
            {
                if (it.RewardType == CapsuleRewardType.PEN)
                {
                    plr.PEN += it.PEN;
                }
                else
                {
                    if (it.PeriodType == ItemPeriodType.Units)
                    {
                        var prev = plr.Inventory
                            .FirstOrDefault(p => p.ItemNumber == it.ItemNumber
                            && p.PeriodType == it.PeriodType
                            && p.PriceType == it.PriceType
                            && p.Effect == it.Effect);

                        if (prev == null || prev.ItemNumber == 0)
                        {
                            plr.Inventory.Create(it.ItemNumber, it.PriceType, it.PeriodType, (ushort)it.Period, it.Unk, it.Effect, 1);
                        }
                        else
                        {
                            prev.Count++;
                            session.SendAsync(new SInventoryActionAckMessage(InventoryAction.Update, prev.Map<PlayerItem, ItemDto>()));
                        }
                    }
                    else
                    {
                        plr.Inventory.Create(it.ItemNumber, it.PriceType, it.PeriodType, (ushort)it.Period, it.Unk, it.Effect, 1);
                    }
                }
            }

            session.SendAsync(new SUseCapsuleAckMessage(Rewards, 3));
            session.SendAsync(new SRefreshCashInfoAckMessage(plr.PEN, plr.AP));
        }

        [MessageHandler(typeof(CUseChangeNickNameItemReqMessage))]
        public void UseChangeNickNameItem(GameSession session, CUseChangeNickNameItemReqMessage message)
        {
            var plr = session.Player;
            var item = plr.Inventory[message.ItemId];

            var nickname = new NicknameHistoryDto
            {
                AccountId = (int)plr.Account.Id,
                Nickname = message.Nickname
            };

            switch (item.ItemNumber)
            {
                case 4000001: // Permanent NickName Change
                    nickname.ExpireDate = (long)(-1);
                    break;
                case 4000002: // Remove NickName Change
                    session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask));
                    break;
                case 4000003: // 1 Day Nickname Change
                    nickname.ExpireDate = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();
                    break;
                case 4000004: // 7 Day NickName Change
                    nickname.ExpireDate = DateTimeOffset.Now.AddDays(7).ToUnixTimeSeconds();
                    break;
                case 4000005: // 30 Day NickName Change
                    nickname.ExpireDate = DateTimeOffset.Now.AddDays(30).ToUnixTimeSeconds();
                    break;
                default:
                    session.SendAsync(new SServerResultInfoAckMessage(ServerResult.FailedToRequestTask));
                    return;
            }

            using (var auth = AuthDatabase.Open())
                auth.Insert(nickname);

            plr.Inventory.Remove(item);

            session.SendAsync(new SUseChangeNickItemAckMessage
            {
                Result = 0,
                Unk2 = 0,
                Unk3 = message.Nickname
             });
        }

        [MessageHandler(typeof(CUseResetRecordItemReqMessage))]
        public void CUseResetRecordItem(GameSession session, CUseResetRecordItemReqMessage message)
        {
            var plr = session.Player;
            var item = plr.Inventory[message.ItemId];

            plr.DeathMatch.Deaths = 0;
            plr.DeathMatch.KillAssists = 0;
            plr.DeathMatch.Kills = 0;

            plr.Inventory.Remove(item);

            session.SendAsync(new SUseResetRecordItemAckMessage
            {
                Result = 0,
                Unk2 = 0
            });
        }

        [MessageHandler(typeof(CUseCoinFillingItemReqMessage))]
        public void CUseCoinFillingItem(GameSession session, CUseCoinFillingItemReqMessage message)
        {
            var plr = session.Player;
            var item = plr.Inventory[message.ItemId];

            session.SendAsync(new SUseCoinFillingItemAckMessage
            {
                Result = 0
            });

            if (item.Count > 1)
            {
                item.Count--;
                session.SendAsync(new SInventoryActionAckMessage(InventoryAction.Update, item.Map<PlayerItem, ItemDto>()));
            }
            else if (item.Count == 1)
            {
                plr.Inventory.Remove(item);
            }
            else
            {
                return;
            }

            plr.Coins2 += 10;

            session.SendAsync(new SSetCoinAckMessage { ArcadeCoins = plr.Coins1, BuffCoins = plr.Coins2 });
        }
    }
}
