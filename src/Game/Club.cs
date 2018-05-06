namespace Netsphere
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dapper.FastCrud;
    using Netsphere.Database;
    using Netsphere.Database.Auth;
    using Netsphere.Database.Game;
    using Netsphere.Network;
    using Netsphere.Network.Data.Game;

    internal class Club
    {
        public static Club Instance { get; private set; }
        private Dictionary<Player, PlayerClubInfoDto> _ClubInfo;

        public Club()
        {
            _ClubInfo = new Dictionary<Player, PlayerClubInfoDto>();
        }

        public static void Initialize()
        {
            if (Instance != null)
                throw new InvalidOperationException("Club system already started");

            Instance = new Club();
        }

        public PlayerClubInfoDto LoadClubInfo(Player plr)
        {
            _ClubInfo[plr] = null;

            using (var db = GameDatabase.Open())
            using (var adb = AuthDatabase.Open())
            {
                var member = db.Find<ClubMembersDto>(smtp => smtp
                .Where($"{nameof(ClubMembersDto.Id):C} = @Player")
                .WithParameters(new { Player = plr.Account.Id })
                ).FirstOrDefault();

                if (member == null)
                    return null;

                var Club = db.Find<ClubDto>(smtp => smtp
                .Where($"{nameof(ClubDto.Id):C} = @Id")
                .WithParameters(new { Id = member.ClubId })
                ).FirstOrDefault();

                if (Club == null)
                    throw new InvalidOperationException("Club don't exist");

                var Moderator = adb.Find<AccountDto>(stmp => stmp
                            .Include<NicknameHistoryDto>(join => join.LeftOuterJoin())
                            .Where($"{nameof(AccountDto.Id):C} = @Id")
                            .WithParameters(new { Id = Club.Moderator })
                            ).FirstOrDefault();

                var hasNickNameChanger = from nick in Moderator.NicknameHistory
                                         where nick.ExpireDate == -1 || nick.ExpireDate > DateTimeOffset.Now.ToUnixTimeSeconds()
                                         orderby nick.Id descending
                                         select nick.Nickname;

                if (hasNickNameChanger.Any())
                    Moderator.Nickname = hasNickNameChanger.First();

                _ClubInfo[plr] = new PlayerClubInfoDto
                {
                    Unk1 = member.State,
                    Unk2 = 2,
                    Unk3 = 3,
                    Unk4 = 4,
                    Unk5 = 5,
                    Unk6 = 6,
                    Unk7 = "Cadena1",
                    Unk8 = "Cadena2",
                    Unk11 = "Cadena3",
                    Unk12 = Club.Notice,
                    ClanName = Club.Name,
                    ClanMark = Club.Mark,
                    ModeratorName = Moderator.Nickname
                };

                return _ClubInfo[plr];
            }
        }

        public PlayerClubInfoDto GetClubInfo(Player plr)
        {
            return _ClubInfo[plr];
        }
    }
}
