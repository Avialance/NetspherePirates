﻿using System.Linq;
using BlubLib.DotNetty.Handlers.MessageHandling;
using ExpressMapper.Extensions;
using Netsphere.Network.Data.Chat;
using Netsphere.Network.Message.Chat;
using ProudNet.Handlers;

namespace Netsphere.Network.Services
{
    internal class CommunityService : ProudMessageHandler
    {
        [MessageHandler(typeof(CSetUserDataReqMessage))]
        public void SetUserDataHandler(ChatSession session, CSetUserDataReqMessage message)
        {
            var plr = session.Player;
            if (message.UserData.ChannelId > 0 && !plr.SentPlayerList && plr.Channel != null)
            {
                // We can't send the channel player list in Channel.Join because the client only accepts it here :/
                plr.SentPlayerList = true;
                var data = plr.Channel.Players.Values.Select(p => p.Map<Player, UserDataWithNickDto>()).ToArray();
                session.SendAsync(new SChannelPlayerListAckMessage(data));
            }

            // Save settings if any of them changed
            var settings = plr.Settings;
            var name = nameof(UserDataDto.AllowCombiInvite);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowCombiInvite)
                settings.AddOrUpdate(name, message.UserData.AllowCombiInvite);

            name = nameof(UserDataDto.AllowFriendRequest);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowFriendRequest)
                settings.AddOrUpdate(name, message.UserData.AllowFriendRequest);

            name = nameof(UserDataDto.AllowRoomInvite);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowRoomInvite)
                settings.AddOrUpdate(name, message.UserData.AllowRoomInvite);

            name = nameof(UserDataDto.AllowInfoRequest);
            if (!settings.Contains(name) || settings.Get<CommunitySetting>(name) != message.UserData.AllowInfoRequest)
                settings.AddOrUpdate(name, message.UserData.AllowInfoRequest);
        }

        [MessageHandler(typeof(CGetUserDataReqMessage))]
        public void GetUserDataHandler(ChatSession session, CGetUserDataReqMessage message)
        {
            var plr = session.Player;
            if (plr.Account.Id == message.AccountId)
            {
                session.SendAsync(new SUserDataAckMessage(plr.Map<Player, UserDataDto>()));
                return;
            }

            Player target;
            if (!plr.Channel.Players.TryGetValue(message.AccountId, out target))
                return;

            switch (target.Settings.Get<CommunitySetting>(nameof(UserDataDto.AllowInfoRequest)))
            {
                case CommunitySetting.Deny:
                    // Not sure if there is an answer to this
                    return;

                case CommunitySetting.FriendOnly:
                    // ToDo
                    return;
            }

            session.SendAsync(new SUserDataAckMessage(target.Map<Player, UserDataDto>()));
        }

        [MessageHandler(typeof(CDenyChatReqMessage))]
        public void DenyHandler(ChatServer service, ChatSession session, CDenyChatReqMessage message)
        {
            var plr = session.Player;

            if (message.Deny.AccountId == plr.Account.Id)
                return;

            Deny deny;
            switch (message.Action)
            {
                case DenyAction.Add:
                    if (plr.DenyManager.Contains(message.Deny.AccountId))
                        return;

                    var target = GameServer.Instance.PlayerManager[message.Deny.AccountId];
                    if (target == null)
                        return;

                    deny = plr.DenyManager.Add(target);
                    session.SendAsync(new SDenyChatAckMessage(0, DenyAction.Add, deny.Map<Deny, DenyDto>()));
                    break;

                case DenyAction.Remove:
                    deny = plr.DenyManager[message.Deny.AccountId];
                    if (deny == null)
                        return;

                    plr.DenyManager.Remove(message.Deny.AccountId);
                    session.SendAsync(new SDenyChatAckMessage(0, DenyAction.Remove, deny.Map<Deny, DenyDto>()));
                    break;
            }
        }

        [MessageHandler(typeof(CFriendReqMessage))]
        public void FriendRequest(ChatSession session, CFriendReqMessage message)
        {
            var target = GameServer.Instance.PlayerManager[message.AccountId];
            var trg_settings = target.Settings;

            var name = nameof(UserDataDto.AllowFriendRequest);
            var allowFriendReq =
                trg_settings.Contains(name) && trg_settings.Get<CommunitySetting>(name) == CommunitySetting.Allow;

            session.SendAsync(new SFriendAckMessage(allowFriendReq ? 0 : 2));

            if (allowFriendReq)
            {
                // How handle it?
            }
        }
    }
}
