using System.Collections.Generic;
using UnityEngine;

namespace EVARefueling
{
    public class EVARefuelingPump : PartModule, ISerializationCallbackReceiver
    {
        public static EVARefuelingPump awaitingPump;
        static Dictionary<string, float> copiedRPRDict;     // Stores the dictionary to avoid data loss on reserialization

        EVARefuelingPump connectedPump;
        bool connected = false;

        [KSPField(isPersistant = true, guiActive = false)]
        public string resourcePumpingRates = "";

        Dictionary<string, float> resourcePumpingRatesDict = new Dictionary<string, float>();               // resource name: units per sec
        Dictionary<string, float> activeResourcePumpingRatedDict = new Dictionary<string, float>();

        [KSPField(isPersistant = true, guiActive = false)]
        public bool isEVASide = false;

        float nextDebugAt = 0;

        #region Events
        [KSPEvent(guiActive = true, guiName = "Find Pumping Counterpart", guiActiveUnfocused = true)]
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

        [KSPEvent(guiActive = false, guiName = "Cut Connection", guiActiveUnfocused = false)]
        public void CutConnection()
        {
            if (connectedPump != null)
            {
                connectedPump.connectedPump = null;

                connectedPump.Events["FindPumpingCounterPart"].guiActive = true;
                connectedPump.Events["FindPumpingCounterPart"].guiActiveUnfocused = true;

                connectedPump.Events["CutConnection"].guiActive = false;
                connectedPump.Events["CutConnection"].guiActiveUnfocused = false;

                connectedPump = null;

            }
            if (awaitingPump == this)
            {
                awaitingPump = null;
            }

            connected = false;

            Events["FindPumpingCounterPart"].guiActive = true;
            Events["FindPumpingCounterPart"].guiActiveUnfocused = true;

            Events["CutConnection"].guiActive = false;
            Events["CutConnection"].guiActiveUnfocused = false;
        }

        void ActivatePump(string resourceName, float rate)                      // Positive rates for pumping into this.part, negative for puming from this.part
        {
            activeResourcePumpingRatedDict[resourceName] = rate;
        }

        void EngagePumpPair()
        {
            awaitingPump.connectedPump = this;
            awaitingPump.connected = true;

            connectedPump = awaitingPump;
            connected = true;

            awaitingPump = null;

            Dictionary<string, float> rPRD = isEVASide ? connectedPump.resourcePumpingRatesDict : resourcePumpingRatesDict;

            foreach (string resourceName in rPRD.Keys)
            {
                if (part.Resources.Contains(resourceName) && connectedPump.part.Resources.Contains(resourceName))
                {
                    KSPEvent attributeHolder = new KSPEvent();
                    attributeHolder.guiActive = true;
                    attributeHolder.guiName = $"Pump {part.Resources[resourceName].info.displayName} here";
                    attributeHolder.groupName = "EVARefuelingPump";
                    attributeHolder.groupDisplayName = "EVA Refueling";
                    Events.Add(new BaseEvent(Events, $"InPump_{resourceName}", () =>
                    {
                        activeResourcePumpingRatedDict[resourceName] = rPRD[resourceName];
                        connectedPump.activeResourcePumpingRatedDict[resourceName] = -rPRD[resourceName];
                    },
                    attributeHolder));

                    attributeHolder = new KSPEvent();
                    attributeHolder.guiActive = true;
                    attributeHolder.guiName = $"Pump {part.Resources[resourceName].info.displayName} here";
                    attributeHolder.groupName = "EVARefuelingPump";
                    attributeHolder.groupDisplayName = "EVA Refueling";
                    connectedPump.Events.Add(new BaseEvent(connectedPump.Events, $"InPump_{resourceName}", () =>
                    {
                        connectedPump.activeResourcePumpingRatedDict[resourceName] = rPRD[resourceName];
                        activeResourcePumpingRatedDict[resourceName] = -rPRD[resourceName];
                    },
                    attributeHolder));
                }
            }
        }
        #endregion

        #region PartModule
        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
            }
        }

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
            if (Time.time > nextDebugAt + 5)
            {
                nextDebugAt = Time.time + 5;
                //Debug.Log($"isEVASide: {isEVASide}");
                //Debug.Log($"is awaiting: {awaitingPump == this}");
                if (connectedPump != null)
                {
                    //Debug.Log($"[EVARefueling] {(part.transform.position - connectedPump.part.transform.position).magnitude}");
                }
            }

            if (connected && connectedPump == null)
            {
                connected = false;
            }
            if (connectedPump != null)
            {
            }
        }
        #endregion

        #region ISerializationCallbackReciever
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            copiedRPRDict = new Dictionary<string, float>(resourcePumpingRatesDict);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            resourcePumpingRatesDict = copiedRPRDict;
            copiedRPRDict = null;
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
