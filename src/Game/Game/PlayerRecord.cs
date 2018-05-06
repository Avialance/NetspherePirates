using System.IO;
using System.Linq;
using BlubLib.IO;

// ReSharper disable once CheckNamespace
namespace Netsphere.Game
{
    internal abstract class PlayerRecord
    {
        public Player Player { get; }
        public abstract uint TotalScore { get; }
        public uint Kills { get; set; }
        public uint KillAssists { get; set; }
        public uint Suicides { get; set; }
        public uint Deaths { get; set; }
        public long PENLC { get; set; }
        public long EXPLC { get; set; }

        protected PlayerRecord(Player player)
        {
            Player = player;
        }

        public virtual uint GetPenGain(out uint bonusPen)
        {
            bonusPen = 0;
            return 0;
        }

        public virtual uint GetExpGain(out uint bonusExp)
        {
            bonusExp = 0;
            return 0;
        }

        public uint GetPenGain(ExperienceRates ExpRates, out uint bonusPen)
        {
            var exp = GetExpGain(ExpRates, out var bonusExp);
            exp -= bonusExp;

            var pen = exp;
            bonusPen = (uint)(pen * Player.GetPenRate());

            return pen + bonusPen;
        }

        public uint GetExpGain(ExperienceRates ExpRates, out uint bonusExp)
        {
            var place = 1;
            var plrs = Player.Room.TeamManager.Players
                .Where(plr => plr.RoomInfo.State == PlayerState.Waiting &&
                    plr.RoomInfo.Mode == PlayerGameMode.Normal)
                .ToArray();

            foreach (var plr in plrs.OrderByDescending(plr => plr.RoomInfo.Stats.TotalScore))
            {
                if (plr == Player)
                    break;

                place++;
                if (place > 3)
                    break;
            }

            var rankingBonus = 1.0f;
            switch (place)
            {
                case 1:
                    rankingBonus += ExpRates.FirstPlaceBonus / 100.0f;
                    break;

                case 2:
                    rankingBonus += ExpRates.SecondPlaceBonus / 100.0f;
                    break;

                case 3:
                    rankingBonus += ExpRates.ThirdPlaceBonus / 100.0f;
                    break;
            }

            var TimeExp = ExpRates.ExpPerMin * Player.RoomInfo.PlayTime.Minutes;
            var PlayersExp = plrs.Length * ExpRates.PlayerCountFactor;
            var ScoreExp = ExpRates.ExpPerMin * TotalScore;

            var ExpGained = (TimeExp + PlayersExp + ScoreExp) * rankingBonus;

            bonusExp = (uint)(ExpGained * Player.GetExpRate());

            return (uint)ExpGained + bonusExp;
        }

        public virtual void Reset()
        {
            Kills = 0;
            KillAssists = 0;
            Suicides = 0;
            Deaths = 0;
        }

        public virtual void Serialize(BinaryWriter w, bool isResult)
        {
            w.Write(Player.Account.Id);
            w.WriteEnum(Player.RoomInfo.Team.Team);
            w.WriteEnum(Player.RoomInfo.State);
            w.Write(Player.RoomInfo.IsReady);
            w.Write((uint)Player.RoomInfo.Mode);
            w.Write(TotalScore);
            w.Write(0);

            uint bonusPen = 0;
            uint bonusExp = 0;
            uint MissionEXP = 0;
            var rankUp = false;
            if (isResult)
            {
                var expPlus = Player.CharacterManager.Boosts.GetExpRate() + 1.0f;
                var penPlus = Player.CharacterManager.Boosts.GetPenRate() + 1.0f;
                var expGain = GetExpGain(out bonusExp);
                var penGain = GetPenGain(out bonusPen);

                Player.Mission.Commit(out MissionEXP);

                Player.PEN += (uint)(penGain * penPlus);
                rankUp = Player.GainExp((uint)(expGain * expPlus) + MissionEXP);
                w.Write(penGain);
                w.Write(expGain);
            }
            else
            {
                w.Write(0);
                w.Write(0);
            }

            w.Write(Player.TotalExperience);
            w.Write(rankUp);
            w.Write(bonusExp);
            w.Write(bonusPen);
            w.Write(0);

            /*
                1 PC Room(korean internet cafe event)
                2 PEN+ Boost Item
                4 EXP+ Boost Item
                8 PEN 20%
                16 PEN 25%
                32 PEN 30%
            */
            //w.Write(Player.CharacterManager.Boosts.GetBoostType() + 8 + 1);
            w.Write(Player.CharacterManager.Boosts.GetBoostType());
            w.Write((byte)1);
            w.Write((byte)2);
            w.Write((byte)3);
            w.Write(4);
            w.Write(5);
            w.Write(6);
            w.Write(7);
        }
    }
}
