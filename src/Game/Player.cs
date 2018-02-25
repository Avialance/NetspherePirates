using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper.FastCrud;
using ExpressMapper.Extensions;
using Netsphere.Database;
using Netsphere.Database.Game;
using Netsphere.Network;
using Netsphere.Network.Data.Chat;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Chat;
using Netsphere.Network.Message.Game;
using Serilog;
using Serilog.Core;

namespace Netsphere
{
    internal class Player
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(Player));
        private byte _tutorialState;
        private byte _level;
        private uint _totalExperience;
        private uint _pen;
        private uint _ap;
        private uint _coins1;
        private uint _coins2;

        #region Properties

        internal bool NeedsToSave { get; set; }

        public GameSession Session { get; set; }
        public ChatSession ChatSession { get; set; }
        public RelaySession RelaySession { get; set; }

        public PlayerSettingManager Settings { get; }

        public DenyManager DenyManager { get; }
        public Mailbox Mailbox { get; }

        public Account Account { get; set; }
        public LicenseManager LicenseManager { get; }
        public CharacterManager CharacterManager { get; }
        public Inventory Inventory { get; }
        public Channel Channel { get; internal set; }

        public Room Room { get; internal set; }
        public PlayerRoomInfo RoomInfo { get; }

        public WeeklyMission Mission { get; set; }

        public DMStats DeathMatch { get; set; }
        public TDStats TouchDown { get; set; }
        public ChaserStats Chasser { get; set; }
        public BRStats BattleRoyal { get; set; }
        public CPTStats CaptainMode { get; set; }

        internal bool SentPlayerList { get; set; }

        public byte TutorialState
        {
            get { return _tutorialState; }
            set
            {
                if (_tutorialState == value)
                    return;
                _tutorialState = value;
                NeedsToSave = true;
            }
        }
        public byte Level
        {
            get { return _level; }
            set
            {
                if (_level == value)
                    return;
                _level = value;
                NeedsToSave = true;
            }
        }
        public uint TotalExperience
        {
            get { return _totalExperience; }
            set
            {
                if (_totalExperience == value)
                    return;
                _totalExperience = value;
                NeedsToSave = true;
            }
        }
        public uint PEN
        {
            get { return _pen; }
            set
            {
                if (_pen == value)
                    return;
                _pen = value;
                NeedsToSave = true;
            }
        }
        public uint AP
        {
            get { return _ap; }
            set
            {
                if (_ap == value)
                    return;
                _ap = value;
                NeedsToSave = true;
            }
        }
        public uint Coins1
        {
            get { return _coins1; }
            set
            {
                if (_coins1 == value)
                    return;
                _coins1 = value;
                NeedsToSave = true;
            }
        }
        public uint Coins2
        {
            get { return _coins2; }
            set
            {
                if (_coins2 == value)
                    return;
                _coins2 = value;
                NeedsToSave = true;
            }
        }

        #endregion

        public Player(GameSession session, Account account, PlayerDto dto)
        {
            Session = session;
            Account = account;
            _tutorialState = dto.TutorialState;
            _level = dto.Level;
            _totalExperience = (uint)dto.TotalExperience;
            _pen = (uint)dto.PEN;
            _ap = (uint)dto.AP;
            _coins1 = (uint)dto.Coins1;
            _coins2 = (uint)dto.Coins2;

            Settings = new PlayerSettingManager(this, dto);
            DenyManager = new DenyManager(this, dto);
            Mailbox = new Mailbox(this, dto);

            LicenseManager = new LicenseManager(this, dto);
            Inventory = new Inventory(this, dto);
            CharacterManager = new CharacterManager(this, dto);

            RoomInfo = new PlayerRoomInfo();

            DeathMatch = new DMStats(this, dto);
            TouchDown = new TDStats(this, dto);
            Chasser = new ChaserStats(this, dto);
            BattleRoyal = new BRStats(this, dto);
            CaptainMode = new CPTStats(this, dto);

            Mission = new WeeklyMission(this, dto);
        }

        /// <summary>
        /// Gains experiences and levels up if the player earned enough experience
        /// </summary>
        /// <param name="amount">Amount of experience to earn</param>
        /// <returns>true if the player leveled up</returns>
        public bool GainExp(uint amount)
        {
            Logger.ForAccount(this)
                .Debug("Gained {amount} exp", amount);

            var expTable = GameServer.Instance.ResourceCache.GetExperience();
            var expInfo = expTable.GetValueOrDefault(Level);
            if (expInfo == null)
            {
                Logger.ForAccount(this)
                    .Warning("Level {level} not found", Level);

                return false;
            }

            // We cant earn exp when we reached max level
            if (expInfo.ExperienceToNextLevel == 0 || Level >= Config.Instance.Game.MaxLevel)
                return false;

            var leveledUp = false;
            TotalExperience += amount;

            // Did we level up?
            // Using a loop for multiple level ups
            while (expInfo.ExperienceToNextLevel != 0 &&
                expInfo.ExperienceToNextLevel <= (int)(TotalExperience - expInfo.TotalExperience))
            {
                var newLevel = Level + 1;
                expInfo = expTable.GetValueOrDefault(newLevel);

                if (expInfo == null)
                {
                    Logger.ForAccount(this)
                        .Warning("Can't level up because level {level} not found", newLevel);
                    break;
                }

                Logger.ForAccount(this)
                    .Debug("Leveled up to {level}", newLevel);

                // ToDo level rewards

                Level++;
                leveledUp = true;
            }

            if (!leveledUp)
                return false;

            Channel?.Broadcast(new SUserDataAckMessage(this.Map<Player, UserDataDto>()));

            // ToDo Do we need to update inside rooms too?

            // ToDo Do we need this?
            //await Session.SendAsync(new SBeginAccountInfoAckMessage())
            //    .ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Gets the maximum hp for the current character
        /// </summary>
        public float GetMaxHP()
        {
            return GameServer.Instance.ResourceCache.GetGameTempos()["GAMETEMPO_FREE"].ActorDefaultHPMax +
                   GetAttributeValue(Attribute.HP);
        }

        public float GetChaserRate()
        {
            return GetAttributeRate(Attribute.ChaserCastRate);
        }

        public float GetChaserMoveSpeed()
        {
            return GetAttributeValue(Attribute.ChaserMovespeed);
        }

        /// <summary>
        /// Gets the total attribute value for the current character
        /// </summary>
        /// <param name="attribute">The attribute to retrieve</param>
        /// <returns></returns>
        public int GetAttributeValue(Attribute attribute)
        {
            if (CharacterManager.CurrentCharacter == null)
                return 0;

            var @char = CharacterManager.CurrentCharacter;
            var value = GetAttributeValueFromItems(attribute, @char.Weapons.GetItems());
            value += GetAttributeValueFromItems(attribute, @char.Skills.GetItems());
            value += GetAttributeValueFromItems(attribute, @char.Costumes.GetItems());

            return value;
        }

        /// <summary>
        /// Gets the total attribute rate for the current character
        /// </summary>
        /// <param name="attribute">The attribute to retrieve</param>
        /// <returns></returns>
        public float GetAttributeRate(Attribute attribute)
        {
            if (CharacterManager.CurrentCharacter == null)
                return 0;

            var @char = CharacterManager.CurrentCharacter;
            var value = GetAttributeRateFromItems(attribute, @char.Weapons.GetItems());
            value += GetAttributeRateFromItems(attribute, @char.Skills.GetItems());
            value += GetAttributeRateFromItems(attribute, @char.Costumes.GetItems());

            return value;
        }

        /// <summary>
        /// Sends a message to the game master console
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendConsoleMessage(string message)
        {
            Session.SendAsync(new SAdminActionAckMessage { Result = 1, Message = message });
        }

        /// <summary>
        /// Sends a notice message
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendNotice(string message)
        {
            Session.SendAsync(new SNoticeMessageAckMessage(message));
        }

        /// <summary>
        /// Saves all pending changes to the database
        /// </summary>
        public void Save()
        {
            using (var db = GameDatabase.Open())
            {
                if (NeedsToSave)
                {
                    db.Update(new PlayerDto
                    {
                        Id = (int)Account.Id,
                        TutorialState = TutorialState,
                        Level = Level,
                        TotalExperience = (int)TotalExperience,
                        PEN = (int)PEN,
                        AP = (int)AP,
                        Coins1 = (int)Coins1,
                        Coins2 = (int)Coins2,
                        CurrentCharacterSlot = CharacterManager.CurrentSlot
                    });
                    NeedsToSave = false;
                }

                Settings.Save(db);
                Inventory.Save(db);
                CharacterManager.Save(db);
                LicenseManager.Save(db);
                DenyManager.Save(db);
                Mailbox.Save(db);

                DeathMatch.Save(db);
                TouchDown.Save(db);
                Chasser.Save(db);
                BattleRoyal.Save(db);
                CaptainMode.Save(db);
                Mission.Save(db);
            }
        }

        /// <summary>
        /// Disconnects the player
        /// </summary>
        public void Disconnect()
        {
            Session?.Dispose();
        }

        private static int GetAttributeValueFromItems(Attribute attribute, IEnumerable<PlayerItem> items)
        {
            return items.Where(item => item != null)
                .Select(item => item.GetItemEffect())
                .Where(effect => effect != null)
                .SelectMany(effect => effect.Attributes)
                .Where(attrib => attrib.Attribute == attribute)
                .Sum(attrib => attrib.Value);
        }

        private static float GetAttributeRateFromItems(Attribute attribute, IEnumerable<PlayerItem> items)
        {
            return items.Where(item => item != null)
                .Select(item => item.GetItemEffect())
                .Where(effect => effect != null)
                .SelectMany(effect => effect.Attributes)
                .Where(attrib => attrib.Attribute == attribute)
                .Sum(attrib => attrib.Rate);
        }
    }

    internal class DMStats
    {
        public Player Player { get; set; }
        public ulong Won { get; set; }
        public ulong Loss { get; set; }
        public ulong Kills { get; set; }
        public ulong KillAssists { get; set; }
        public ulong Deaths { get; set; }
        public float KDRate => Deaths > 0 ? ((Kills * 2) + KillAssists) / (Deaths * 2) : 1.0f;
        private bool _existsInDatabase;

        public DMStats(Player player, PlayerDto playerDto)
        {
            Player = player;
            var dm = playerDto.DeathMatchInfo.FirstOrDefault();
            _existsInDatabase = false;
            if (dm != null)
            {
                _existsInDatabase = true;
                Won = dm.Won;
                Loss = dm.Loss;
                Kills = dm.Kills;
                KillAssists = dm.KillAssists;
                Deaths = dm.Deaths;
            }
        }

        public void Save(IDbConnection db)
        {
            var update = new PlayerDeathMatchDto
            {
                PlayerId = (int)Player.Account.Id,
                Won = Won,
                Loss = Loss,
                Kills = Kills,
                KillAssists = KillAssists,
                Deaths = Deaths
            };

            if (_existsInDatabase)
            {
                db.Update(update);
            }
            else
            {
                _existsInDatabase = true;
                db.Insert(update);
            }
        }

        public DMStatsDto GetStatsDto()
        {
            return new DMStatsDto
            {
                Won = (uint)Won,
                Lost = (uint)Loss,
                Kills = (uint)Kills,
                KillAssists = (uint)KillAssists,
                Deaths = (uint)Deaths
            };
        }
    }

    internal class TDStats
    {
        private bool _existsInDatabase;

        public Player Player { get; set; }
        public ulong Won { get; set; }
        public ulong Loss { get; set; }
        public ulong TD { get; set; }
        public ulong TDAssist { get; set; }
        public ulong Offense { get; set; }
        public ulong OffenseAssist { get; set; }
        public ulong OffenseRebound { get; set; }
        public ulong Defense { get; set; }
        public ulong DefenseAssist { get; set; }
        public ulong Kill { get; set; }
        public ulong KillAssists { get; set; }
        public ulong TDHeal { get; set; }
        public ulong Heal { get; set; }

        public TDStats(Player player, PlayerDto playerDto)
        {
            Player = player;
            var td = playerDto.TouchDownInfo.FirstOrDefault();
            _existsInDatabase = false;

            if (td != null)
            {
                _existsInDatabase = true;
                Won = td.Won;
                Loss = td.Loss;
                TD = td.TD;
                TDAssist = td.TDAssist;
                Offense = td.Offense;
                OffenseAssist = td.OffenseAssist;
                Defense = td.Defense;
                DefenseAssist = td.DefenseAssist;
                Kill = td.Kill;
                KillAssists = td.KillAssist;
                TDHeal = td.TDHeal;
                Heal = td.Heal;
            }
        }

        public void Save(IDbConnection db)
        {
            var update = new PlayerTouchDownDto
            {
                PlayerId = (int)Player.Account.Id,
                Won = Won,
                Loss = Loss,
                TD = TD,
                TDAssist = TDAssist,
                Offense = Offense,
                OffenseAssist = OffenseAssist,
                Defense = Defense,
                DefenseAssist = DefenseAssist,
                Kill = Kill,
                KillAssist = KillAssists,
                TDHeal = TDHeal,
                Heal = Heal
            };
            if (_existsInDatabase)
            {
                db.Update(update);
            }
            else
            {
                db.Insert(update);
                _existsInDatabase = true;
            }
        }

        public TDStatsDto GetStatsDto()
        {
            return new TDStatsDto
            {
                Unk1 = (uint)Won,//Win
                Unk2 = (uint)Loss,//Loss
                Unk3 = (uint)TD,
                Unk4 = 0, // Unk3 * 20/unk4
                Unk5 = (uint)TDAssist, // TDAssits
                Unk6 = (uint)Kill,//Kill
                Unk7 = (uint)KillAssists,//KillAssists
                Unk8 = (uint)Defense,//DefenseScore
                Unk9 = (uint)DefenseAssist,//Defense Assists
                Unk10 = (uint)Offense,//Offense
                Unk11 = (uint)OffenseAssist,//Offensive Assis
                Unk12 = (uint)TDHeal,//Healx2
                Unk13 = (uint)Heal,//Heal x1
                Unk14 = 0,
                Unk15 = 0,
                Unk16 = 0,
                Unk17 = 0,
                Unk18 = 0
            };
        }
    }

    internal class ChaserStats
    {
        private bool _existsInDatabase;

        public Player Player { get; set; }
        public ulong ChasedWon { get; set; }
        public ulong ChasedRounds { get; set; }
        public ulong ChaserWon { get; set; }
        public ulong ChaserRounds { get; set; }

        public ChaserStats(Player player, PlayerDto playerDto)
        {
            Player = player;
            var chs = playerDto.ChaserInfo.FirstOrDefault();
            _existsInDatabase = false;

            if (chs != null)
            {
                _existsInDatabase = true;
                ChasedWon = chs.ChasedWon;
                ChasedRounds = chs.ChasedRounds;
                ChaserWon = chs.ChaserWon;
                ChaserRounds = chs.ChasedRounds;
            }
        }

        public void Save(IDbConnection db)
        {
            var update = new PlayerChaserDto
            {
                PlayerId = (int)Player.Account.Id,
                ChasedRounds = ChasedRounds,
                ChasedWon = ChasedWon,
                ChaserRounds = ChaserRounds,
                ChaserWon = ChaserWon
            };
            if (_existsInDatabase)
            {
                db.Update(update);
            }
            else
            {
                _existsInDatabase = true;
                db.Insert(update);
            }
        }

        public ChaserStatsDto GetStatsDto()
        {
            return new ChaserStatsDto
            {
                ChasedRounds = (uint)ChasedRounds,
                ChasedWon = (uint)ChasedWon,
                ChaserRounds = (uint)ChaserRounds,
                ChaserWon = (uint)ChaserWon
            };
        }
    }

    internal class BRStats
    {
        private bool _existsInDatabase;

        public Player Player { get; set; }
        public ulong Won { get; set; }
        public ulong Loss { get; set; }
        public ulong FirstKilled { get; set; }
        public ulong FirstPlace { get; set; }

        public BRStats(Player player, PlayerDto playerDto)
        {
            Player = player;
            var br = playerDto.BattleRoyalInfo.FirstOrDefault();
            _existsInDatabase = false;

            if (br != null)
            {
                _existsInDatabase = true;
                Won = br.Won;
                Loss = br.Loss;
                FirstKilled = br.FirstKilled;
                FirstPlace = br.FirstPlace;
            }
        }

        public void Save(IDbConnection db)
        {
            var update = new PlayerBattleRoyalDto
            {
                PlayerId = (int)Player.Account.Id,
                Won = Won,
                Loss = Loss,
                FirstKilled = FirstKilled,
                FirstPlace = FirstPlace
            };

            if (_existsInDatabase)
            {
                db.Update(update);
            }
            else
            {
                db.Insert(update);
                _existsInDatabase = true;
            }
        }

        public BRStatsDto GetStatsDto()
        {
            return new BRStatsDto
            {
                Won = (uint)Won,
                Lost = (uint)Loss,
                FirstKilled = (uint)FirstKilled,
                FirstPlace = (uint)FirstPlace
            };
        }
    }

    internal class CPTStats
    {
        private bool _existsInDatabase;

        public Player Player { get; set; }
        public ulong Won { get; set; }
        public ulong Loss { get; set; }
        public ulong CPTKilled { get; set; }
        public ulong CPTCount { get; set; }

        public CPTStats(Player player, PlayerDto playerDto)
        {
            Player = player;
            var cpt = playerDto.CaptainInfo.FirstOrDefault();
            _existsInDatabase = false;

            if (cpt != null)
            {
                _existsInDatabase = true;
                Won = cpt.Won;
                Loss = cpt.Loss;
                CPTKilled = cpt.CPTKilled;
                CPTCount = cpt.CPTCount;
            }
        }

        public void Save(IDbConnection db)
        {
            var update = new PlayerCaptainDto
            {
                PlayerId = (int)Player.Account.Id,
                Won = Won,
                Loss = Loss,
                CPTCount = CPTCount,
                CPTKilled = CPTKilled
            };

            if (_existsInDatabase)
            {
                db.Update(update);
            }
            else
            {
                db.Insert(update);
                _existsInDatabase = true;
            }
        }

        public CPTStatsDto GetStatsDto()
        {
            return new CPTStatsDto
            {
                Won = (uint)Won,
                Lost = (uint)Loss,
                Captain = (uint)CPTCount,
                CaptainKilled = (uint)CPTCount
            };
        }
    }
}
