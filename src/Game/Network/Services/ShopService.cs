using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlubLib.DotNetty.Handlers.MessageHandling;
using BlubLib.IO;
using ExpressMapper.Extensions;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using ProudNet.Handlers;
using Serilog;
using Serilog.Core;

namespace Netsphere.Network.Services
{
    internal class ShopService : ProudMessageHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(ShopService));

        [MessageHandler(typeof(CNewShopUpdateCheckReqMessage))]
        public void ShopUpdateCheckHandler(GameSession session, CNewShopUpdateCheckReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();
            var version = shop.Version;
            session.SendAsync(new SNewShopUpdateCheckAckMessage
            {
                Date01 = version,
                Date02 = version,
                Date03 = version,
                Date04 = version,
                Unk = 0
            });
            //session.Send(new SRandomShopInfoAckMessage
            //{
            //    Info = new RandomShopDto
            //    {
            //        ItemNumbers = new List<ItemNumber> { 2000001, 2000002, 2000003 },
            //        Effects = new List<uint> { 0, 0, 0 },
            //        Colors = new List<uint> { 2, 0, 0 },
            //        PeriodTypes = new List<ItemPeriodType> { ItemPeriodType.Hours, ItemPeriodType.Hours, ItemPeriodType.Hours },
            //        Periods = new List<ushort> { 2, 4, 10 },
            //        Unk6 = 10000,
            //    }
            //});

            if (message.Date01 == version &&
                message.Date02 == version &&
                message.Date03 == version &&
                message.Date04 == version)
            {
                return;
            }


            #region NewShopPrice

            using (var w = new BinaryWriter(new MemoryStream()))
            {
                w.Serialize(shop.Prices.Values.ToArray());

                session.SendAsync(new SNewShopUpdateInfoAckMessage
                {
                    Type = ShopResourceType.NewShopPrice,
                    Data = w.ToArray(),
                    Date = version
                });
            }

            #endregion

            #region NewShopEffect

            using (var w = new BinaryWriter(new MemoryStream()))
            {
                w.Serialize(shop.Effects.Values.ToArray());

                session.SendAsync(new SNewShopUpdateInfoAckMessage
                {
                    Type = ShopResourceType.NewShopEffect,
                    Data = w.ToArray(),
                    Date = version
                });
            }

            #endregion

            #region NewShopItem

            using (var w = new BinaryWriter(new MemoryStream()))
            {
                w.Serialize(shop.Items.Values.ToArray());

                session.SendAsync(new SNewShopUpdateInfoAckMessage
                {
                    Type = ShopResourceType.NewShopItem,
                    Data = w.ToArray(),
                    Date = version
                });
            }

            #endregion

            // ToDo
            using (var w = new BinaryWriter(new MemoryStream()))
            {
                w.Write(0);

                session.SendAsync(new SNewShopUpdateInfoAckMessage
                {
                    Type = ShopResourceType.NewShopUniqueItem,
                    Data = w.ToArray(),
                    Date = version
                });
            }

            using (var w = new BinaryWriter(new MemoryStream()))
            {
                w.Write(new byte[200]);

                session.SendAsync(new SNewShopUpdateInfoAckMessage
                {
                    Type = (ShopResourceType)16,
                    Data = w.ToArray(),
                    Date = version
                });
            }
        }

        [MessageHandler(typeof(CLicensedReqMessage))]
        public void LicensedHandler(GameSession session, CLicensedReqMessage message)
        {
            try
            {
                session.Player.LicenseManager.Acquire(message.License);
            }
            catch (LicenseNotFoundException ex)
            {
                Logger.ForAccount(session)
                    .Error(ex, "Failed to acquire license");
            }
        }

        [MessageHandler(typeof(CExerciseLicenceReqMessage))]
        public void ExerciseLicenseHandler(GameSession session, CExerciseLicenceReqMessage message)
        {
            try
            {
                session.Player.LicenseManager.Acquire(message.License);
            }
            catch (LicenseException ex)
            {
                Logger.ForAccount(session)
                    .Error(ex, "Failed to exercise license");
            }
        }

        [MessageHandler(typeof(CBuyItemReqMessage))]
        public async Task BuyItemHandler(GameSession session, CBuyItemReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();
            var plr = session.Player;

            foreach (var item in message.Items)
            {
                var shopItemInfo = shop.GetItemInfo(item.ItemNumber, item.PriceType);
                if (shopItemInfo == null)
                {
                    Logger.ForAccount(session)
                        .Error("No shop entry found for {item}",
                            new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }
                if (!shopItemInfo.IsEnabled)
                {
                    Logger.ForAccount(session)
                        .Error("Shop entry is not enabled {item}",
                            new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }

                var priceGroup = shopItemInfo.PriceGroup;
                var price = priceGroup.GetPrice(item.PeriodType, item.Period);
                if (price == null)
                {
                    Logger.ForAccount(session)
                        .Error("Invalid price group for shop entry {item}",
                            new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }

                if (!price.IsEnabled)
                {
                    Logger.ForAccount(session)
                        .Error("Shop entry is not enabled {item}",
                            new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }

                if (item.Color > shopItemInfo.ShopItem.ColorGroup)
                {
                    Logger.ForAccount(session)
                        .Error("Shop entry has no color {color} {item}",
                            item.Color, new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }

                if (item.Effect != 0)
                {
                    if (shopItemInfo.EffectGroup.Effects.All(effect => effect.Effect != item.Effect))
                    {
                        Logger.ForAccount(session)
                            .Error("Shop entry has no effect {effect} {item}",
                                item.Effect, new { item.ItemNumber, item.PriceType, item.Period, item.PeriodType });

                        session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                        return;
                    }
                }

                if (shopItemInfo.ShopItem.License != ItemLicense.None &&
                    !plr.LicenseManager.Contains(shopItemInfo.ShopItem.License) &&
                    Config.Instance.Game.EnableLicenseRequirement)
                {
                    Logger.ForAccount(session)
                        .Error("Doesn't have license {license}", shopItemInfo.ShopItem.License);

                    session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.UnkownItem));
                    return;
                }

                // ToDo missing price types
                switch (shopItemInfo.PriceGroup.PriceType)
                {
                    case ItemPriceType.PEN:
                        if (plr.PEN < price.Price)
                        {
                            session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.NotEnoughMoney));
                            return;
                        }

                        plr.PEN -= (uint)price.Price;
                        break;

                    case ItemPriceType.AP:
                    case ItemPriceType.Premium:
                        if (plr.AP < price.Price)
                        {
                            session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.NotEnoughMoney));
                            return;
                        }

                        plr.AP -= (uint)price.Price;
                        break;

                    case ItemPriceType.CP:
                        {
                            var CP = plr.Inventory.FirstOrDefault(p => p.ItemNumber == 6000001);
                            if (CP == null || CP.Count < price.Price)
                            {
                                session.SendAsync(new SBuyItemAckMessage(ItemBuyResult.NotEnoughMoney));
                                return;
                            }

                            CP.Count -= (uint)price.Price;

                            session.SendAsync(new SInventoryActionAckMessage(InventoryAction.Update, CP.Map<PlayerItem, ItemDto>()));
                        }

                        break;

                    default:
                        Logger.ForAccount(session)
                            .Error("Unknown PriceType {priceType}", shopItemInfo.PriceGroup.PriceType);
                        return;
                }

                // ToDo
                //var purchaseDto = new PlayerPurchaseDto
                //{
                //    account_id = (int)plr.Account.Id,
                //    shop_item_id = item.ItemNumber,
                //    shop_item_info_id = shopItemInfo.Id,
                //    shop_price_id = price.Id,
                //    time = DateTimeOffset.Now.ToUnixTimeSeconds()
                //};
                //db.player_purchase.Add(purchaseDto);

                var plrItem = session.Player.Inventory.Create(shopItemInfo, price, item.Color, item.Effect, (uint)(price.PeriodType == ItemPeriodType.Units ? price.Period : 0));

                await session.SendAsync(new SBuyItemAckMessage(new[] { plrItem.Id }, item));
                await session.SendAsync(new SRefreshCashInfoAckMessage(plr.PEN, plr.AP));
            }
        }

        [MessageHandler(typeof(CRandomShopRollingStartReqMessage))]
        public void RandomShopRollHandler(GameSession session, CRandomShopRollingStartReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();
            var itemList = shop.Items;
            var effectList = shop.Effects;
            var plr = session.Player;
            var rand = new Random();
            Shop.ShopItem item = null;

            var PEN = message.IsWeapon ? 3000 : 800;
            var rnd = plr.RandomShop[message.IsWeapon ? 1 : 0];

            if (plr.PEN < PEN)
            {
                session.SendAsync(new SRandomShopItemInfoAckMessage
                {
                    Item = new RandomShopItemDto()
                });
            }

            plr.PEN -= (uint)PEN;

            session.SendAsync(new SRefreshCashInfoAckMessage { PEN = plr.PEN, AP = plr.AP });

        roll:
            if (message.StopItem == 0)
            {
                var bag = new List<Shop.ShopItem>();
                ItemCategory cat;

                if (message.IsWeapon)
                    cat = ItemCategory.Weapon;
                else
                    cat = ItemCategory.Costume;

                bag = (from it in itemList
                        where it.Value.ItemNumber.Category == cat
                        select it.Value).ToList();

                item = bag.ElementAt(rand.Next(bag.Count));
                rnd.Item = item.ItemNumber;
            }
            else
            {
                item = itemList[rnd.Item];
            }

            var infos = item.ItemInfos.ElementAtOrDefault(rand.Next(item.ItemInfos.Count));
            if (infos == null)
                goto roll;

            var prices = infos.PriceGroup.Prices;
            var price = prices.ElementAt(rand.Next(prices.Count));

            if (message.StopEffect == 0)
            {
                var effects = infos.EffectGroup.Effects;
                var effect = effects.ElementAt(rand.Next(effects.Count));
                rnd.Effect = effect.Effect;
            }

            rnd.PriceType = infos.PriceGroup.PriceType;
            rnd.PeriodType = price.PeriodType;
            rnd.Period = price.Period;

            session.SendAsync(new SRandomShopItemInfoAckMessage
            {
                Item = new RandomShopItemDto
                {
                    Unk1 = (uint)(message.IsWeapon ? 1 : 0),
                    ItemNumber = rnd.Item,
                    Effect = rnd.Effect,
                    PeriodType = rnd.PeriodType,
                    Period = rnd.Period
                }
            });
        }

        [MessageHandler(typeof(CRandomShopItemSaleReqMessage))]
        public void RandomShopItemSaleHandler(GameSession session, CRandomShopItemSaleReqMessage message)
        {
            var shop = GameServer.Instance.ResourceCache.GetShop();

            session.SendAsync(new SRandomShopItemInfoAckMessage
            {
                Item = new RandomShopItemDto
                {
                    Unk1 = message.Unk
                }
            });
        }

        [MessageHandler(typeof(CRandomShopItemGetReqMessage))]
        public void RandomShopItemGet(GameSession session, CRandomShopItemGetReqMessage message)
        {
            var plr = session.Player;
            var rnd = plr.RandomShop[message.Unk];
            plr.Inventory.Create(rnd.Item, rnd.PriceType, rnd.PeriodType, rnd.Period, (byte)rnd.color, rnd.Effect, 0);

            session.SendAsync(new SRandomShopItemInfoAckMessage
            {
                Item = new RandomShopItemDto
                {
                    Unk1 = message.Unk
                }
            });
        }
    }
}
