using Intel.Manageability.Cim.Typed;
using Intel.Manageability.WSManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Common
{
    public class WatchdogWrapper
    {
        private const ushort WATCHDOG_STATE_NOT_STARTED = 1;
        private const ushort WATCHDOG_STATE_STOPPED = 2;
        private const ushort WATCHDOG_STATE_RUNNING = 4;
        private const ushort WATCHDOG_STATE_EXPIRED = 8;
        private const ushort WATCHDOG_STATE_SUSPENDED = 16;
        private const ushort WATCHDOG_STATE_ANY = 255;

        public List<WatchdogModel> List(IWSManClient client, string id)
        {
            AMT_AgentPresenceService service = (AMT_AgentPresenceService)AssociationTraversalTypedUtils.GetAssociated(client,
                                                                  AssociationTraversalTypedUtils.DiscoverIntelAMT(client),
                                                                  typeof(AMT_AgentPresenceService),
                                                                  typeof(CIM_HostedService));

            Collection<CimBase> agentWatchdogs = AssociationTraversalTypedUtils.EnumerateAssociated(client,
                                                                                   service.Reference,
                                                                                   typeof(AMT_AgentPresenceWatchdog),
                                                                                   typeof(CIM_ConcreteDependency));

            var list=new List<WatchdogModel>();
            bool addWatch = false;
            foreach (AMT_AgentPresenceWatchdog dog in agentWatchdogs)
            {
                if (id != null)
                {
                    addWatch = false;
                    if (dog.DeviceID == id)
                    {
                        addWatch = true;
                    }
                }
                else
                {
                    addWatch = true;
                }

                if (addWatch)
                {
                    WatchdogModel model = new WatchdogModel();
                    model.Name = dog.MonitoredEntityDescription;
                    model.State = GetAgentStateString(dog.CurrentState);
                    model.Id = dog.DeviceID;
                    list.Add(model);
                }
            }
            return list;
        }

        /// <summary>
        /// Returns a string representation for the given state
        /// </summary>
        private string GetAgentStateString(ushort state)
        {
            bool temp = false;
            string stateStr = "";
            if (state == WATCHDOG_STATE_ANY)
            {
                return "any";
            }
            if ((state & WATCHDOG_STATE_NOT_STARTED) != 0)
            {
                temp = true;
                stateStr = "not started";
            }
            if ((state & WATCHDOG_STATE_STOPPED) != 0)
            {
                if (temp)
                {
                    stateStr += " / ";
                }
                temp = true;
                stateStr += "stopped";
            }
            if ((state & WATCHDOG_STATE_RUNNING) != 0)
            {
                if (temp)
                {
                    stateStr += " / ";
                }
                temp = true;
                stateStr += "running";
            }
            if ((state & WATCHDOG_STATE_EXPIRED) != 0)
            {
                if (temp)
                {
                    stateStr += " / ";
                }
                temp = true;
                stateStr += "expired";
            }
            if ((state & WATCHDOG_STATE_SUSPENDED) != 0)
            {
                if (temp)
                {
                    stateStr += " / ";
                }
                temp = true;
                stateStr += "suspended";
            }
            if (!temp)
            {
                stateStr += "unknown error";
            }
            return stateStr;
        }

    }

    public class WatchdogModel
    {
        public string State { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
