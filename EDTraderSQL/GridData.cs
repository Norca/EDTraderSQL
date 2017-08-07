using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EDTraderSQL
{
    public partial class TradingCentral : Form
    {
        private void ObtainTopProductsToBuy()
        {
            using (var db = new EDTSQLEntities())
            {
                UpdateMonitor("Calculating best items to buy");

                StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == CurrSystem); // Get System
                Station station = db.Stations.SingleOrDefault(o => o.SystemID == starsystem.SystemID && o.StationName == lblStation.Text); // Get Station
                // List of items supplied at this station
                List<MarketDetail> topmarketsupplies = db.MarketDetails.Where(n => n.SystemID == starsystem.SystemID && n.StationID == station.StationID && n.SupplyStatus > 0).ToList();
                // Sort list by supply availability
                topmarketsupplies = topmarketsupplies.OrderByDescending(s => s.SupplyStatus).ToList();

                lvSupply.Items.Clear(); // Clear the items in the grid

                FillProductSupplyList(topmarketsupplies); // Fill the list with details of best place to sell the items

                lvSupply.Refresh(); // Display the items in the grid

                db.Dispose();
            }
        }

        private void FillProductSupplyList(List<MarketDetail> sourcelist)
        {
            Dictionary<string, string> StockLevels = new Dictionary<string, string>();
            StockLevels.Add("4", "High");
            StockLevels.Add("2", "Med");
            StockLevels.Add("1", "Low");
            StockLevels.Add("0", "");

            using (var db = new EDTSQLEntities())
            {
                StarSystem currsystem = db.StarSystems.Single(n => n.SystemName == CurrSystem); // Get Current System
                Station currstation = db.Stations.Single(n => n.StationName == lblStation.Text); // Get Current Station

                List<TopDemands> SuppliesWithDemand = new List<TopDemands>();

                int itemsfound = 0;

                foreach (var item in sourcelist)
                {
                    //Find Systems-Stations that need (have Demand for) the product on sale
                    List<MarketDetail> ItemLocationsWithDemand = db.MarketDetails.Where(n => n.CommodityName == item.CommodityName && n.DemandStatus > 0).ToList();

                    List<DemandList> DemandItemList = new List<DemandList>(); // List of Demand Locations for Supply Item with calculated profit
                    foreach (MarketDetail DemandItem in ItemLocationsWithDemand)
                    {
                        StarSystem starsystem = db.StarSystems.Single(n => n.SystemID == DemandItem.SystemID);
                        Station station = db.Stations.Single(n => n.StationID == DemandItem.StationID);
                        // Calculate the distance between current location and possible selling system
                        double distance = Program.SystemDistance(currsystem.SpaceX, currsystem.SpaceY, currsystem.SpaceZ, starsystem.SpaceX, starsystem.SpaceY, starsystem.SpaceZ);
                        // Calculate estimated amount of profit per unit
                        int profit = Convert.ToInt16((DemandItem.SellPrice ?? 0) - (item.BuyPrice ?? 0));

                        int sellingprice = Convert.ToInt16(DemandItem.SellPrice ?? 0);
                        DemandItemList.Add(new DemandList { ProductName = DemandItem.CommodityName, SellSystem = starsystem.SystemName, SellStation = station.StationName, Distance = distance, SellPrice = sellingprice, Profit = profit });
                    }
                    // Sort list by amount of profit , distance
                    DemandItemList = DemandItemList.OrderByDescending(s => s.Profit).ThenBy(s => s.Distance).ToList();

                    // Return the best profit item if one exists
                    if (DemandItemList.Count() > 0)
                    {
                        var TopDemandItem = DemandItemList.First();
                        if (TopDemandItem.Profit > 0)
                        {
                            TopDemands TopItem = new TopDemands()
                            {
                                CommodityName = item.CommodityName,
                                SupplyStatus = item.SupplyStatus.ToString(),
                                BuyPrice = item.BuyPrice.ToString(),
                                SellSystem = TopDemandItem.SellSystem,
                                SellSysDist = TopDemandItem.Distance,
                                SellStation = TopDemandItem.SellStation,
                                SellPrice = TopDemandItem.SellPrice.ToString(),
                                Profit = TopDemandItem.Profit
                            };
                            itemsfound++;
                            lblMonitoredEvent.Text = "Found " + itemsfound + " items";
                            lblMonitoredEvent.Refresh();

                            SuppliesWithDemand.Add(TopItem);
                        }
                    }
                }
                // Sort the List by Supply Status then Profit per unit
                SuppliesWithDemand = SuppliesWithDemand.OrderByDescending(s => s.Profit).ToList();
                // Add the Top 10 to the Supply List
                int i = 1;
                foreach (var prod in SuppliesWithDemand)
                {
                    if (i <= 10)
                    {
                        var listitem = new ListViewItem(new[] { prod.CommodityName, StockLevels[prod.SupplyStatus], prod.BuyPrice, prod.SellSystem, prod.SellSysDist.ToString(), prod.SellStation, prod.SellPrice, prod.Profit.ToString() });
                        lvSupply.Items.Add(listitem);
                        i++;
                    }
                }
                db.Dispose();
            }
        }

        private void ObtainTopProductsDemanded()
        {
            using (var db = new EDTSQLEntities())
            {
                UpdateMonitor("Calculating items in highest demand");

                StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == CurrSystem); // Get System
                Station station = db.Stations.SingleOrDefault(o => o.SystemID == starsystem.SystemID && o.StationName == lblStation.Text); // Get Station
                // List of items demanded by this station
                List<MarketDetail> topmarketdemands = db.MarketDetails.Where(n => n.SystemID == starsystem.SystemID && n.StationID == station.StationID && n.DemandStatus > 0).ToList();
                // Sort list by demand level
                topmarketdemands = topmarketdemands.OrderByDescending(s => s.DemandStatus).ToList();

                lvDemand.Items.Clear(); // Clear the items in the grid

                FillProductDemandList(topmarketdemands); // Fill the list with details of best place to buy the items

                lvDemand.Refresh(); // Display the items in the grid

                db.Dispose();
            }
        }

        private void FillProductDemandList(List<MarketDetail> sourcelist)
        {
            Dictionary<string, string> DemandLevels = new Dictionary<string, string>();
            DemandLevels.Add("4", "High");
            DemandLevels.Add("2", "Med");
            DemandLevels.Add("1", "Low");
            DemandLevels.Add("0", "");

            using (var db = new EDTSQLEntities())
            {
                StarSystem currsystem = db.StarSystems.Single(n => n.SystemName == CurrSystem); // Get Current System
                Station currstation = db.Stations.Single(n => n.StationName == lblStation.Text); // Get Current Station

                List<TopNeeds> DemandsWithKnownPlaces = new List<TopNeeds>();

                int itemsfound = 0;

                foreach (var item in sourcelist)
                {
                    //Find Systems-Stations that need (have Demand for) the product on sale
                    List<MarketDetail> ItemLocationsWithStock = db.MarketDetails.Where(n => n.CommodityName == item.CommodityName && n.SupplyStatus > 0).ToList();

                    List<SupplyList> DemandItemList = new List<SupplyList>(); // List of Supply Locations for Demand Item with calculated profit
                    foreach (MarketDetail SupplyItem in ItemLocationsWithStock)
                    {
                        StarSystem starsystem = db.StarSystems.Single(n => n.SystemID == SupplyItem.SystemID);
                        Station station = db.Stations.Single(n => n.StationID == SupplyItem.StationID);
                        // Calculate the distance between current location and possible selling system
                        double distance = Program.SystemDistance(currsystem.SpaceX, currsystem.SpaceY, currsystem.SpaceZ, starsystem.SpaceX, starsystem.SpaceY, starsystem.SpaceZ);
                        // Calculate estimated amount of profit per unit
                        int profit = Convert.ToInt16((item.SellPrice ?? 0) - (SupplyItem.BuyPrice ?? 0));

                        int buyingprice = Convert.ToInt16(SupplyItem.BuyPrice ?? 0);
                        DemandItemList.Add(new SupplyList { ProductName = SupplyItem.CommodityName, BuyPrice = buyingprice, FromSystem = starsystem.SystemName, FromStation = station.StationName, Distance = distance, Profit = profit });
                    }
                    // Sort list by amount of profit , distance
                    DemandItemList = DemandItemList.OrderByDescending(s => s.Profit).ThenBy(s => s.Distance).ToList();

                    // Return the best profit item if one exists
                    if (DemandItemList.Count() > 0)
                    {
                        var TopSupplyItem = DemandItemList.First();
                        if (TopSupplyItem.Profit > 0)
                        {
                            TopNeeds TopItem = new TopNeeds();
                            TopItem.CommodityName = item.CommodityName;
                            TopItem.DemandStatus = item.DemandStatus.ToString();
                            TopItem.SellPrice = item.SellPrice.ToString();
                            TopItem.FromSystem = TopSupplyItem.FromSystem;
                            TopItem.BuySysDist = TopSupplyItem.Distance;
                            TopItem.FromStation = TopSupplyItem.FromStation;
                            TopItem.BuyPrice = TopSupplyItem.BuyPrice.ToString();
                            TopItem.Profit = TopSupplyItem.Profit;

                            itemsfound++;
                            lblMonitoredEvent.Text = "Found " + itemsfound + " items";
                            lblMonitoredEvent.Refresh();

                            DemandsWithKnownPlaces.Add(TopItem);
                        }
                    }
                }
                // Sort the List by Supply Status then Profit per unit
                DemandsWithKnownPlaces = DemandsWithKnownPlaces.OrderByDescending(s => s.Profit).ToList();
                // Add the Top 10 to the Supply List
                int i = 1;
                foreach (var prod in DemandsWithKnownPlaces)
                {
                    if (i <= 10)
                    {
                        var listitem = new ListViewItem(new[] { prod.CommodityName, DemandLevels[prod.DemandStatus], prod.SellPrice, prod.FromSystem, prod.BuySysDist.ToString(), prod.FromStation, prod.BuyPrice, prod.Profit.ToString() });
                        lvDemand.Items.Add(listitem);
                        i++;
                    }
                }
                db.Dispose();
            }
        }

        private void DisplayCargo()
        {
            using (var db = new EDTSQLEntities())
            {
                if (db.StarSystems.Count() == 0)
                {
                    return;
                }
                if (CurrSystem != null)
                {
                    if (CurrSystem.Contains("=") == true)
                    {
                        int index = CurrSystem.IndexOf("=");
                        CurrSystem = (index > 1 ? CurrSystem.Substring(0, index - 1) : null);
                    }
                }
                if (CurrSystem != null)
                {
                    StarSystem currsystem = new StarSystem();
                    if (CurrSystem == "" | db.StarSystems.Count(n => n.SystemName == CurrSystem) == 0)
                    {
                        currsystem = new StarSystem() { SpaceX = 0, SpaceY = 0, SpaceZ = 0 };
                    }
                    else
                    {
                        currsystem = db.StarSystems.Single(n => n.SystemName == CurrSystem); // Get Current System
                    }

                    lvCargo.Items.Clear();

                    if (db.CargoHolds.Count() > 0)
                    {
                        foreach (CargoHold chitem in db.CargoHolds)
                        {
                            //Find Systems-Stations that need (have Demand for) the product on sale
                            List<MarketDetail> possdemand = db.MarketDetails.Where(n => n.CommodityName == chitem.CommodityName && n.DemandStatus > 0).ToList();

                            List<DemandList> demandlist = new List<DemandList>();
                            foreach (MarketDetail possitem in possdemand)
                            {
                                StarSystem starsystem = db.StarSystems.Single(n => n.SystemID == possitem.SystemID);
                                Station station = db.Stations.Single(n => n.StationID == possitem.StationID);
                                // Calculate the distance between current location and possible selling system
                                double distance = Program.SystemDistance(currsystem.SpaceX, currsystem.SpaceY, currsystem.SpaceZ, starsystem.SpaceX, starsystem.SpaceY, starsystem.SpaceZ);
                                // Calculate estimated amount of profit per unit
                                int profit = Convert.ToInt16((possitem.SellPrice ?? 0) - (chitem.AvgPurchasePrice ?? 0));

                                int sellingprice = Convert.ToInt16(possitem.SellPrice ?? 0);
                                demandlist.Add(new DemandList { ProductName = possitem.CommodityName, SellSystem = starsystem.SystemName, SellStation = station.StationName, Distance = distance, SellPrice = sellingprice, Profit = profit });
                            }
                            // Sort list by amount of profit , distance
                            demandlist = demandlist.OrderByDescending(s => s.Profit).ThenBy(s => s.Distance).ToList();

                            // Return the best profit item
                            var cargostatus = "Tradable";
                            if (chitem.Stolen == true)
                            {
                                cargostatus = "Stolen";
                            }
                            else
                            {
                                if (chitem.MissionCargo == true)
                                {
                                    cargostatus = "Mission";
                                }
                            }
                            if (demandlist.Count() > 0)
                            {
                                var topdemand = demandlist.First();
                                // var chlistitem = new ListViewItem(new[] { chitem.CommodityName, chitem.Qty.ToString(), chitem.AvgPurchasePrice.ToString(), chitem.Stolen.ToString(), topdemand.SellSystem, topdemand.Distance.ToString(), topdemand.SellStation, topdemand.Profit.ToString() });
                                var chlistitem = new ListViewItem(new[] { chitem.CommodityName, chitem.Qty.ToString(), chitem.AvgPurchasePrice.ToString(), cargostatus, topdemand.SellSystem, topdemand.Distance.ToString(), topdemand.SellStation, topdemand.Profit.ToString() });
                                lvCargo.Items.Add(chlistitem);
                            }
                            else
                            {
                                // var chlistitem = new ListViewItem(new[] { chitem.CommodityName, chitem.Qty.ToString(), chitem.AvgPurchasePrice.ToString(), chitem.Stolen.ToString(), "", "0.0", "", "0" });
                                var chlistitem = new ListViewItem(new[] { chitem.CommodityName, chitem.Qty.ToString(), chitem.AvgPurchasePrice.ToString(), cargostatus, "", "0.0", "", "0" });
                                lvCargo.Items.Add(chlistitem);
                            }
                        }
                    }
                    lvCargo.Refresh();
                }
            }
        }

        private void DisplayMission()
        {
            // Current date & time as seconds
            DateTimeOffset dto = new DateTimeOffset(DateTime.Now);
            var currseconds = dto.ToUnixTimeSeconds();

            using (var db = new EDTSQLEntities())
            {
                if (db.StarSystems.Count() == 0)
                {
                    return;
                }
                if (CurrSystem != null)
                {
                    if (CurrSystem.Contains("=") == true)
                    {
                        int index = CurrSystem.IndexOf("=");
                        CurrSystem = (index > 1 ? CurrSystem.Substring(0, index - 1) : null);
                    }
                }
                if (CurrSystem != null)
                {
                    StarSystem currsystem = new StarSystem();
                    if (CurrSystem == "" | db.StarSystems.Count(n => n.SystemName == CurrSystem) == 0)
                    {
                        currsystem = new StarSystem() { SpaceX = 0, SpaceY = 0, SpaceZ = 0 };
                    }
                    else
                    {
                        currsystem = db.StarSystems.Single(n => n.SystemName == CurrSystem); // Get Current System
                    }

                    lvMissions.Items.Clear();

                    if (db.ActiveMissions.Count() > 0)
                    {
                        foreach (ActiveMission amitem in db.ActiveMissions)
                        {
                            var remainingtime = amitem.Expiry.Value - currseconds;
                            var timeSpan = TimeSpan.FromSeconds(remainingtime);
                            int dy = timeSpan.Days;
                            int hr = timeSpan.Hours;
                            int mn = timeSpan.Minutes;
                            int sec = timeSpan.Seconds;
                            string expiresIn = "D:" + dy + " H:" + hr + " M:" + mn + " S:" + sec;

                            if (db.StarSystems.Any(o => o.SystemName == amitem.DestinationSystem))
                            {
                                StarSystem starsystem = db.StarSystems.Single(n => n.SystemName == amitem.DestinationSystem);
                                // Calculate the distance between current location and possible selling system
                                double distance = Program.SystemDistance(currsystem.SpaceX, currsystem.SpaceY, currsystem.SpaceZ, starsystem.SpaceX, starsystem.SpaceY, starsystem.SpaceZ);

                                var amlistitem = new ListViewItem(new[] { amitem.MissionID.ToString(), amitem.MissionType, amitem.MissionCargo, amitem.DestinationSystem, distance.ToString(), amitem.DestinationStation, expiresIn });
                                lvMissions.Items.Add(amlistitem);
                            }
                            else
                            {
                                var amlistitem = new ListViewItem(new[] { amitem.MissionID.ToString(), amitem.MissionType, amitem.MissionCargo, amitem.DestinationSystem, "", amitem.DestinationStation, expiresIn });
                                lvMissions.Items.Add(amlistitem);
                            }
                        }
                    }
                    lvMissions.Sort();
                    lvMissions.Refresh();
                }
            }
        }

        private void DisplayMaterial()
        {
            using (var db = new EDTSQLEntities())
            {
                if (db.Materials.Count() > 0)
                {
                    lvRaw.Items.Clear();
                    lvMaterials.Items.Clear();
                    lvEncoded.Items.Clear();

                    foreach (Material matitem in db.Materials)
                    {
                        var matlistitem = new ListViewItem(new[] { matitem.MaterialName, matitem.Quantity.ToString() });

                        if (matitem.MaterialGroup == "Raw")
                        {
                            lvRaw.Items.Add(matlistitem);
                        }
                        if (matitem.MaterialGroup == "Manufactured")
                        {
                            lvMaterials.Items.Add(matlistitem);
                        }
                        if (matitem.MaterialGroup == "Encoded")
                        {
                            lvEncoded.Items.Add(matlistitem);
                        }
                    }
                    lvRaw.Refresh();
                    lvMaterials.Refresh();
                    lvEncoded.Refresh();
                }
            }
        }
    }
}