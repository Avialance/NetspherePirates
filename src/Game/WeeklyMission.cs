using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper.FastCrud;
using Netsphere.Database.Game;
using Netsphere.Network;
using Netsphere.Network.Data.Game;
using Netsphere.Network.Message.Game;
using Netsphere.Resource;
using Serilog;
using Serilog.Core;

namespace Netsphere
{
    internal class WeeklyMission
    {
        private const int MaxRuningTask = 3;

        // ReSharper disable once InconsistentNaming
        private static readonly ILogger Logger = Log.ForContext(Constants.SourceContextPropertyName, nameof(WeeklyMission));
        private readonly List<PlayerMissionsDto> _tasksR;
        private List<PlayerMissionsDto> _taskOnGoing;
        private int _rMRunning;
        private int _wMRunning;

        public Player Player { get; set; }

        public WeeklyMission(Player plr, PlayerDto plrDto)
        {
            int _rMissions;
            int _wMissions;
            Player = plr;
            _tasksR = new List<PlayerMissionsDto>();

            var TaskList = GameServer.Instance.ResourceCache.GetTasks();

            _rMissions = 0;
            _rMRunning = 0;
            _wMissions = 0;
            _wMRunning = 0;

            foreach (var task in plrDto.Missions)
            {
                var t = TaskList[task.TaskId];

                var TaskDate = DateTime.FromBinary(task.Date);
                var today = DateTime.Now.DayOfWeek - DayOfWeek.Monday;
                var dt = DateTime.Now.AddDays(-today);

                if (t.constant || dt.Subtract(TaskDate).Days < 7)
                {
                    if (t.constant)
                    {
                        if (task.Progress == t.EndCondition.repetetion)
                            _rMissions++;
                        else
                            _rMRunning++;
                    }
                    else
                    {
                        if (task.Progress == t.EndCondition.repetetion)
                            _wMissions++;
                        else
                            _wMRunning++;
                    }

                    _tasksR.Add(task);
                    Logger.ForAccount(Player)
                            .Debug($"Mission {task.Id} - {t.Name}, progress {task.Progress}/{t.EndCondition.repetetion}");
                }
            }

            var rand = new Random();

            if (_rMissions + _rMRunning == 0)
            {
                var RCMissionList = from t in TaskList
                                    where t.Value.Level == 0
                                    && t.Value.constant && t.Value.StartCondition.CanStart(plr)
                                    orderby t.Value.Level ascending
                                    select t.Value;

                while (_rMRunning < MaxRuningTask)
                {
                    foreach (var task in RCMissionList)
                    {
                        if (task.GetChance(plr) < rand.Next(100)
                            || _tasksR.Any(t => t.TaskId == task.Id))
                            continue;
                        if (_rMRunning >= MaxRuningTask)
                            break;

                        var rt = (task.RewardExp != 0) ? rand.Next(1, 2) : 1;

                        _tasksR.Add(new PlayerMissionsDto
                        {
                            TaskId = task.Id,
                            PlayerId = plr.Account.Id,
                            Slot = (byte)(_rMRunning%3),
                            Progress = 0,
                            Date = DateTime.Now.ToBinary(),
                            RewardType = (byte)rt,
                            Reward = (ushort)(rt == 1 ? task.RewardPen : task.RewardExp)
                        });

                        Logger.ForAccount(Player)
                            .Debug($"Added Mission {task.Id} - {task.Level} - {task.Name}");

                        _rMRunning++;
                    }
                }
            }

            if (_wMissions + _wMRunning == 0 && _rMissions == 12)
            {
                var RCMissionList = from t in TaskList
                                    where t.Value.Level == 0
                                    && t.Value.constant && t.Value.StartCondition.CanStart(plr)
                                    orderby t.Value.Level ascending
                                    select t.Value;
                while (_wMRunning < MaxRuningTask)
                {
                    foreach (var task in RCMissionList)
                    {
                        if (_wMRunning >= MaxRuningTask)
                            break;
                        if (task.GetChance(plr) < rand.Next(100)
                            || _tasksR.Any(t => t.TaskId == task.Id))
                            continue;

                        var rt = (task.RewardExp != 0) ? rand.Next(1,2) : 1;

                        _tasksR.Add(new PlayerMissionsDto
                        {
                            TaskId = task.Id,
                            PlayerId = plr.Account.Id,
                            Progress = 0,
                            Date = DateTime.Now.ToBinary(),
                            Slot = (byte)(_wMRunning % 3),
                            RewardType = (byte)rt,
                            Reward = (ushort)(rt == 1 ? task.RewardPen : task.RewardExp)
                        });

                        Logger.ForAccount(Player)
                            .Debug($"Weekly Added Mission {task.Id} - {task.Level} - {task.Name}");

                        _wMRunning++;
                    }
                }
            }
        }

        public TaskDto[] GetTasks()
        {
            var i = 0;

            var result = new List<TaskDto>();

            foreach (var task in _tasksR)
            {
                result.Add(new TaskDto
                {
                    Id = task.TaskId,
                    Unk1 = task.Slot, //Slot?
                    Progress = (ushort)task.Progress,
                    Reward = task.Reward,
                    RewardType = (MissionRewardType)task.RewardType
                });
                i++;
            }

            return result.ToArray();
        }

        public TaskDto AcceptTask(byte type, uint level, byte slot)
        {
            //Logger.Debug($"Claimed mission {TaskLevel}");
            var TaskList = GameServer.Instance.ResourceCache.GetTasks();
            var constants = type == 1;

            var tasks = from t in TaskList
                       where t.Value.constant == constants &&
                       t.Value.Level == level
                       select t.Value;

            if (tasks.Any())
            {
                TaskInfo task = null;
                var rand = new Random();

                while (task == null)
                {
                    foreach (var tt in tasks)
                    {
                        if (tt.GetChance(Player) < rand.Next(100)
                            || _tasksR.Any(t => t.TaskId == tt.Id))
                            continue;

                        task = tt;

                        break;
                    }
                }

                if (constants)
                    _rMRunning++;
                else
                    _wMRunning++;

                var rt = (task.RewardExp != 0) ? rand.Next(1, 2) : 1;
                var reward = (rt == 1 ? task.RewardPen : task.RewardExp);

                _tasksR.Add(new PlayerMissionsDto
                {
                    TaskId = task.Id,
                    PlayerId = Player.Account.Id,
                    Progress = 0,
                    Date = DateTime.Now.ToBinary(),
                    Slot = slot,
                    RewardType = (byte)rt,
                    Reward = (ushort)reward

                });
                Logger.ForAccount(Player)
                    .Debug($"Added Mission {task.Id} - {task.Level} - {task.Name}");
                return new TaskDto
                {
                    Id = task.Id,
                    Unk1 = slot, //Slot?
                    Progress = 0,
                    Reward = reward,
                    RewardType = (MissionRewardType)rt
                };
            }

            return null;
        }

        public ushort UpdateTask(uint Taskid, ushort Progress)
        {
            var TaskInfo = GameServer.Instance.ResourceCache.GetTasks();

            if (_tasksR.Exists(t => t.TaskId == Taskid))
            {
                var task = _tasksR.Find(t => t.TaskId == Taskid);
                var repetition = (ushort)TaskInfo[Taskid].EndCondition.repetetion;
                task.Progress++;

                if (task.Progress == repetition)
                {
                    Player.PEN += task.Reward;
                    if (task.RewardType == 2)
                        Player.GainExp(task.Reward);

                    Player.Session.SendAsync(new SRefreshCashInfoAckMessage { PEN = Player.PEN, AP = Player.AP });
                }

                if (task.Progress > repetition)
                {
                    task.Progress = repetition;
                    return 0xffff;
                }

                return task.Progress;
            }

            return 0;
        }

        public void Save(IDbConnection db)
        {
            foreach (var task in _tasksR)
            {
                if (task.Id == 0)
                {
                    db.Insert(task);
                    Logger.Debug($"Mission inserted as {task.Id}");
                }
                else
                {
                    db.Update(task);
                }
            }
        }
    }
}
