﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVARefueling
{
    public class EVARefuelingPump : PartModule
    {
        public static EVARefuelingPump awaitingPump;

        //bool awaitingConnection = false;
        EVARefuelingPump connectedPump;

        [KSPField(isPersistant = true, guiActive = false)]
        readonly string resourcePumpingRates = "";

        Dictionary<string, float> resourcePumpingRatesDict = new Dictionary<string, float>();               // resource name: units per sec

        [KSPField(isPersistant = true, guiActive = false)]
        readonly bool isEVASide = false;

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
        
        override public void OnFixedUpdate()
        {
            if (connectedPump != null)
            {
                UnityEngine.Debug.Log((part.orgPos - connectedPump.part.orgPos).magnitude);
            }
        }
    }
}
