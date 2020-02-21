using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalConstructionTime
{
    public class KCT_KSC
    {
        public string KSCName;
        public List<KCT_BuildListVessel> VABList = new List<KCT_BuildListVessel>();
        public List<KCT_BuildListVessel> VABWarehouse = new List<KCT_BuildListVessel>();
        public SortedList<string, KCT_BuildListVessel> VABPlans = new SortedList<string, KCT_BuildListVessel>();
        public List<KCT_BuildListVessel> SPHList = new List<KCT_BuildListVessel>();
        public List<KCT_BuildListVessel> SPHWarehouse = new List<KCT_BuildListVessel>();
        public SortedList<string, KCT_BuildListVessel> SPHPlans = new SortedList<string, KCT_BuildListVessel>();
        public List<KCT_UpgradingBuilding> KSCTech = new List<KCT_UpgradingBuilding>();
        //public List<KCT_TechItem> TechList = new List<KCT_TechItem>();
        public List<int> VABUpgrades = new List<int>() { 0 };
        public List<int> SPHUpgrades = new List<int>() { 0 };
        public List<int> RDUpgrades = new List<int>() { 0, 0 }; //research/development
        public List<KCT_Recon_Rollout> Recon_Rollout = new List<KCT_Recon_Rollout>();
        public List<KCT_AirlaunchPrep> AirlaunchPrep = new List<KCT_AirlaunchPrep>();
        public List<double> VABRates = new List<double>(), SPHRates = new List<double>();
        public List<double> UpVABRates = new List<double>(), UpSPHRates = new List<double>();

        public List<KCT_LaunchPad> LaunchPads = new List<KCT_LaunchPad>();
        public int ActiveLaunchPadID = 0;

        public KCT_KSC(string name)
        {
            //KCTDebug.Log("Creating KSC with name: " + name);
            KSCName = name;
            //We propogate the tech list and upgrades throughout each KSC, since it doesn't make sense for each one to have its own tech.
            RDUpgrades[1] = KCT_GameStates.TechUpgradesTotal;
            //TechList = KCT_GameStates.ActiveKSC.TechList;
            LaunchPads.Add(new KCT_LaunchPad("LaunchPad", KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.LaunchPad)));
        }

        public KCT_LaunchPad ActiveLPInstance
        {
            get
            {
                return LaunchPads.Count > ActiveLaunchPadID ? LaunchPads[ActiveLaunchPadID] : null; 
            }
        }

        public int LaunchPadCount
        {
            get
            {
                int count = 0;
                foreach (KCT_LaunchPad lp in LaunchPads)
                    if (lp.level >= 0) count++;
                return count;
            }
        }

        public KCT_Recon_Rollout GetReconditioning(string launchSite = "LaunchPad")
        {
            return Recon_Rollout.FirstOrDefault(r => r.launchPadID == launchSite && ((IKCTBuildItem)r).GetItemName() == "LaunchPad Reconditioning");
        }

        public KCT_Recon_Rollout GetReconRollout(KCT_Recon_Rollout.RolloutReconType type, string launchSite = "LaunchPad")
        {
            return Recon_Rollout.FirstOrDefault(r => r.RRType == type && r.launchPadID == launchSite);
        }

        public void RecalculateBuildRates()
        {
            VABRates.Clear();
            SPHRates.Clear();
            double rate = 0.1;
            int index = 0;
            while (rate > 0)
            {
                rate = KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.VAB, index, this);
                if (rate >= 0)
                    VABRates.Add(rate);
                index++;
            }
            rate = 0.1;
            index = 0;
            while (rate > 0)
            {
                rate = KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.SPH, index, this);
                if (rate >= 0)
                    SPHRates.Add(rate);
                index++;
            }

            KCTDebug.Log("VAB Rates:");
            foreach (double v in VABRates)
            {
                KCTDebug.Log(v);
            }

            KCTDebug.Log("SPH Rates:");
            foreach (double v in SPHRates)
            {
                KCTDebug.Log(v);
            }
        }

        public void RecalculateUpgradedBuildRates()
        {
            UpVABRates.Clear();
            UpSPHRates.Clear();
            double rate = 0.1;
            int index = 0;
            while (rate > 0)
            {
                rate = KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.VAB, index, this, true);
                if (rate >= 0 && (index == 0 || VABRates[index - 1] > 0))
                    UpVABRates.Add(rate);
                else
                    break;
                index++;
            }
            rate = 0.1;
            index = 0;
            while (rate > 0)
            {
                rate = KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.SPH, index, this, true);
                if (rate >= 0 && (index == 0 || SPHRates[index - 1] > 0))
                    UpSPHRates.Add(rate);
                else
                    break;
                index++;
            }
        }

        public void SwitchToPrevLaunchPad()
        {
            SwitchLaunchPad(false);
        }

        public void SwitchToNextLaunchPad()
        {
            SwitchLaunchPad(true);
        }

        public void SwitchLaunchPad(bool forwardDirection)
        {
            if (KCT_GameStates.ActiveKSC.LaunchPadCount < 2) return;

            int activePadCount = LaunchPads.Count(p => p.level >= 0);
            if (activePadCount < 2) return;

            int idx = KCT_GameStates.ActiveKSC.ActiveLaunchPadID;
            KCT_LaunchPad pad;
            do
            {
                if (forwardDirection)
                {
                    idx = (idx + 1) % LaunchPads.Count;
                }
                else
                {
                    //Simple fix for mod function being "weird" in the negative direction
                    //http://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain
                    idx = ((idx - 1) % LaunchPads.Count + LaunchPads.Count) % LaunchPads.Count;
                }
                pad = LaunchPads[idx];
            } while (pad.level < 0);

            KCT_GameStates.ActiveKSC.SwitchLaunchPad(idx);
        }

        public void SwitchLaunchPad(int LP_ID, bool updateDestrNode = true)
        {
            //set the active LP's new state
            //activate new pad

            //LaunchPads[ActiveLaunchPadID].level = KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.LaunchPad);
            //LaunchPads[ActiveLaunchPadID].destroyed = !KCT_Utilities.LaunchFacilityIntact(KCT_BuildListVessel.ListType.VAB); //Might want to remove this as well
            if (updateDestrNode)
                ActiveLPInstance?.RefreshDestructionNode();

            LaunchPads[LP_ID].SetActive();
        }

        /// <summary>
        /// Finds the highest level LaunchPad on the KSC
        /// </summary>
        /// <returns>The instance of the highest level LaunchPad</returns>
        public KCT_LaunchPad GetHighestLevelLaunchPad()
        {
            KCT_LaunchPad highest = LaunchPads[0];
            for (int i = LaunchPads.Count - 1; i >= 0; i--)
            {
                KCT_LaunchPad pad = LaunchPads[i];
            
            //foreach (KCT_LaunchPad pad in LaunchPads)
            //{
                if (pad.level > highest.level)
                {
                    highest = pad;
                }
            }
            return highest;
        }

        public ConfigNode AsConfigNode()
        {
            KCTDebug.Log("Saving KSC "+KSCName);
            ConfigNode node = new ConfigNode("KSC");
            node.AddValue("KSCName", KSCName);
            node.AddValue("ActiveLPID", ActiveLaunchPadID);
            
            ConfigNode vabup = new ConfigNode("VABUpgrades");
            foreach (int upgrade in VABUpgrades)
            {
                vabup.AddValue("Upgrade", upgrade.ToString());
            }
            node.AddNode(vabup);
            
            ConfigNode sphup = new ConfigNode("SPHUpgrades");
            foreach (int upgrade in SPHUpgrades)
            {
                sphup.AddValue("Upgrade", upgrade.ToString());
            }
            node.AddNode(sphup);
            
            ConfigNode rdup = new ConfigNode("RDUpgrades");
            foreach (int upgrade in RDUpgrades)
            {
                rdup.AddValue("Upgrade", upgrade.ToString());
            }
            node.AddNode(rdup);
            
            ConfigNode vabl = new ConfigNode("VABList");
            foreach (KCT_BuildListVessel blv in VABList)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                vabl.AddNode(cnTemp);
            }
            node.AddNode(vabl);
            
            ConfigNode sphl = new ConfigNode("SPHList");
            foreach (KCT_BuildListVessel blv in SPHList)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                sphl.AddNode(cnTemp);
            }
            node.AddNode(sphl);
            
            ConfigNode vabwh = new ConfigNode("VABWarehouse");
            foreach (KCT_BuildListVessel blv in VABWarehouse)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                vabwh.AddNode(cnTemp);
            }
            node.AddNode(vabwh);
            
            ConfigNode sphwh = new ConfigNode("SPHWarehouse");
            foreach (KCT_BuildListVessel blv in SPHWarehouse)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                sphwh.AddNode(cnTemp);
            }
            node.AddNode(sphwh);

            ConfigNode upgradeables = new ConfigNode("KSCTech");
            foreach (KCT_UpgradingBuilding buildingTech in KSCTech)
            {
                ConfigNode bT = new ConfigNode("UpgradingBuilding");
                buildingTech.Save(bT);
                upgradeables.AddNode(bT);
            }
            node.AddNode(upgradeables);

            ConfigNode vabplans = new ConfigNode("VABPlans");
            foreach (KCT_BuildListVessel blv in VABPlans.Values)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                vabplans.AddNode(cnTemp);
            }
            node.AddNode(vabplans);

            ConfigNode sphplans = new ConfigNode("SPHPlans");
            foreach (KCT_BuildListVessel blv in SPHPlans.Values)
            {
                KCT_BuildListStorage.BuildListItem ship = new KCT_BuildListStorage.BuildListItem();
                ship.FromBuildListVessel(blv);
                ConfigNode cnTemp = new ConfigNode("KCTVessel");
                cnTemp = ConfigNode.CreateConfigFromObject(ship, cnTemp);
                ConfigNode shipNode = new ConfigNode("ShipNode");
                blv.shipNode.CopyTo(shipNode);
                cnTemp.AddNode(shipNode);
                sphplans.AddNode(cnTemp);
            }
            node.AddNode(sphplans);

            ConfigNode RRCN = new ConfigNode("Recon_Rollout");
            foreach (KCT_Recon_Rollout rr in Recon_Rollout)
            {
                ConfigNode rrCN = new ConfigNode("Recon_Rollout_Item");
                rrCN = ConfigNode.CreateConfigFromObject(rr, rrCN);
                RRCN.AddNode(rrCN);
            }
            node.AddNode(RRCN);

            ConfigNode APCN = new ConfigNode("Airlaunch_Prep");
            foreach (KCT_AirlaunchPrep ap in AirlaunchPrep)
            {
                ConfigNode cn = new ConfigNode("Airlaunch_Prep_Item");
                cn = ConfigNode.CreateConfigFromObject(ap, cn);
                APCN.AddNode(cn);
            }
            node.AddNode(APCN);

            ConfigNode LPs = new ConfigNode("LaunchPads");
            foreach (KCT_LaunchPad lp in LaunchPads)
            {
                ConfigNode lpCN = lp.AsConfigNode();
                lpCN.AddNode(lp.DestructionNode);
                LPs.AddNode(lpCN);
            }
            node.AddNode(LPs);

            //Cache the regular rates
            ConfigNode CachedVABRates = new ConfigNode("VABRateCache");
            foreach (double rate in VABRates)
            {
                CachedVABRates.AddValue("rate", rate);
            }
            node.AddNode(CachedVABRates);

            ConfigNode CachedSPHRates = new ConfigNode("SPHRateCache");
            foreach (double rate in SPHRates)
            {
                CachedSPHRates.AddValue("rate", rate);
            }
            node.AddNode(CachedSPHRates);
            return node;
        }

        public KCT_KSC FromConfigNode(ConfigNode node)
        {
            VABUpgrades.Clear();
            SPHUpgrades.Clear();
            RDUpgrades.Clear();
            VABList.Clear();
            VABWarehouse.Clear();
            SPHList.Clear();
            SPHWarehouse.Clear();
            VABPlans.Clear();
            SPHPlans.Clear();
            KSCTech.Clear();
            //TechList.Clear();
            Recon_Rollout.Clear();
            AirlaunchPrep.Clear();
            VABRates.Clear();
            SPHRates.Clear();

            this.KSCName = node.GetValue("KSCName");
            if (!int.TryParse(node.GetValue("ActiveLPID"), out this.ActiveLaunchPadID))
                this.ActiveLaunchPadID = 0;
            ConfigNode vabup = node.GetNode("VABUpgrades");
            foreach (string upgrade in vabup.GetValues("Upgrade"))
            {
                this.VABUpgrades.Add(int.Parse(upgrade));
            }
            ConfigNode sphup = node.GetNode("SPHUpgrades");
            foreach (string upgrade in sphup.GetValues("Upgrade"))
            {
                this.SPHUpgrades.Add(int.Parse(upgrade));
            }
            ConfigNode rdup = node.GetNode("RDUpgrades");
            foreach (string upgrade in rdup.GetValues("Upgrade"))
            {
                this.RDUpgrades.Add(int.Parse(upgrade));
            }

            ConfigNode tmp = node.GetNode("VABList");
            foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
            {
                KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                ConfigNode.LoadObjectFromConfig(listItem, vessel);
                KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                blv.shipNode = vessel.GetNode("ShipNode");
                blv.KSC = this;
                this.VABList.Add(blv);
            }

            tmp = node.GetNode("SPHList");
            foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
            {
                KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                ConfigNode.LoadObjectFromConfig(listItem, vessel);
                KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                blv.shipNode = vessel.GetNode("ShipNode");
                blv.KSC = this;
                this.SPHList.Add(blv);
            }

            tmp = node.GetNode("VABWarehouse");
            foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
            {
                KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                ConfigNode.LoadObjectFromConfig(listItem, vessel);
                KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                blv.shipNode = vessel.GetNode("ShipNode");
                blv.KSC = this;
                this.VABWarehouse.Add(blv);
            }

            tmp = node.GetNode("SPHWarehouse");
            foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
            {
                KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                ConfigNode.LoadObjectFromConfig(listItem, vessel);
                KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                blv.shipNode = vessel.GetNode("ShipNode");
                blv.KSC = this;
                this.SPHWarehouse.Add(blv);
            }

            if (node.TryGetNode("VABPlans", ref tmp))
            {
                if (tmp.HasNode("KCTVessel"))
                foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
                {
                    KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                    ConfigNode.LoadObjectFromConfig(listItem, vessel);
                    KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                    blv.shipNode = vessel.GetNode("ShipNode");
                    blv.KSC = this;
                    if (this.VABPlans.ContainsKey(blv.shipName))
                        this.VABPlans.Remove(blv.shipName);
                    
                    this.VABPlans.Add(blv.shipName, blv);
                }
            }

            if (node.TryGetNode("SPHPlans", ref tmp))
            {
                if (tmp.HasNode("KCTVessel"))
                foreach (ConfigNode vessel in tmp.GetNodes("KCTVessel"))
                {
                    KCT_BuildListStorage.BuildListItem listItem = new KCT_BuildListStorage.BuildListItem();
                    ConfigNode.LoadObjectFromConfig(listItem, vessel);
                    KCT_BuildListVessel blv = listItem.ToBuildListVessel();
                    blv.shipNode = vessel.GetNode("ShipNode");
                    blv.KSC = this;
                    if (this.SPHPlans.ContainsKey(blv.shipName))
                        this.SPHPlans.Remove(blv.shipName);
                    this.SPHPlans.Add(blv.shipName, blv);
                }
            }

            tmp = node.GetNode("Recon_Rollout");
            foreach (ConfigNode RRCN in tmp.GetNodes("Recon_Rollout_Item"))
            {
                KCT_Recon_Rollout tempRR = new KCT_Recon_Rollout();
                ConfigNode.LoadObjectFromConfig(tempRR, RRCN);
                Recon_Rollout.Add(tempRR);
            }

            if (node.TryGetNode("Airlaunch_Prep", ref tmp))
            {
                foreach (ConfigNode APCN in tmp.GetNodes("Airlaunch_Prep_Item"))
                {
                    KCT_AirlaunchPrep temp = new KCT_AirlaunchPrep();
                    ConfigNode.LoadObjectFromConfig(temp, APCN);
                    AirlaunchPrep.Add(temp);
                }
            }

            if (node.HasNode("KSCTech"))
            {
                tmp = node.GetNode("KSCTech");
                foreach (ConfigNode upBuild in tmp.GetNodes("UpgradingBuilding"))
                {
                    KCT_UpgradingBuilding tempUP = new KCT_UpgradingBuilding();
                    tempUP.Load(upBuild);
                    KSCTech.Add(tempUP);
                }
            }

            if (node.HasNode("LaunchPads"))
            {
                LaunchPads.Clear();
                tmp = node.GetNode("LaunchPads");
                foreach (ConfigNode LP in tmp.GetNodes("KCT_LaunchPad"))
                {
                    KCT_LaunchPad tempLP = new KCT_LaunchPad("LP0");
                    ConfigNode.LoadObjectFromConfig(tempLP, LP);
                    tempLP.DestructionNode = LP.GetNode("DestructionState");
                    LaunchPads.Add(tempLP);
                }
            }

            if (node.HasNode("VABRateCache"))
            {
                foreach (string rate in node.GetNode("VABRateCache").GetValues("rate"))
                {
                    double r;
                    if (double.TryParse(rate, out r))
                    {
                        VABRates.Add(r);
                    }
                }
            }

            if (node.HasNode("SPHRateCache"))
            {
                foreach (string rate in node.GetNode("SPHRateCache").GetValues("rate"))
                {
                    double r;
                    if (double.TryParse(rate, out r))
                    {
                        SPHRates.Add(r);
                    }
                }
            }

            return this;
        }
    }
}
