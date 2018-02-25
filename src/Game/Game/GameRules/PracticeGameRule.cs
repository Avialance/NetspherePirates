using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Core;

namespace Netsphere.Game.GameRules
{
    internal class PracticeGameRule : GameRuleBase
    {
        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(PracticeGameRule));

        public override GameRule GameRule => GameRule.Practice;
        public override Briefing Briefing { get; }

        public PracticeGameRule(Room room)
            : base(room)
        {
            Briefing = new Briefing(this);
            
            StateMachine.Configure(GameRuleState.Waiting)
                .PermitIf(GameRuleStateTrigger.StartGame, GameRuleState.Neutral, CanStart);

            StateMachine.Configure(GameRuleState.Neutral)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.EnteringResult);

            StateMachine.Configure(GameRuleState.EnteringResult)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.StartResult, GameRuleState.Result);

            StateMachine.Configure(GameRuleState.Result)
                .SubstateOf(GameRuleState.Playing)
                .Permit(GameRuleStateTrigger.EndGame, GameRuleState.Waiting);
        }

        public override void Initialize()
        {
            var teamMgr = Room.TeamManager;
            teamMgr.Add(Team.Alpha, 1, 0);
            base.Initialize();
        }

        public override void Update(TimeSpan delta)
        {
            if (StateMachine.IsInState(GameRuleState.Playing) &&
                !StateMachine.IsInState(GameRuleState.EnteringResult) &&
                !StateMachine.IsInState(GameRuleState.Result) &&
                RoundTime >= TimeSpan.FromSeconds(5)) // Let the round run for at least 5 seconds - Fixes StartResult trigger on game start(race condition)
            {
                    if (RoundTime >= Room.Options.TimeLimit)
                        StateMachine.Fire(GameRuleStateTrigger.StartResult);
            }

            base.Update(delta);
        }

        public override PlayerRecord GetPlayerRecord(Player plr)
        {
            return new PracticeRecord(plr);
        }

        public override void OnScoreKill(Player killer, Player assist, Player target, AttackAttribute attackAttribute)
        {
            //killer.RoomInfo.Team.Score++;
            GetRecord(killer).Kills++;
            base.OnScoreKill(killer, null, null, attackAttribute);
            //Logger.Debug("Kill");
        }

        public override void OnScoreSuicide(Player plr)
        {
            base.OnScoreSuicide(plr);
            Logger.Debug("Suicide");
        }

        private static PracticeRecord GetRecord(Player plr)
        {
            return (PracticeRecord)plr.RoomInfo.Stats;
        }

        private bool CanStart()
        {
            return true;
        }
    }

    internal class PracticeRecord : PlayerRecord
    {
        public override uint TotalScore => Kills;

        public PracticeRecord(Player plr)
            : base(plr)
        {

        }
    }
}
