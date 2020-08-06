using System.Collections.Generic;
using UnityEngine;

namespace EVARefueling
{
    public class EVARefuelingPump : PartModule, ISerializationCallbackReceiver
    {
        public static EVARefuelingPump awaitingPump;
        static Dictionary<string, float> copiedRPRDict;

        EVARefuelingPump connectedPump;

        [KSPField(isPersistant = true, guiActive = false)]
        public string resourcePumpingRates = "";

        Dictionary<string, float> resourcePumpingRatesDict = new Dictionary<string, float>();               // resource name: units per sec

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
                awaitingPump.connectedPump = this;
                connectedPump = awaitingPump;
                awaitingPump = null;
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

            Events["FindPumpingCounterPart"].guiActive = true;
            Events["FindPumpingCounterPart"].guiActiveUnfocused = true;

            Events["CutConnection"].guiActive = false;
            Events["CutConnection"].guiActiveUnfocused = false;
        }
        #endregion

        #region PartModule
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            print("lol");
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

        public override void OnCopy(PartModule fromModule)
        {
            base.OnCopy(fromModule);
            resourcePumpingRatesDict = (fromModule as EVARefuelingPump).resourcePumpingRatesDict;
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
