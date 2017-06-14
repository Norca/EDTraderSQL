using System;
using System.Collections.Generic;

namespace EDTraderSQL
{
    public class MarketInfo
    {
        public string StarSystem { get; set; }
        public string Station { get; set; }
        public string Commodity { get; set; }
        public int SellPrice { get; set; }
        public int BuyPrice { get; set; }
        public int DemandVolume { get; set; }
        public string DemandLevel { get; set; }
        public int SupplyVolume { get; set; }
        public string SupplyLevel { get; set; }
        public DateTime LogDate { get; set; }
    }

    public class DemandList
    {
        public string ProductName { get; set; }
        public string SellSystem { get; set; }
        public double Distance { get; set; }
        public string SellStation { get; set; }
        public int SellPrice { get; set; }
        public int Profit { get; set; }
    }

    public class SupplyList
    {
        public string ProductName { get; set; }
        public string FromSystem { get; set; }
        public double Distance { get; set; }
        public string FromStation { get; set; }
        public int BuyPrice { get; set; }
        public int Profit { get; set; }
    }

    public class TopDemands
    {
        public string CommodityName { get; set; }
        public string SupplyStatus { get; set; }
        public string BuyPrice { get; set; }
        public string SellSystem { get; set; }
        public double SellSysDist { get; set; }
        public string SellStation { get; set; }
        public string SellPrice { get; set; }
        public int Profit { get; set; }
    }

    public class TopNeeds
    {
        public string CommodityName { get; set; }
        public string DemandStatus { get; set; }
        public string SellPrice { get; set; }
        public string FromSystem { get; set; }
        public double BuySysDist { get; set; }
        public string FromStation { get; set; }
        public string BuyPrice { get; set; }
        public int Profit { get; set; }
    }

    public class CommodityData
    {
        public string DataType { get; set; }
        public string Name { get; set; }
        public List<CommodityItem> Commodities { get; set; }
    }

    public class MaterialData
    {
        public string DataType { get; set; }
        public string Name { get; set; }
        public List<CommodityItem> Items { get; set; }
    }

    public class RaresData
    {
        public string DataType { get; set; }
        public string Name { get; set; }
        public List<RareItem> Rares { get; set; }
    }

    public class CommodityItem
    {
        public string Name { get; set; }
        public string EDCode { get; set; }
    }

    public class RareItem
    {
        public string Name { get; set; }
        public string EDCode { get; set; }
        public string StarSystem { get; set; }
        public string Station { get; set; }
    }

    public class MaterialLookup
    {
        public string EDCodeName { get; set; }
        public string MaterialName { get; set; }
        public string MaterialGroup { get; set; }
    }
}
