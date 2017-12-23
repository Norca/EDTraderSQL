using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace EDTraderSQL
{
    public partial class TradingCentral : Form
    {
        // 3 Startup
        // 3.1 Cargo
        private void JournalCargo(string jstr)
        {
            CargoEvent cargo = JsonConvert.DeserializeObject<CargoEvent>(jstr);
            // Reset stored list of Cargo
            using (var db = new EDTSQLEntities())
            {
                var allrecords = db.CargoHolds;
                foreach (CargoHold item in allrecords)
                {
                    item.StockChecked = false; //false
                }
                db.SaveChanges();
                DisplayCargo();

                foreach (var inventitem in cargo.Inventory)
                {
                    //Lookup NiceName of Cargo Item from Commodities table
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == inventitem.Name).First();
                    //Locate Cargo Item in CargoHold table
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper()))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper()).First();
                        cargoUpdate.StockChecked = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new CargoHold() { CommodityName = commod.CommodityName, Qty = inventitem.Count, AvgPurchasePrice = 0, Stolen = false, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                // Delete Cargo entries not found on load of Journal
                db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.StockChecked == false));
                db.SaveChanges();
                db.Dispose();
            }
        }
        // 3.4 Materials
        private void JournalMaterials(string jstr)
        {
            MaterialsEvent materials = JsonConvert.DeserializeObject<MaterialsEvent>(jstr);
            // Reset stored list of Materials
            using (var db = new EDTSQLEntities())
            {
                if (materials.Raw.Count() > 0 || materials.Manufactured.Count() > 0 || materials.Encoded.Count() > 0)
                {
                    // Delete all existing records
                    db.Materials.RemoveRange(db.Materials);
                    db.SaveChanges();
                    // Add reported materials to table
                    foreach (var item in materials.Raw)
                    {
                        MaterialList matlookup = db.MaterialLists.Where(p => p.EDCodeName == item.Name).First();
                        db.Materials.Add(new Material() { MaterialName = matlookup.MaterialName, Quantity = item.Count, MaterialGroup = "Raw" });
                    }
                    foreach (var item in materials.Manufactured)
                    {
                        MaterialList matlookup = db.MaterialLists.Where(p => p.EDCodeName == item.Name).First();
                        db.Materials.Add(new Material() { MaterialName = matlookup.MaterialName, Quantity = item.Count, MaterialGroup = "Manufactured" });
                    }
                    foreach (var item in materials.Encoded)
                    {
                        MaterialList matlookup = db.MaterialLists.Where(p => p.EDCodeName == item.Name).First();
                        db.Materials.Add(new Material() { MaterialName = matlookup.MaterialName, Quantity = item.Count, MaterialGroup = "Encoded" });
                    }
                    db.SaveChanges();
                }
                db.Dispose();
            }
        }

        // 4 Travel
        // 4.1 Docked
        private void JournalDocked(string jstr)
        {
            DockedEvent docked = JsonConvert.DeserializeObject<DockedEvent>(jstr);
            //Display System & Station from event
            lblStarSystem.Text = docked.StarSystem;
            CurrSystem = docked.StarSystem;
            lblStation.Text = docked.StationName;
            lblEconomy.Text = docked.StationEconomy_Localised;
            // Check for Station in Database
            using (var db = new EDTSQLEntities())
            {
                StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == docked.StarSystem);
                if (db.Stations.Any(o => o.SystemID == starsystem.SystemID && o.StationName == docked.StationName))
                {
                    //Update Station record
                    Station stationUpdate = db.Stations.Where(p => p.SystemID == starsystem.SystemID && p.StationName == docked.StationName).First();
                    stationUpdate.Economy = docked.StationEconomy_Localised;
                    db.SaveChanges();
                }
                else
                {
                    //Add Station record
                    Station stationAdd = new Station();
                    stationAdd.StationID = 0;
                    stationAdd.SystemID = starsystem.SystemID;
                    stationAdd.StationName = docked.StationName;
                    stationAdd.StationType = docked.StationType;
                    stationAdd.Economy = docked.StationEconomy_Localised;
                    stationAdd.BlackMarketAvailable = false;
                    db.Stations.Add(stationAdd);
                    db.SaveChanges();
                    lblDBStations.Text = db.Stations.Count().ToString();
                }
                db.Dispose();
            }
        }
        //4.7 FSDJump
        private void JournalFSDJump(string jstr)
        {
            FSDJumpEvent fsdjump = JsonConvert.DeserializeObject<FSDJumpEvent>(jstr);
            lblStarSystem.Text = fsdjump.StarSystem;
            CurrSystem = fsdjump.StarSystem;
            lblStation.Text = "IN SPACE";
            lblFaction.Text = fsdjump.SystemFaction;
            lblEconomy.Text = "";
            // Check for System in Database
            using (var db = new EDTSQLEntities())
            {
                if (db.StarSystems.Any(o => o.SystemName == fsdjump.StarSystem))
                {
                    StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == fsdjump.StarSystem);
                    //Delete all existing Factions in System
                    if (fsdjump.Factions != null)
                    {
                        db.Factions.RemoveRange(db.Factions.Where(x => x.StarSystemID == starsystem.SystemID));
                        db.SaveChanges();

                        //Store array of Factions present in System
                        foreach (var item in fsdjump.Factions)
                        {
                            Faction addFact = new Faction();
                            addFact.SSIDFaction = starsystem.SystemID.ToString() + "-" + item.Name;
                            addFact.StarSystemID = starsystem.SystemID;
                            addFact.FactionName = item.Name;
                            db.Factions.Add(addFact);
                        }
                        db.SaveChanges();
                    }

                    //Update System record
                    StarSystem systemUpdate = db.StarSystems.Where(p => p.SystemName == fsdjump.StarSystem).First();
                    systemUpdate.Allegiance = fsdjump.SystemAllegiance;
                    systemUpdate.Economy = fsdjump.SystemEconomy_Localised;
                    systemUpdate.Government = fsdjump.SystemGovernment_Localised;
                    systemUpdate.SystemFaction = fsdjump.SystemFaction;
                    systemUpdate.TimesVisited = systemUpdate.TimesVisited + 1;
                    db.SaveChanges();
                    lblStarSystem.Text = lblStarSystem.Text + " = Visits (" + systemUpdate.TimesVisited.ToString() + ")";
                }
                else
                {
                    //Add System record
                    StarSystem systemAdd = new StarSystem();
                    systemAdd.SystemID = 0;
                    systemAdd.SystemName = fsdjump.StarSystem;
                    systemAdd.SpaceX = fsdjump.StarPos[0];
                    systemAdd.SpaceY = fsdjump.StarPos[1];
                    systemAdd.SpaceZ = fsdjump.StarPos[2];
                    systemAdd.Allegiance = fsdjump.SystemAllegiance;
                    systemAdd.Economy = fsdjump.SystemEconomy_Localised;
                    systemAdd.Government = fsdjump.SystemGovernment_Localised;
                    systemAdd.SystemFaction = fsdjump.SystemFaction;
                    systemAdd.TimesVisited = 1;
                    db.StarSystems.Add(systemAdd);
                    db.SaveChanges();
                    lblDBSystems.Text = db.StarSystems.Count().ToString();
                    lblStarSystem.Text = lblStarSystem.Text + " = Visits (1)";

                    StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == fsdjump.StarSystem);
                    if (fsdjump.Factions != null)
                    {
                        //Store array of Factions present in System
                        foreach (var item in fsdjump.Factions)
                        {
                            Faction addFact = new Faction();
                            addFact.SSIDFaction = starsystem.SystemID.ToString() + "-" + item.Name;
                            addFact.StarSystemID = starsystem.SystemID;
                            addFact.FactionName = item.Name;
                            db.Factions.Add(addFact);
                        }
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }
        }
        // 4.9 Location
        private void JournalLocation(string jstr)
        {
            LocationEvent location = JsonConvert.DeserializeObject<LocationEvent>(jstr);
            //Test for in Training
            if (location.StarSystem == "Training")
            {
                return;
            }

            //Display System & Station from event
            lblStarSystem.Text = location.StarSystem;
            CurrSystem = location.StarSystem;
            lblStation.Text = location.StationName;

            //Update System Faction and display
            using (var db = new EDTSQLEntities())
            {
                lblFaction.Text = location.SystemFaction;
                try
                {
                    try
                    {
                        StarSystem ssystem = db.StarSystems.Single(p => p.SystemName == location.StarSystem);
                    }
                    catch
                    {
                        //Add System record
                        StarSystem systemAdd = new StarSystem();
                        systemAdd.SystemID = 0;
                        systemAdd.SystemName = location.StarSystem;
                        systemAdd.SpaceX = location.StarPos[0];
                        systemAdd.SpaceY = location.StarPos[1];
                        systemAdd.SpaceZ = location.StarPos[2];
                        systemAdd.Allegiance = location.SystemAllegiance;
                        systemAdd.Economy = location.SystemEconomy_Localised;
                        systemAdd.Government = location.SystemGovernment_Localised;
                        systemAdd.SystemFaction = location.SystemFaction;
                        db.StarSystems.Add(systemAdd);
                        db.SaveChanges();
                        lblDBSystems.Text = db.StarSystems.Count().ToString();
                    }
                    StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == location.StarSystem);
                    if (starsystem.SystemFaction != location.SystemFaction)
                    {
                        starsystem.SystemFaction = location.SystemFaction;
                        db.SaveChanges();
                    }

                    //Delete all existing Factions in System
                    if (location.Factions != null)
                    {
                        db.Factions.RemoveRange(db.Factions.Where(x => x.StarSystemID == starsystem.SystemID));
                        db.SaveChanges();
                        //Store array of Factions present in System
                        foreach (var item in location.Factions)
                        {
                            Faction addFact = new Faction();
                            addFact.SSIDFaction = starsystem.SystemID.ToString() + "-" + item.Name;
                            addFact.StarSystemID = starsystem.SystemID;
                            addFact.FactionName = item.Name;
                            db.Factions.Add(addFact);
                        }
                        db.SaveChanges();
                    }
                    //Display current System Faction
                    lblFaction.Text = starsystem.SystemFaction;
                }
                catch
                {
                    lblStarSystem.Text = lblStarSystem.Text + " = not in DataBase";
                    lblStation.Text = "";
                    lblFaction.Text = "";
                }
                db.Dispose();
            }
        }
        //4.14 Undocked
        private void JournalUndocked()
        {
            lblStation.Text = "IN SPACE";
            lblEconomy.Text = "";
            lvSupply.Items.Clear();
            lvDemand.Items.Clear();
        }

        // 5 Combat
        // 5.3 Died
        private void JournalDied()
        {
            using (var db = new EDTSQLEntities())
            {
                var allrecords = db.CargoHolds;
                foreach (CargoHold item in allrecords)
                {
                    item.StockChecked = false; //false
                }
                db.SaveChanges();

                // Delete Cargo entries not found on load of Journal
                db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.StockChecked == false));
                db.SaveChanges();
                db.Dispose();
            }
        }

        // 6 Exploration
        // 6.2 MaterialCollected
        private void JournalMaterialCollected(string jstr)
        {
            MaterialCollectedEvent materialcollected = JsonConvert.DeserializeObject<MaterialCollectedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                MaterialList mater = db.MaterialLists.Where(p => p.EDCodeName == materialcollected.Name).First();
                if (db.Materials.Any(o => o.MaterialName == mater.MaterialName))
                {
                    //Update Materials record
                    Material materialUpdate = db.Materials.Where(o => o.MaterialName == mater.MaterialName).First();
                    materialUpdate.Quantity = materialUpdate.Quantity + materialcollected.Count;
                }
                else
                {
                    //Add Materials record
                    db.Materials.Add(new Material() { MaterialName = mater.MaterialName, Quantity = materialcollected.Count });
                }
                db.SaveChanges();
                db.Dispose();
            }
        }
        // 6.3 MaterialDiscarded
        private void JournalMaterialDiscarded(string jstr)
        {
            MaterialDiscardedEvent materialdiscarded = JsonConvert.DeserializeObject<MaterialDiscardedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                MaterialList mater = db.MaterialLists.Where(p => p.EDCodeName == materialdiscarded.Name).First();
                if (db.Materials.Any(o => o.MaterialName == mater.MaterialName))
                {
                    Material materialitem = db.Materials.Where(o => o.MaterialName == mater.MaterialName).First();
                    if (materialitem.Quantity == materialdiscarded.Count)
                    {
                        //Remove item from materials
                        db.Materials.RemoveRange(db.Materials.Where(o => o.MaterialName == mater.MaterialName));
                    }
                    else
                    {
                        //Update Materials record
                        materialitem.Quantity = materialitem.Quantity - materialdiscarded.Count;
                    }
                    db.SaveChanges();
                }
                db.Dispose();
            }
        }

        // 7 Trade
        // 7.2 CollectCargo
        private void JournalCollectCargo(string jstr)
        {
            CollectCargoEvent collectcargo = JsonConvert.DeserializeObject<CollectCargoEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                //Lookup NiceName of Cargo Item from Commodities table
                try
                {
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == collectcargo.Type).First();
                    // Commodity commod = db.Commodities.Where(p => p.EDCodeName == collectcargo.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.Stolen == collectcargo.Stolen))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.Stolen == collectcargo.Stolen).First();
                        cargoUpdate.Qty = cargoUpdate.Qty + 1;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record because record of matching stolen status not found
                        db.CargoHolds.Add(new EDTraderSQL.CargoHold() { CommodityName = commod.CommodityName, Qty = 1, AvgPurchasePrice = 0, Stolen = collectcargo.Stolen, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                catch
                {
                    RareCommodity commod = db.RareCommodities.Where(p => p.EDCodeName == collectcargo.Type).First();
                    // Commodity commod = db.Commodities.Where(p => p.EDCodeName == collectcargo.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.Stolen == collectcargo.Stolen))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.Stolen == collectcargo.Stolen).First();
                        cargoUpdate.Qty = cargoUpdate.Qty + 1;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record because record of matching stolen status not found
                        db.CargoHolds.Add(new EDTraderSQL.CargoHold() { CommodityName = commod.CommodityName, Qty = 1, AvgPurchasePrice = 0, Stolen = collectcargo.Stolen, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }
        }
        // 7.3 EjectCargo
        private void JournalEjectCargo(string jstr)
        {
            EjectCargoEvent ejectcargo = JsonConvert.DeserializeObject<EjectCargoEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                //Lookup NiceName of Cargo Item from Commodities table
                try
                {
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == ejectcargo.Type).First();
                    // Commodity commod = db.Commodities.Where(p => p.EDCodeName == ejectcargo.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        if (cargoitem.Qty == ejectcargo.Count)
                        {
                            //Remove item of cargo
                            db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                            db.SaveChanges();
                        }
                        else
                        {
                            //Update quantity of cargo
                            cargoitem.Qty = cargoitem.Qty - ejectcargo.Count;
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == true))
                        {
                            CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == true).First();
                            if (cargoitem.Qty == ejectcargo.Count)
                            {
                                //Remove item of cargo
                                db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                                db.SaveChanges();
                            }
                            else
                            {
                                //Update quantity of cargo
                                cargoitem.Qty = cargoitem.Qty - ejectcargo.Count;
                                db.SaveChanges();
                            }
                        }
                    }
                }
                catch
                {
                    RareCommodity commod = db.RareCommodities.Where(p => p.EDCodeName == ejectcargo.Type).First();
                    // Commodity commod = db.Commodities.Where(p => p.EDCodeName == ejectcargo.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        if (cargoitem.Qty == ejectcargo.Count)
                        {
                            //Remove item of cargo
                            db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                            db.SaveChanges();
                        }
                        else
                        {
                            //Update quantity of cargo
                            cargoitem.Qty = cargoitem.Qty - ejectcargo.Count;
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == true))
                        {
                            CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == true).First();
                            if (cargoitem.Qty == ejectcargo.Count)
                            {
                                //Remove item of cargo
                                db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                                db.SaveChanges();
                            }
                            else
                            {
                                //Update quantity of cargo
                                cargoitem.Qty = cargoitem.Qty - ejectcargo.Count;
                                db.SaveChanges();
                            }
                        }
                    }
                }
                db.Dispose();
            }
        }
        // 7.4 MarketBuy
        private void JournalMarketBuy(string jstr)
        {
            MarketBuyEvent marketbuy = JsonConvert.DeserializeObject<MarketBuyEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                //Lookup NiceName of Cargo Item from Commodities table
                try
                {
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketbuy.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketbuy.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        var existingCargoValue = cargoUpdate.AvgPurchasePrice * cargoUpdate.Qty;
                        cargoUpdate.Qty = cargoUpdate.Qty + marketbuy.Count;
                        cargoUpdate.AvgPurchasePrice = (existingCargoValue + (marketbuy.BuyPrice * marketbuy.Count)) / cargoUpdate.Qty;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new CargoHold() { CommodityName = commod.CommodityName, Qty = marketbuy.Count, AvgPurchasePrice = marketbuy.BuyPrice, Stolen = false, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                catch
                {
                    RareCommodity commod = db.RareCommodities.Where(p => p.EDCodeName == marketbuy.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketbuy.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        var existingCargoValue = cargoUpdate.AvgPurchasePrice * cargoUpdate.Qty;
                        cargoUpdate.Qty = cargoUpdate.Qty + marketbuy.Count;
                        cargoUpdate.AvgPurchasePrice = (existingCargoValue + (marketbuy.BuyPrice * marketbuy.Count)) / cargoUpdate.Qty;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new CargoHold() { CommodityName = commod.CommodityName, Qty = marketbuy.Count, AvgPurchasePrice = marketbuy.BuyPrice, Stolen = false, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }
        }
        // 7.5 MarketSell
        private void JournalMarketSell(string jstr)
        {
            MarketSellEvent marketsell = JsonConvert.DeserializeObject<MarketSellEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                //Lookup NiceName of Cargo Item from Commodities table
                try
                {
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketsell.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketsell.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        if (cargoitem.Qty == marketsell.Count)
                        {
                            //Remove item of cargo
                            db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                            db.SaveChanges();
                        }
                        else
                        {
                            //Update quantity of cargo
                            cargoitem.Qty = cargoitem.Qty - marketsell.Count;
                            db.SaveChanges();
                        }
                    }
                }
                catch
                {
                    RareCommodity commod = db.RareCommodities.Where(p => p.EDCodeName == marketsell.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == marketsell.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        if (cargoitem.Qty == marketsell.Count)
                        {
                            //Remove item of cargo
                            db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                            db.SaveChanges();
                        }
                        else
                        {
                            //Update quantity of cargo
                            cargoitem.Qty = cargoitem.Qty - marketsell.Count;
                            db.SaveChanges();
                        }
                    }
                }
                db.Dispose();
            }

        }
        // 7.6 Miningrefined
        private void JournalMiningRefined(string jstr)
        {
            MiningRefinedEvent miningrefined = JsonConvert.DeserializeObject<MiningRefinedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                //Lookup NiceName of Mined Item from Commodities table
                try
                {
                    Commodity commod = db.Commodities.Where(p => p.EDCodeName == miningrefined.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == miningrefined.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        cargoUpdate.Qty = cargoUpdate.Qty + 1;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new EDTraderSQL.CargoHold() { CommodityName = commod.CommodityName, Qty = 1, AvgPurchasePrice = 0, Stolen = false, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                catch
                {
                    RareCommodity commod = db.RareCommodities.Where(p => p.EDCodeName == miningrefined.Type).First();
                    //Commodity commod = db.Commodities.Where(p => p.EDCodeName == miningrefined.Type).First();
                    if (db.CargoHolds.Any(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false))
                    {
                        //Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName.ToUpper() == commod.CommodityName.ToUpper() && o.MissionCargo == false).First();
                        cargoUpdate.Qty = cargoUpdate.Qty + 1;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new EDTraderSQL.CargoHold() { CommodityName = commod.CommodityName, Qty = 1, AvgPurchasePrice = 0, Stolen = false, StockChecked = true, MissionCargo = false });
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }

        }

        // 8 Station Services
        // 8.16 MissionAccepted
        private void JournalMissionAccepted(string jstr)
        {
            MissionAcceptedEvent missionaccept = JsonConvert.DeserializeObject<MissionAcceptedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                if (db.ActiveMissions.Any(o => o.MissionID == missionaccept.MissionID))
                {
                    return;
                }

                DateTimeOffset dto = new DateTimeOffset(missionaccept.getExpiryAsDate());

                if (missionaccept.Commodity_Localised != null)
                {
                    missionaccept.Commodity_Localised = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(missionaccept.Commodity_Localised);
                }

                string MissType = "";
                if (missionaccept.Name.Contains("Collect"))
                {
                    MissType = "Collect";
                    db.ActiveMissions.Add(new ActiveMission() { MissionID = missionaccept.MissionID, MissionCargo = $"{missionaccept.Commodity_Localised} ({missionaccept.Count.ToString()}) - Required", MissionType = MissType, DestinationSystem = missionaccept.DestinationSystem, DestinationStation = missionaccept.DestinationStation, Expiry = Convert.ToInt32(dto.ToUnixTimeSeconds()) });
                }
                if (missionaccept.Name.Contains("Delivery"))
                {
                    MissType = "Delivery";
                    db.ActiveMissions.Add(new ActiveMission() { MissionID = missionaccept.MissionID, MissionCargo = $"{missionaccept.Commodity_Localised} ({missionaccept.Count.ToString()})", MissionType = MissType, DestinationSystem = missionaccept.DestinationSystem, DestinationStation = missionaccept.DestinationStation, Expiry = Convert.ToInt32(dto.ToUnixTimeSeconds()) });
                }
                if (missionaccept.Name.Contains("Courier"))
                {
                    MissType = "Courier";
                    db.ActiveMissions.Add(new ActiveMission() { MissionID = missionaccept.MissionID, MissionCargo = "Data Only", MissionType = MissType, DestinationSystem = missionaccept.DestinationSystem, DestinationStation = missionaccept.DestinationStation, Expiry = Convert.ToInt32(dto.ToUnixTimeSeconds()) });
                }
                if (missionaccept.Name.Contains("Salvage"))
                {
                    MissType = "Salvage";
                    db.ActiveMissions.Add(new ActiveMission() { MissionID = missionaccept.MissionID, MissionCargo = $"{missionaccept.Commodity_Localised} ({missionaccept.Count.ToString()})", MissionType = MissType, DestinationSystem = missionaccept.DestinationSystem, DestinationStation = missionaccept.DestinationStation, Expiry = Convert.ToInt32(dto.ToUnixTimeSeconds()) });
                }
                db.SaveChanges();

                if (missionaccept.Name.Contains("Delivery") == true)
                {
                    if (db.CargoHolds.Any(o => o.CommodityName == missionaccept.Commodity_Localised))
                    {
                        // Update CargoHold record
                        CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName == missionaccept.Commodity_Localised && o.MissionCargo == true).First();
                        cargoUpdate.Qty = cargoUpdate.Qty + missionaccept.Count;
                        db.SaveChanges();
                    }
                    else
                    {
                        //Add CargoHold record
                        db.CargoHolds.Add(new CargoHold() { CommodityName = missionaccept.Commodity_Localised, Qty = missionaccept.Count, AvgPurchasePrice = 0, Stolen = false, StockChecked = true, MissionCargo = true });
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }
        }
        // 8.17 MissionCompleted
        private void JournalMissionCompleted(string jstr)
        {
            MissionCompletedEvent missioncomplete = JsonConvert.DeserializeObject<MissionCompletedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                if (db.ActiveMissions.Any(o => o.MissionID == missioncomplete.MissionID))
                {
                    // Remove mission
                    db.ActiveMissions.RemoveRange(db.ActiveMissions.Where(o => o.MissionID == missioncomplete.MissionID));
                    db.SaveChanges();
                }

                if (db.CargoHolds.Any(o => o.CommodityName == missioncomplete.Commodity_Localised && o.MissionCargo == true))
                {
                    CargoHold cargoitem = db.CargoHolds.Where(o => o.CommodityName == missioncomplete.Commodity_Localised && o.MissionCargo == true).First();
                    if (cargoitem.Qty == missioncomplete.Count)
                    {
                        //Remove item of cargo
                        //db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.CommodityName == missioncomplete.Commodity_Localised && o.MissionCargo = true));
                        db.CargoHolds.RemoveRange(db.CargoHolds.Where(o => o.TradeID == cargoitem.TradeID));
                    }
                    else
                    {
                        //Update quantity of cargo
                        cargoitem.Qty = cargoitem.Qty - missioncomplete.Count;
                    }
                    db.SaveChanges();
                }
                if ((missioncomplete.CommodityReward != null) && (missioncomplete.CommodityReward.Count > 0))
                {
                    foreach (var item in missioncomplete.CommodityReward)
                    {
                        Commodity commod = db.Commodities.Where(p => p.EDCodeName == item.Name.ToLower()).First();
                        if (db.CargoHolds.Any(o => o.CommodityName == commod.CommodityName && o.MissionCargo == false))
                        {
                            //Update CargoHold record
                            CargoHold cargoUpdate = db.CargoHolds.Where(o => o.CommodityName == commod.CommodityName && o.MissionCargo == false).First();
                            var existingCargoValue = cargoUpdate.AvgPurchasePrice * cargoUpdate.Qty;
                            cargoUpdate.Qty = cargoUpdate.Qty + item.Count;
                            cargoUpdate.AvgPurchasePrice = existingCargoValue / cargoUpdate.Qty;
                        }
                        else
                        {
                            //Add CargoHold record
                            db.CargoHolds.Add(new CargoHold() { CommodityName = commod.CommodityName, Qty = item.Count, AvgPurchasePrice = 0, Stolen = false, StockChecked = true, MissionCargo = false });
                        }
                        db.SaveChanges();
                    }
                }
                db.Dispose();
            }
        }
        // 8.18 MissionFailed
        private void JournalMissionFailed(string jstr)
        {
            MissionFailedEvent missionfailed = JsonConvert.DeserializeObject<MissionFailedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                if (db.ActiveMissions.Any(o => o.MissionID == missionfailed.MissionID))
                {
                    // Remove Mission
                    db.ActiveMissions.RemoveRange(db.ActiveMissions.Where(o => o.MissionID == missionfailed.MissionID));
                    db.SaveChanges();
                }
                db.Dispose();
            }
        }
        // 8.19 MissionRedirected
        private void MissionRedirected(string jstr)
        {
            MissionRedirectedEvent missionredirect = JsonConvert.DeserializeObject<MissionRedirectedEvent>(jstr);
            using (var db = new EDTSQLEntities())
            {
                if (db.ActiveMissions.Any(o => o.MissionID == missionredirect.MissionID))
                {
                    // Update Mission record
                    ActiveMission missionupdate = db.ActiveMissions.Where(o => o.MissionID == missionredirect.MissionID).First();
                    missionupdate.DestinationStation = missionredirect.NewDestinationStation;
                    missionupdate.DestinationSystem = missionredirect.NewDestinationSystem;
                    db.SaveChanges();
                }
                db.Dispose();
            }
        }
        // 8.26 PayFines
        private void JournalPayFines(string jstr)
        {

        }
        // 8.27 PayLegacyFines
        private void JournalPayLegacyFines(string jstr)
        {

        }
        // 8.34 ScientificResearch
        private void JournalScientificResearch(string jstr)
        {

        }

        // 10 Other Events
        // 10.4 CommitCrime
        private void JournalCommitCrime(string jstr)
        {

        }
        // 10.31 Synthesis
        private void JournalSynthesis(string jstr)
        {

        }
    }
}