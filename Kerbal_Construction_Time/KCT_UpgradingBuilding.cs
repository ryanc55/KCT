using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalConstructionTime
{
    public class KCT_UpgradingBuilding : IKCTBuildItem
    {
        [Persistent]
        public SpaceCenterFacility facilityType;
        [Persistent]
        public int upgradeLevel, currentLevel, launchpadID = 0;
        [Persistent]
        public string id, commonName;
        [Persistent]
        public double progress = 0, BP = 0, cost = 0;
        [Persistent]
        public bool UpgradeProcessed = false, isLaunchpad = false;
        //public bool allowUpgrade = false;
        private KCT_KSC _KSC = null;

        public KCT_UpgradingBuilding()
        {

        }

        public KCT_UpgradingBuilding(SpaceCenterFacility type, string facilityID, int newLevel, int oldLevel, string name)
        {
            facilityType = type;
            id = facilityID;
            upgradeLevel = newLevel;
            currentLevel = oldLevel;
            commonName = name;

            KCTDebug.Log(string.Format("Upgrade of {0} requested from {1} to {2}", name, oldLevel, newLevel));
        }

        public void Downgrade()
        {
            KCTDebug.Log("Downgrading " + commonName + " to level " + currentLevel);
            if (isLaunchpad)
            {
                KSC.LaunchPads[launchpadID].level = currentLevel;
                if (KCT_GameStates.activeKSCName != KSC.KSCName || KCT_GameStates.ActiveKSC.ActiveLaunchPadID != launchpadID)
                {
                    return;
                }
            }
            foreach (Upgradeables.UpgradeableFacility facility in GetFacilityReferences())
            {
                KCT_Events.allowedToUpgrade = true;
                facility.SetLevel(currentLevel);
            }
            //KCT_Events.allowedToUpgrade = false;
        }

        public void Upgrade()
        {
            KCTDebug.Log("Upgrading " + commonName + " to level " + upgradeLevel);
            if (isLaunchpad)
            {
                KSC.LaunchPads[launchpadID].level = upgradeLevel;
                KSC.LaunchPads[launchpadID].DestructionNode = new ConfigNode("DestructionState");
                if (KCT_GameStates.activeKSCName != KSC.KSCName || KCT_GameStates.ActiveKSC.ActiveLaunchPadID != launchpadID)
                {
                    UpgradeProcessed = true;
                    return;
                }
                KSC.LaunchPads[launchpadID].Upgrade(upgradeLevel);
            }
            KCT_Events.allowedToUpgrade = true;
            foreach (Upgradeables.UpgradeableFacility facility in GetFacilityReferences())
            {
                facility.SetLevel(upgradeLevel);
            }
            int newLvl = KCT_Utilities.BuildingUpgradeLevel(id);
            UpgradeProcessed = (newLvl == upgradeLevel);

            KCTDebug.Log($"Upgrade processed: {UpgradeProcessed} Current: {newLvl} Desired: {upgradeLevel}");

            //KCT_Events.allowedToUpgrade = false;
        }

        public List<Upgradeables.UpgradeableFacility> GetFacilityReferences()
        {
            return ScenarioUpgradeableFacilities.protoUpgradeables[id].facilityRefs;
        }

        public void SetBP(double cost)
        {
            BP = CalculateBP(cost, facilityType);
        }

        public bool AlreadyInProgress()
        {
            return (KSC != null);
        }

        public KCT_KSC KSC
        {
            get
            {
                if (_KSC == null)
                {
                    if (!isLaunchpad)
                        _KSC = KCT_GameStates.KSCs.Find(ksc => ksc.KSCTech.Find(ub => ub.id == this.id) != null);
                    else
                        _KSC = KCT_GameStates.KSCs.Find(ksc => ksc.KSCTech.Find(ub => ub.id == this.id && ub.isLaunchpad && ub.launchpadID == this.launchpadID) != null);
                }
                return _KSC;
            }
        }

        public string GetItemName()
        {
            return commonName;
        }

        public double GetBuildRate()
        {
            double rateTotal = 0;
            if (KSC != null)
            {
                rateTotal = KCT_Utilities.GetBothBuildRateSum(KSC);
            }
            return rateTotal;
        }

        public double GetTimeLeft()
        {
            return (BP - progress) / ((IKCTBuildItem)this).GetBuildRate();
        }

        public bool IsComplete()
        {
            return progress >= BP;
        }

        public KCT_BuildListVessel.ListType GetListType()
        {
            return KCT_BuildListVessel.ListType.KSC;
        }

        public void IncrementProgress(double UTDiff)
        {
            if (!IsComplete()) AddProgress(GetBuildRate() * UTDiff);
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && (IsComplete() || !KCT_PresetManager.Instance.ActivePreset.generalSettings.KSCUpgradeTimes))
            {
                if (ScenarioUpgradeableFacilities.Instance != null && KCT_GameStates.erroredDuringOnLoad.OnLoadFinished)
                {
                    Upgrade();

                    try
                    {
                        KCT_Events.onFacilityUpgradeComplete?.Fire(this);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public static double CalculateBP(double cost, SpaceCenterFacility facilityType)
        {
            int isAdm = 0, isAC = 0, isLP = 0, isMC = 0, isRD = 0, isRW = 0, isTS = 0, isSPH = 0, isVAB = 0;
            switch (facilityType)
            {
                case SpaceCenterFacility.Administration:
                    isAdm = 1;
                    break;
                case SpaceCenterFacility.AstronautComplex:
                    isAC = 1;
                    break;
                case SpaceCenterFacility.LaunchPad:
                    isLP = 1;
                    break;
                case SpaceCenterFacility.MissionControl:
                    isMC = 1;
                    break;
                case SpaceCenterFacility.ResearchAndDevelopment:
                    isRD = 1;
                    break;
                case SpaceCenterFacility.Runway:
                    isRW = 1;
                    break;
                case SpaceCenterFacility.TrackingStation:
                    isTS = 1;
                    break;
                case SpaceCenterFacility.SpaceplaneHangar:
                    isSPH = 1;
                    break;
                case SpaceCenterFacility.VehicleAssemblyBuilding:
                    isVAB = 1;
                    break;
                default:
                    break;
            }

            var variables = new Dictionary<string, string>()
            {
                { "C", cost.ToString() },
                { "O", KCT_PresetManager.Instance.ActivePreset.timeSettings.OverallMultiplier.ToString() },
                { "Adm", isAdm.ToString() },
                { "AC", isAC.ToString() },
                { "LP", isLP.ToString() },
                { "MC", isMC.ToString() },
                { "RD", isRD.ToString() },
                { "RW", isRW.ToString() },
                { "TS", isTS.ToString() },
                { "SPH", isSPH.ToString() },
                { "VAB", isVAB.ToString() }
            };

            double bp = KCT_MathParsing.GetStandardFormulaValue("KSCUpgrade", variables);
            if (bp <= 0) { bp = 1; }

            return bp;
        }

        public static double CalculateBuildTime(double cost, SpaceCenterFacility facilityType, KCT_KSC KSC = null)
        {
            double bp = CalculateBP(cost, facilityType);
            double rateTotal = KCT_Utilities.GetBothBuildRateSum(KSC ?? KCT_GameStates.ActiveKSC);

            return bp / rateTotal;
        }

        private void AddProgress(double amt)
        {
            progress += amt;
            if (progress > BP) progress = BP;
        }
    }
}
