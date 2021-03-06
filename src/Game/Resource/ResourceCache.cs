using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlubLib.Caching;
using Serilog;
using Serilog.Core;

namespace Netsphere.Resource
{
    internal class ResourceCache
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(ResourceCache));
        private readonly ResourceLoader _loader;
        private readonly ICache _cache = new MemoryCache();

        public ResourceCache()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "data");
            _loader = new ResourceLoader(path);
        }

        public void PreCache()
        {
            Logger.Information("Caching: Channels");
            GetChannels();

            Logger.Information("Caching: Effects");
            GetEffects();

            Logger.Information("Caching: Mission");
            GetTasks();

            Logger.Information("Caching: Items");
            GetItems();

            Logger.Information("Caching: DefaultItems");
            GetDefaultItems();

            Logger.Information("Caching: Shop");
            GetShop();

            Logger.Information("Caching: Experience");
            GetExperience();

            Logger.Information("Caching: Maps");
            GetMaps();

            Logger.Information("Caching: GameTempos");
            GetGameTempos();

            Logger.Information("Caching: Level Rewards");
            GetLevelRewards();

            Logger.Information("Caching: Item Rewards");
            GetItemRewards();
        }

        public IReadOnlyList<ChannelInfo> GetChannels()
        {
            var value = _cache.Get<IReadOnlyList<ChannelInfo>>(ResourceCacheType.Channels);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadChannels().ToList();
                _cache.Set(ResourceCacheType.Channels, value);
            }
            return value;
        }

        public IReadOnlyDictionary<uint, ItemEffect> GetEffects()
        {
            var value = _cache.Get<IReadOnlyDictionary<uint, ItemEffect>>(ResourceCacheType.Effects);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadEffects().ToDictionary(effect => effect.Id);
                _cache.Set(ResourceCacheType.Effects, value);
            }

            return value;
        }

        public IReadOnlyDictionary<uint, TaskInfo> GetTasks()
        {
            var value = _cache.Get<IReadOnlyDictionary<uint, TaskInfo>>(ResourceCacheType.Tasks);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadTaskList().ToDictionary(task => task.Id);
                _cache.Set(ResourceCacheType.Tasks, value);
            }

            return value;
        }

        public IReadOnlyDictionary<ItemNumber, ItemInfo> GetItems()
        {
            var value = _cache.Get<IReadOnlyDictionary<ItemNumber, ItemInfo>>(ResourceCacheType.Items);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadItems().ToDictionary(item => item.ItemNumber);
                _cache.Set(ResourceCacheType.Items, value);
            }

            return value;
        }

        public IReadOnlyList<DefaultItem> GetDefaultItems()
        {
            var value = _cache.Get<IReadOnlyList<DefaultItem>>(ResourceCacheType.DefaultItems);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadDefaultItems().ToList();
                _cache.Set(ResourceCacheType.DefaultItems, value);
            }

            return value;
        }

        public ShopResources GetShop()
        {
            var value = _cache.Get<ShopResources>(ResourceCacheType.Shop);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = new ShopResources();
                _cache.Set(ResourceCacheType.Shop, value);
            }
            if (string.IsNullOrWhiteSpace(value.Version))
                value.Load();

            return value;
        }

        public IReadOnlyDictionary<int, Experience> GetExperience()
        {
            var value = _cache.Get<IReadOnlyDictionary<int, Experience>>(ResourceCacheType.Exp);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadExperience().ToDictionary(e => e.Level);
                _cache.Set(ResourceCacheType.Exp, value);
            }

            return value;
        }

        public IReadOnlyDictionary<byte, MapInfo> GetMaps()
        {
            var value = _cache.Get<IReadOnlyDictionary<byte, MapInfo>>(ResourceCacheType.Maps);
            if (value == null)
            {
                Logger.Debug("Caching...");
                value = _loader.LoadMaps().ToDictionary(maps => maps.Id);
                _cache.Set(ResourceCacheType.Maps, value);
            }

            return value;
        }

        public IReadOnlyDictionary<string, GameTempo> GetGameTempos()
        {
            var value = _cache.Get<IReadOnlyDictionary<string, GameTempo>>(ResourceCacheType.GameTempo);
            if (value == null)
            {
                Logger.Debug("Caching...");

                value = _loader.LoadGameTempos().ToDictionary(t => t.Name);
                _cache.Set(ResourceCacheType.GameTempo, value);
            }

            return value;
        }

        public IReadOnlyDictionary<byte, LevelReward> GetLevelRewards()
        {
            var value = _cache.Get<IReadOnlyDictionary<byte, LevelReward>>(ResourceCacheType.LevelRewards);
            if (value == null)
            {
                Logger.Debug("Caching...");

                value = _loader.LoadLevelRewards().ToDictionary(t => t.Level);
                _cache.Set(ResourceCacheType.LevelRewards, value);
            }

            return value;
        }

        public IReadOnlyDictionary<ulong, CapsuleReward> GetItemRewards()
        {
            var value = _cache.Get<IReadOnlyDictionary<ulong, CapsuleReward>>(ResourceCacheType.ItemRewards);
            if (value == null)
            {
                Logger.Debug("Caching...");

                value = _loader.LoadItemRewards().ToDictionary(t => (ulong)t.Item);
                _cache.Set(ResourceCacheType.ItemRewards, value);
            }

            return value;
        }

        public void Clear()
        {
            Logger.Debug("Clearing cache");
            _cache.Clear();
        }

        public void Clear(ResourceCacheType type)
        {
            Logger.Debug($"Clearing cache for {type}");

            if (type == ResourceCacheType.Shop)
            {
                GetShop().Clear();
                return;
            }
            _cache.Remove(type.ToString());
        }
    }

    internal static class ResourceCacheExtensions
    {
        public static T Get<T>(this ICache cache, ResourceCacheType type)
            where T : class
        {
            return cache.Get<T>(type.ToString());
        }

        public static void Set(this ICache cache, ResourceCacheType type, object value)
        {
            cache.Set(type.ToString(), value);
        }

        public static void Set(this ICache cache, ResourceCacheType type, object value, TimeSpan ts)
        {
            cache.Set(type.ToString(), value, ts);
        }
    }
}
