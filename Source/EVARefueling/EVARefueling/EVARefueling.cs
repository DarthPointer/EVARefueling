using System.Collections.Generic;
using UnityEngine;

namespace EVARefueling
{
    public class EVARefuelingPump : PartModule, ISerializationCallbackReceiver
    {
        public static EVARefuelingPump awaitingPump;
        static Dictionary<string, float> copiedresourcePumpingRatesDictict;     // Stores the dictionary to avoid data loss on reserialization

        EVARefuelingPump connectedPump;
        bool connected = false;

        [KSPField(isPersistant = true, guiActive = false)]
        public string resourcePumpingRates = "";

        Dictionary<string, float> resourcePumpingRatesDict = new Dictionary<string, float>();               // resource name: units per sec
        Dictionary<string, float> activeResourcePumpingRatedDict = new Dictionary<string, float>();

        [KSPField(isPersistant = true, guiActive = false)]
        public bool isEVASide = false;

        #region Events
        [KSPEvent(guiActive = true, guiName = "Find Pumping Counterpart", groupName = "EVARefuelingPump", groupDisplayName = "EVA Refueling", guiActiveUnfocused = true, requireFullControl = false, guiActiveUncommand = true)]
        public void FindPumpingCounterPart()
        {
            if (awaitingPump == null)
            {
                awaitingPump = this;
            }
            else if (awaitingPump.isEVASide != isEVASide)
            {
                EngagePumpPair();
            }
            else
            {
                awaitingPump.CutConnection();
                awaitingPump = this;
            }

            Events["FindPumpingCounterPart"].guiActive = false;
            Events["FindPumpingCounterPart"].guiActiveUnfocused = false;

            Events["CutConnection"].guiActive = true;
            Events["CutConnection"].guiActiveUnfocused = true;
        }

        [KSPEvent(guiActive = false, guiName = "Cut Connection", groupName = "EVARefuelingPump", groupDisplayName = "EVA Refueling", guiActiveUnfocused = false, requireFullControl = false, guiActiveUncommand = true)]
        public void CutConnection()
        {
            if (connectedPump != null)
            {
                connectedPump.DisengageFromPair();
            }
            if (awaitingPump == this)
            {
                awaitingPump = null;
            }

            DisengageFromPair();
        }

        void EngagePumpPair()
        {
            if (isEVASide)
            {
                if (awaitingPump.isEVASide)
                {
                    Debug.LogError("[EVARefueling] Both pumps marked for engaging are EVA-Side. Normally this error message should NEVER arise as engaging is only called for EVA+nonEVA pump pairs");
                }
                else
                {
                    EVARefuelingPump tmp = awaitingPump;
                    awaitingPump = this;
                    tmp.EngagePumpPair();                       // Actual engaging is to be done by non-EVA-Side pump
                }
            }
            else
            {
                awaitingPump.connectedPump = this;
                awaitingPump.connected = true;

                connectedPump = awaitingPump;
                connected = true;

                awaitingPump = null;

                foreach (string resourceName in resourcePumpingRatesDict.Keys)
                {
                    if (part.Resources.Contains(resourceName) && connectedPump.part.Resources.Contains(resourceName))
                    {
                        KSPEvent attributeHolder = new KSPEvent();
                        #region Setting Attribs
                        attributeHolder.guiActive = true;
                        attributeHolder.guiName = $"Pump {part.Resources[resourceName].info.displayName} here";
                        attributeHolder.groupName = "EVARefuelingPump";
                        attributeHolder.groupDisplayName = "EVA Refueling";
                        attributeHolder.guiActiveUnfocused = true;
                        attributeHolder.requireFullControl = false;
                        attributeHolder.guiActiveUncommand = true;
                        #endregion
                        Events.Add(new BaseEvent(Events, $"InPump_{resourceName}", () =>
                        {
                            activeResourcePumpingRatedDict[resourceName] = resourcePumpingRatesDict[resourceName];

                            Events[$"InPump_{resourceName}"].guiActive = false;
                            Events[$"InPump_{resourceName}"].guiActiveUnfocused = false;

                            Events[$"StopPump_{resourceName}"].guiActive = true;
                            Events[$"StopPump_{resourceName}"].guiActiveUnfocused = true;

                            connectedPump.Events[$"InPump_{resourceName}"].guiActive = true;
                            connectedPump.Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            connectedPump.Events[$"StopPump_{resourceName}"].guiActive = true;
                            connectedPump.Events[$"StopPump_{resourceName}"].guiActiveUnfocused = true;
                        },
                        attributeHolder));

                        attributeHolder = new KSPEvent();
                        #region Setting Attribs
                        attributeHolder.guiActive = true;
                        attributeHolder.guiName = $"Pump {part.Resources[resourceName].info.displayName} here";
                        attributeHolder.groupName = "EVARefuelingPump";
                        attributeHolder.groupDisplayName = "EVA Refueling";
                        attributeHolder.guiActiveUnfocused = true;
                        attributeHolder.requireFullControl = false;
                        attributeHolder.guiActiveUncommand = true;
                        #endregion
                        connectedPump.Events.Add(new BaseEvent(connectedPump.Events, $"InPump_{resourceName}", () =>
                        {
                            activeResourcePumpingRatedDict[resourceName] = -resourcePumpingRatesDict[resourceName];

                            Events[$"InPump_{resourceName}"].guiActive = true;
                            Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            Events[$"StopPump_{resourceName}"].guiActive = true;
                            Events[$"StopPump_{resourceName}"].guiActiveUnfocused = true;

                            connectedPump.Events[$"InPump_{resourceName}"].guiActive = false;
                            connectedPump.Events[$"InPump_{resourceName}"].guiActiveUnfocused = false;

                            connectedPump.Events[$"StopPump_{resourceName}"].guiActive = true;
                            connectedPump.Events[$"StopPump_{resourceName}"].guiActiveUnfocused = true;
                        },
                        attributeHolder));

                        attributeHolder = new KSPEvent();
                        #region Setting Attribs
                        attributeHolder.guiActive = false;
                        attributeHolder.guiName = $"Stop Pumping {part.Resources[resourceName].info.displayName}";
                        attributeHolder.groupName = "EVARefuelingPump";
                        attributeHolder.groupDisplayName = "EVA Refueling";
                        attributeHolder.guiActiveUnfocused = false;
                        attributeHolder.requireFullControl = false;
                        attributeHolder.guiActiveUncommand = false;
                        #endregion
                        Events.Add(new BaseEvent(Events, $"StopPump_{resourceName}", () =>
                        {
                            activeResourcePumpingRatedDict[resourceName] = 0;

                            Events[$"InPump_{resourceName}"].guiActive = true;
                            Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            Events[$"StopPump_{resourceName}"].guiActive = false;
                            Events[$"StopPump_{resourceName}"].guiActiveUnfocused = false;

                            connectedPump.Events[$"InPump_{resourceName}"].guiActive = true;
                            connectedPump.Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            connectedPump.Events[$"StopPump_{resourceName}"].guiActive = false;
                            connectedPump.Events[$"StopPump_{resourceName}"].guiActiveUnfocused = false;
                        },
                        attributeHolder));

                        attributeHolder = new KSPEvent();
                        #region Setting Attribs
                        attributeHolder.guiActive = false;
                        attributeHolder.guiName = $"Stop Pumping {part.Resources[resourceName].info.displayName}";
                        attributeHolder.groupName = "EVARefuelingPump";
                        attributeHolder.groupDisplayName = "EVA Refueling";
                        attributeHolder.guiActiveUnfocused = false;
                        attributeHolder.requireFullControl = false;
                        attributeHolder.guiActiveUncommand = false;
                        #endregion
                        connectedPump.Events.Add(new BaseEvent(connectedPump.Events, $"StopPump_{resourceName}", () =>
                        {
                            activeResourcePumpingRatedDict[resourceName] = 0;

                            Events[$"InPump_{resourceName}"].guiActive = true;
                            Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            Events[$"StopPump_{resourceName}"].guiActive = false;
                            Events[$"StopPump_{resourceName}"].guiActiveUnfocused = false;

                            connectedPump.Events[$"InPump_{resourceName}"].guiActive = true;
                            connectedPump.Events[$"InPump_{resourceName}"].guiActiveUnfocused = true;

                            connectedPump.Events[$"StopPump_{resourceName}"].guiActive = false;
                            connectedPump.Events[$"StopPump_{resourceName}"].guiActiveUnfocused = false;
                        },
                        attributeHolder));
                    }
                }
            }
        }

        void DisengageFromPair()
        {
            connected = false;

            Events["FindPumpingCounterPart"].guiActive = true;
            Events["FindPumpingCounterPart"].guiActiveUnfocused = true;

            Events["CutConnection"].guiActive = false;
            Events["CutConnection"].guiActiveUnfocused = false;

            connectedPump = null;
            activeResourcePumpingRatedDict = new Dictionary<string, float>();

            Events.RemoveAll((BaseEvent a) => a.name.Contains("Pump_") && a.group.name == "EVARefuelingPump");
            part.Events.RemoveAll((BaseEvent a) => a.name.Contains("Pump_") && a.group.name == "EVARefuelingPump");
            part.PartActionWindow.displayDirty = true;

            Debug.Log($"[EVARefueling] Removed pump switching buttons, {Events.Count} events left in EVARefuelingPump MODULE");
        }
        #endregion

        #region PartModule
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Debug.Log($"[EVARefueling] Module node has \"PUMPING_RATES\" node: {node.HasNode("PUMPING_RATES")}");
            if (node.HasNode("PUMPING_RATES"))
            {
                Debug.Log($"[EVARefueling] Found {node.GetNode("PUMPING_RATES").values.Count} rates");
            }
            foreach (ConfigNode.Value val in node.GetNode("PUMPING_RATES").values)
            {
                resourcePumpingRatesDict[val.name] = float.Parse(val.value);
                Debug.Log($"[EVARefueling] Loaded rate {val.name} = {val.value}");
            }
            Debug.Log($"[EVARefueling] Rates count: {resourcePumpingRatesDict.Count}");
        }

        public override void OnSave(ConfigNode node)
        {
            Debug.Log($"[EVARefueling] Rates count: {resourcePumpingRatesDict.Count}");
            base.OnSave(node);
            ConfigNode pumpingRates = node.AddNode("PUMPING_RATES");

            Dictionary<string, float>.Enumerator enumerator = resourcePumpingRatesDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                pumpingRates.AddValue(enumerator.Current.Key, enumerator.Current.Value);
                Debug.Log($"[EVARefueling] Saved rate {enumerator.Current.Key} = {enumerator.Current.Value}");
            }
        }

        public void FixedUpdate()
        {
            if (connected && connectedPump == null)
            {
                DisengageFromPair();
            }
            if (connectedPump != null && !isEVASide && (part.transform.position - connectedPump.part.transform.position).magnitude < 2)
            {
                Dictionary<string, float>.Enumerator i = activeResourcePumpingRatedDict.GetEnumerator();
                while (i.MoveNext())
                {
                    if (i.Current.Value != 0)
                    {
                        PartResource thisRes = part.Resources[i.Current.Key];
                        PartResource connectedRes = connectedPump.part.Resources[i.Current.Key];
                        if (0 < thisRes.amount + i.Current.Value * TimeWarp.fixedDeltaTime &&
                            thisRes.amount + i.Current.Value * TimeWarp.fixedDeltaTime < thisRes.maxAmount &&
                            0 < connectedRes.amount - i.Current.Value * TimeWarp.fixedDeltaTime &&
                            connectedRes.amount - i.Current.Value * TimeWarp.fixedDeltaTime < connectedRes.maxAmount)
                        {
                            part.RequestResource(i.Current.Key, (double)-i.Current.Value * TimeWarp.fixedDeltaTime);
                            connectedPump.part.RequestResource(i.Current.Key, (double)i.Current.Value * TimeWarp.fixedDeltaTime);
                        }
                        else
                        {
                            Events[$"StopPump_{i.Current.Key}"].Invoke();
                        }
                    }
                }
            }
        }
        #endregion

        #region ISerializationCallbackReciever
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            copiedresourcePumpingRatesDictict = new Dictionary<string, float>(resourcePumpingRatesDict);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            resourcePumpingRatesDict = copiedresourcePumpingRatesDictict;
            copiedresourcePumpingRatesDictict = null;
        }
        #endregion
    }


    #region Addon
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class EVARefuelingAddon : MonoBehaviour
    {
        public void Start()
        {
        }

        public void OnDestroy()
        {
            EVARefuelingPump.awaitingPump = null;
        }
    }
    #endregion
}
