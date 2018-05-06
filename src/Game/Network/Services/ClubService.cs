using BlubLib.DotNetty.Handlers.MessageHandling;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using ProudNet.Handlers;
using Serilog;
using Serilog.Core;

namespace Netsphere.Network.Services
{
    internal class ClubService : ProudMessageHandler
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(ClubService));

        [MessageHandler(typeof(CClubAddressReqMessage))]
        public void CClubAddressReq(GameSession session, CClubAddressReqMessage message)
        {
            Logger.ForAccount(session)
                .Debug($"ClubAddressReq: {message.RequestId} {message.LanguageId} {message.Command}");

            session.SendAsync(new SClubAddressAckMessage("Kappa", 123));
        }

        [MessageHandler(typeof(CClubInfoReqMessage))]
        public void CClubInfoReq(GameSession session)
        {
            Logger.ForAccount(session)
                .Debug("CClubInfoReq");
            session.SendAsync(new SClubInfoAckMessage
            {
                ClubInfo = Club.Instance.GetClubInfo(session.Player)
            });
        }

        [MessageHandler(typeof(CClubHistoryReqMessage))]
        public void CClubHistoryReq(GameSession session)
        {
            session.SendAsync(new SClubHistoryAckMessage
            {
                History = new ClubHistoryDto
                {
                    Unk1 = 1,
                    Unk2 = 2,
                    Unk3 = "aaaaa",
                    Unk4 = "bbbbb",
                    Unk5 = "ccccc",
                    Unk6 = "ddddd",
                    Unk7 = "eeeee",
                    Unk8 = "fffff"
                }
            });
        }

        [MessageHandler(typeof(CClubJoinReqMessage))]
        public void CClubJoinReq(GameSession session, CClubJoinReqMessage message)
        { }

        [MessageHandler(typeof(CClubNoticeChangeReqMessage))]
        public void CClubNoticeChangeReq(GameSession session, CClubNoticeChangeReqMessage message)
        { }

        [MessageHandler(typeof(CClubUnJoinReqMessage))]
        public void CClubUnJoinReq(GameSession session, CClubUnJoinReqMessage message)
        { }

        [MessageHandler(typeof(CGetClubInfoByNameReqMessage))]
        public void CGetClubInfoByNameReq(GameSession session, CGetClubInfoByNameReqMessage message)
        {
            session.SendAsync(new SGetClubInfoAckMessage
            {
                ClubInfo = new ClubInfoDto
                {
                    Unk1 = "aaaa",
                    Unk2 = "bbbb",
                    Unk3 = "cccc",
                    Unk4 = "dddd",
                    Unk5 = "ffff",
                    Unk6 = 6,
                    Unk7 = 7,
                    Unk8 = 8
                }
            });
        }

        [MessageHandler(typeof(CGetClubInfoReqMessage))]
        public void CGetClubInfoReq(GameSession session, CGetClubInfoReqMessage message)
        {
            session.SendAsync(new SGetClubInfoAckMessage
            {
                ClubInfo = new ClubInfoDto
                {
                    Unk1 = "aaaa",
                    Unk2 = "bbbb",
                    Unk3 = "cccc",
                    Unk4 = "dddd",
                    Unk5 = "ffff",
                    Unk6 = 6,
                    Unk7 = 7,
                    Unk8 = 8
                }
            });
        }
    }
}
