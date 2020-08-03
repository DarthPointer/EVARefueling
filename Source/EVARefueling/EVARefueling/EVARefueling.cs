using System.Collections.Generic;
using UnityEngine;

namespace EVARefueling
{
    public class EVARefuelingPump : PartModule
    {
        public static EVARefuelingPump awaitingPump;

        //bool awaitingConnection = false;
        EVARefuelingPump connectedPump;

        [KSPField(isPersistant = true, guiActive = false)]
        public string resourcePumpingRates = "";

        Dictionary<string, float> resourcePumpingRatesDict = new Dictionary<string, float>();               // resource name: units per sec

        [KSPField(isPersistant = true, guiActive = false)]
        public bool isEVASide = false;

        float debugTimeout = 0;

        [KSPEvent(guiActive = true, guiName = "Find Pumping Counterpart")]
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
            Events["CutConnection"].guiActive = true;
        }

        [KSPEvent(guiActive = false, guiName = "Cut Connection")]
        public void CutConnection()
        {
            if (connectedPump != null)
            {
                connectedPump.connectedPump = null;
                connectedPump.Events["FindPumpingCounterPart"].guiActive = true;
                connectedPump.Events["CutConnection"].guiActive = false;
                connectedPump = null;
            }
            if (awaitingPump == this)
            {
                awaitingPump = null;
            }

            Events["FindPumpingCounterPart"].guiActive = true;
            Events["CutConnection"].guiActive = false;
        }
        
        public void FixedUpdate()
        {
            if (Time.time > debugTimeout + 5)
            {
                debugTimeout = Time.time + 5;
                Debug.Log($"isEVASide: {isEVASide}");
                Debug.Log($"is awaiting: {awaitingPump == this}");
                if (connectedPump != null)
                {
                    Debug.Log($"[EVARefueling] {(part.transform.position - connectedPump.part.transform.position).magnitude}");
                }
            }
        }
    }
}
