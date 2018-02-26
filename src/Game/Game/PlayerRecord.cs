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
                var expGain = GetExpGain(out bonusExp);
                w.Write(GetPenGain(out bonusPen));

                Player.Mission.Commit(out MissionEXP);

                rankUp = Player.GainExp(expGain + MissionEXP);
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
                2 PEN+
                4 EXP+
                8 20%
                16 25%
                32 30%
            */
            w.Write(0);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
        }
    }
}
