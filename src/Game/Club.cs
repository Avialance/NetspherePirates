using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Netsphere.Database;
using Netsphere.Network;
using Netsphere.Network.Data.Game;
using Dapper.FastCrud;
using Netsphere.Database.Game;
using System.Threading.Tasks;
using Netsphere.Database.Auth;

namespace Netsphere
{
    internal class Club
    {
        public static Club Instance { get; private set; }
        public static void Initialize()
        {
            if (Instance != null)
                throw new InvalidOperationException("Club system already started");

            Instance = new Club();
        }

        public PlayerClubInfoDto ClubInfo(Player plr)
        {
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
                            .Where($"{nameof(AccountDto.Id):C} = @Id")
                            .WithParameters(new { Id = Club.Moderator })
                            ).FirstOrDefault().Nickname;

                return new PlayerClubInfoDto
                {
                    ClanName = Club.Name,
                    ClanMark = Club.Mark,
                    ModeratorName = Moderator
                };
            }
        }
    }
}
