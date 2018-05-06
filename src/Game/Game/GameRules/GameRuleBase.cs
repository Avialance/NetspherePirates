using System;
using System.Linq;
using Netsphere.Network.Data.GameRule;
using Netsphere.Network.Message.GameRule;
using Serilog;
using Serilog.Core;
using Stateless;

// ReSharper disable once CheckNamespace

namespace Netsphere.Game.GameRules
{
    internal abstract class GameRuleBase
    {
        protected static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(ChaserGameRule));
        private static readonly TimeSpan PreHalfTimeWaitTime = TimeSpan.FromSeconds(9);
        private static readonly TimeSpan PreResultWaitTime = TimeSpan.FromSeconds(9);
        private static readonly TimeSpan HalfTimeWaitTime = TimeSpan.FromSeconds(24);
        private static readonly TimeSpan ResultWaitTime = TimeSpan.FromSeconds(14);
        private readonly Random _seed = new Random();

        public abstract GameRule GameRule { get; }
        public Room Room { get; }
        public abstract Briefing Briefing { get; }
        public StateMachine<GameRuleState, GameRuleStateTrigger> StateMachine { get; }

        public TimeSpan GameTime { get; private set; }
        public TimeSpan RoundTime { get; private set; }

        protected GameRuleBase(Room room)
        {
            Room = room;
            Room.PlayerJoined += PlayerJoined;
            Room.PlayerJoining += PlayerJoining;
            Room.PlayerLeft += PlayerLeft;
            StateMachine = new StateMachine<GameRuleState, GameRuleStateTrigger>(GameRuleState.Waiting);
            StateMachine.OnTransitioned(StateMachine_OnTransition);
        }

        public virtual void Initialize()
        { }

        public virtual void Cleanup()
        { }

        public virtual void Reload()
        { }

        public virtual void PlayerJoining(object room, RoomPlayerEventArgs e)
        { }

        public virtual void PlayerJoined(object room, RoomPlayerEventArgs e)
        { }

        public virtual void PlayerLeft(object room, RoomPlayerEventArgs e)
        {
            var plr = e.Player;
            plr.Mission.OnGoinClear();

            var b = plr.CharacterManager.Boosts.GetItems().First();

            if (b != null)
                b.PlayTime += plr.RoomInfo.PlayTime;

            // Change item status
            foreach (var @char in plr.CharacterManager)
            {
                var sec = plr.RoomInfo.CharacterPlayTime[@char.Slot];
                var loss = (int)plr.RoomInfo.CharacterPlayTime[@char.Slot].TotalMinutes *
                           Config.Instance.Game.DurabilityLossPerMinute;
                loss += plr.RoomInfo.CharacterDeaths[@char.Slot] * Config.Instance.Game.DurabilityLossPerDeath;

                foreach (var item in @char.Weapons.GetItems().Where(item => item != null && item.Durability != -1))
                {
                    if (item.PeriodType == ItemPeriodType.Hours)
                    {
                        item.PlayTime += sec;
                        if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                            plr.Inventory.Remove(item);
                        else
                            plr.Inventory.Update(item);
                    }
                    else if (item.PeriodType == ItemPeriodType.None)
                    {
                        item.LoseDurabilityAsync(loss).Wait();
                    }
                }

                foreach (var item in @char.Costumes.GetItems().Where(item => item != null && item.Durability != -1))
                {
                    if (item.PeriodType == ItemPeriodType.Hours)
                    {
                        item.PlayTime += sec;
                        if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                            plr.Inventory.Remove(item);
                        else
                            plr.Inventory.Update(item);
                    }
                    else if (item.PeriodType == ItemPeriodType.None)
                    {
                        item.LoseDurabilityAsync(loss).Wait();
                    }
                }

                foreach (var item in @char.Skills.GetItems().Where(item => item != null && item.Durability != -1))
                {
                    item.LoseDurabilityAsync(loss).Wait();
                    if (item.PeriodType == ItemPeriodType.Hours)
                    {
                        item.PlayTime += sec;
                        if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                            plr.Inventory.Remove(item);
                        else
                            plr.Inventory.Update(item);
                    }
                    else if (item.PeriodType == ItemPeriodType.None)
                    {
                        item.LoseDurabilityAsync(loss).Wait();
                    }
                }
            }
        }

        public virtual void Update(TimeSpan delta)
        {
            RoundTime += delta;
            if (StateMachine.IsInState(GameRuleState.Playing))
            {
                GameTime += delta;

                foreach (var plr in Room.TeamManager.PlayersPlaying)
                {
                    plr.RoomInfo.PlayTime += delta;
                    plr.RoomInfo.CharacterPlayTime[plr.CharacterManager.CurrentSlot] += delta;
                }
            }

            #region HalfTime

            if (StateMachine.IsInState(GameRuleState.EnteringHalfTime))
            {
                if (RoundTime >= PreHalfTimeWaitTime)
                {
                    RoundTime = TimeSpan.Zero;
                    StateMachine.Fire(GameRuleStateTrigger.StartHalfTime);
                }
                else
                {
                    Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.HalfTimeIn, 2, 0, 0,
                        ((int)(PreHalfTimeWaitTime - RoundTime).TotalSeconds + 1).ToString()));
                }
            }

            if (StateMachine.IsInState(GameRuleState.HalfTime))
            {
                if (RoundTime >= HalfTimeWaitTime)
                    StateMachine.Fire(GameRuleStateTrigger.StartSecondHalf);
            }

            #endregion

            #region Result

            if (StateMachine.IsInState(GameRuleState.EnteringResult))
            {
                if (RoundTime >= PreResultWaitTime)
                {
                    RoundTime = TimeSpan.Zero;
                    StateMachine.Fire(GameRuleStateTrigger.StartResult);
                }
                else
                {
                    Room.Broadcast(new SEventMessageAckMessage(GameEventMessage.ResultIn, 3, 0, 0,
                        (int)(PreResultWaitTime - RoundTime).TotalSeconds + 1 + " second(s)"));
                }
            }

            if (StateMachine.IsInState(GameRuleState.Result))
            {
                if (RoundTime >= ResultWaitTime)
                    StateMachine.Fire(GameRuleStateTrigger.EndGame);
            }

            #endregion
        }

        public abstract PlayerRecord GetPlayerRecord(Player plr);

        #region Scores

        public virtual void OnScoreKill(Player killer, Player assist, Player target, AttackAttribute attackAttribute)
        {
            killer.RoomInfo.Stats.Kills++;
            target.RoomInfo.Stats.Deaths++;

            target.RoomInfo.CharacterDeaths[target.CharacterManager.CurrentSlot]++;

            if (killer.RoomInfo.Stats.PENLC > DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                var gain = (uint)_seed.Next(10, 30);
                killer.PEN += gain;
                killer.Session.SendAsync(new SLuckyShotAckMessage
                {
                    Unk1 = 1,
                    Unk2 = 30,
                    Unk3 = 3
                });
            }

            if (killer.RoomInfo.Stats.EXPLC > DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                var gain = (uint)_seed.Next(10, 30);
                killer.TotalExperience += gain;
                killer.Session.SendAsync(new SLuckyShotAckMessage
                {
                    Unk1 = 2,
                    Unk2 = 30,
                    Unk3 = 3
                });
            }

            if (assist != null)
            {
                assist.RoomInfo.Stats.KillAssists++;

                Room.Broadcast(
                    new SScoreKillAssistAckMessage(new ScoreAssistDto(killer.RoomInfo.PeerId, assist.RoomInfo.PeerId,
                        target.RoomInfo.PeerId, attackAttribute)));
            }
            else
            {
                Room.Broadcast(
                    new SScoreKillAckMessage(new ScoreDto(killer.RoomInfo.PeerId, target.RoomInfo.PeerId,
                        attackAttribute)));
            }
        }

        public virtual void OnScoreTeamKill(Player killer, Player target, AttackAttribute attackAttribute)
        {
            target.RoomInfo.Stats.Deaths++;
            target.RoomInfo.CharacterDeaths[target.CharacterManager.CurrentSlot]++;

            Room.Broadcast(
                new SScoreTeamKillAckMessage(new Score2Dto(killer.RoomInfo.PeerId, target.RoomInfo.PeerId,
                    attackAttribute)));
        }

        public virtual void OnScoreHeal(Player plr)
        {
            Room.Broadcast(new SScoreHealAssistAckMessage(plr.RoomInfo.PeerId));
        }

        public virtual void OnScoreSuicide(Player plr)
        {
            plr.RoomInfo.Stats.Deaths++;
            plr.RoomInfo.CharacterDeaths[plr.CharacterManager.CurrentSlot]++;
            Room.Broadcast(new SScoreSuicideAckMessage(plr.RoomInfo.PeerId, AttackAttribute.KillOneSelf));
        }

        #endregion

        private void StateMachine_OnTransition(StateMachine<GameRuleState, GameRuleStateTrigger>.Transition transition)
        {
            RoundTime = TimeSpan.Zero;
            switch (transition.Destination)
            {
                case GameRuleState.FirstHalf:
                case GameRuleState.Neutral:

                    GameTime = TimeSpan.Zero;
                    foreach (var team in Room.TeamManager.Values)
                        team.Score = 0;
                    foreach ( // ToDo Use one of the Player properties
                        var plr in
                            Room.TeamManager.Values.SelectMany(
                                team =>
                                    team.Values.Where(
                                        plr =>
                                            plr.RoomInfo.IsReady || Room.Master == plr ||
                                            plr.RoomInfo.Mode == PlayerGameMode.Spectate)))
                    {
                        plr.RoomInfo.Reset();
                        plr.RoomInfo.State = plr.RoomInfo.Mode == PlayerGameMode.Normal
                            ? PlayerState.Alive
                            : PlayerState.Spectating;
                        plr.Session.SendAsync(new SBeginRoundAckMessage());
                    }

                    Room.BroadcastBriefing();
                    Room.Broadcast(new SChangeStateAckMessage(GameState.Playing));
                    if (transition.Destination == GameRuleState.Neutral)
                        Room.Broadcast(new SChangeSubStateAckMessage(GameTimeState.Neutral));
                    else
                        Room.Broadcast(new SChangeSubStateAckMessage(GameTimeState.FirstHalf));
                    break;

                case GameRuleState.HalfTime:
                    Room.Broadcast(new SChangeSubStateAckMessage(GameTimeState.HalfTime));
                    break;

                case GameRuleState.SecondHalf:
                    Room.Broadcast(new SChangeSubStateAckMessage(GameTimeState.SecondHalf));
                    break;

                case GameRuleState.Result:
                    foreach (var plr in Room.TeamManager.PlayersPlaying)
                    {
                        foreach (var @char in plr.CharacterManager)
                        {
                            var sec = plr.RoomInfo.CharacterPlayTime[@char.Slot];
                            var loss = (int)plr.RoomInfo.CharacterPlayTime[@char.Slot].TotalMinutes *
                                       Config.Instance.Game.DurabilityLossPerMinute;
                            loss += plr.RoomInfo.CharacterDeaths[@char.Slot] * Config.Instance.Game.DurabilityLossPerDeath;

                            foreach (var item in @char.Weapons.GetItems().Where(item => item != null && item.Durability != -1))
                            {
                                if (item.PeriodType == ItemPeriodType.Hours)
                                {
                                    item.PlayTime += sec;
                                    if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                                        plr.Inventory.Remove(item);
                                    else
                                        plr.Inventory.Update(item);
                                }
                                else if (item.PeriodType == ItemPeriodType.None)
                                {
                                    item.LoseDurabilityAsync(loss).Wait();
                                }
                            }

                            foreach (var item in @char.Costumes.GetItems().Where(item => item != null && item.Durability != -1))
                            {
                                if (item.PeriodType == ItemPeriodType.Hours)
                                {
                                    item.PlayTime += sec;
                                    if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                                        plr.Inventory.Remove(item);
                                    else
                                        plr.Inventory.Update(item);
                                }
                                else if (item.PeriodType == ItemPeriodType.None)
                                {
                                    item.LoseDurabilityAsync(loss).Wait();
                                }
                            }

                            foreach (var item in @char.Skills.GetItems().Where(item => item != null && item.Durability != -1))
                            {
                                item.LoseDurabilityAsync(loss).Wait();
                                if (item.PeriodType == ItemPeriodType.Hours)
                                {
                                    item.PlayTime += sec;
                                    if (item.PlayTime >= TimeSpan.FromHours(item.Period))
                                        plr.Inventory.Remove(item);
                                    else
                                        plr.Inventory.Update(item);
                                }
                            }
                        }
                    }

                    foreach (var plr in Room.TeamManager.Players.Where(plr => plr.RoomInfo.State != PlayerState.Lobby))
                        plr.RoomInfo.State = PlayerState.Waiting;

                    Room.Broadcast(new SChangeStateAckMessage(GameState.Result));
                    Room.BroadcastBriefing(true);
                    break;

                case GameRuleState.Waiting:
                    foreach (var plr in Room.TeamManager.Players.Where(plr => plr.RoomInfo.State != PlayerState.Lobby))
                    {
                        plr.RoomInfo.Reset();
                        plr.RoomInfo.State = PlayerState.Lobby;
                    }

                    Room.Broadcast(new SChangeStateAckMessage(GameState.Waiting));
                    Room.BroadcastBriefing();
                    break;
            }
        }
    }
}
