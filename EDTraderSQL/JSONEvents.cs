using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDTraderSQL
{
    public class LocationEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string StarSystem { get; set; }
        public List<float> StarPos { get; set; }
        public string Body { get; set; }
        public string BodyType { get; set; }
        public bool Docked { get; set; }
        public string StationName { get; set; }
        public string StationType { get; set; }
        public string SystemFaction { get; set; }
        public string SystemAllegiance { get; set; }
        public string SystemEconomy { get; set; }
        public string SystemEconomy_Localised { get; set; }
        public string SystemGovernment { get; set; }
        public string SystemGovernment_Localised { get; set; }
        public string SystemSecurity { get; set; }
        public string SystemSecurity_Localised { get; set; }
        public List<ActiveFactions> Factions { get; set; }
    }

    public class DockedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string StationName { get; set; }
        public string StationType { get; set; }
        public string StarSystem { get; set; }
        public string StationFaction { get; set; }
        public string StationGovernment { get; set; }
        public string StationGovernment_Localised { get; set; }
        public string StationAllegiance { get; set; }
        public string StationEconomy { get; set; }
        public string StationEconomy_Localised { get; set; }
        public double DistFromStarLS { get; set; }
    }

    public class FSDJumpEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string StarSystem { get; set; }
        public List<float> StarPos { get; set; }
        public string SystemAllegiance { get; set; }
        public string SystemEconomy { get; set; }
        public string SystemEconomy_Localised { get; set; }
        public string SystemGovernment { get; set; }
        public string SystemGovernment_Localised { get; set; }
        public string SystemSecurity { get; set; }
        public string SystemSecurity_Localised { get; set; }
        public double JumpDist { get; set; }
        public double FuelUsed { get; set; }
        public double FuelLevel { get; set; }
        public List<ActiveFactions> Factions { get; set; }
        public string SystemFaction { get; set; }
        public string Power { get; set; }
        public string PowerplayState { get; set; }
    }

    public class MaterialsEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public List<Raw> Raw { get; set; }
        public List<Manufactured> Manufactured { get; set; }
        public List<Encoded> Encoded { get; set; }
    }

    public class CargoEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public List<Inventory> Inventory { get; set; }
    }

    public class MarketBuyEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Type { get; set; }
        public int Count { get; set; }
        public int BuyPrice { get; set; }
        public int TotalCost { get; set; }
    }

    public class CollectCargoEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Type { get; set; }
        public bool Stolen { get; set; }
    }

    public class MarketSellEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Type { get; set; }
        public int Count { get; set; }
        public int SellPrice { get; set; }
        public int TotalSell { get; set; }
        public int AvgPricePaid { get; set; }
        public bool IllegalGoods { get; set; }
        public bool StolenGoods { get; set; }
        public bool BlackMarket { get; set; }
    }

    public class EjectCargoEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Type { get; set; }
        public int Count { get; set; }
        public bool Abandoned { get; set; }
    }

    public class MiningRefinedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Type { get; set; }
    }

    public class FileHeaderEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public int part { get; set; }
        public string language { get; set; }
        public string gameversion { get; set; }
        public string build { get; set; }
    }

    public class MissionAcceptedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Faction { get; set; }
        public string Name { get; set; }
        public string Commodity { get; set; }
        public string Commodity_Localised { get; set; }
        public int Count { get; set; }
        public string DestinationSystem { get; set; }
        public string DestinationStation { get; set; }
        public string Expiry { get; set; }
        public string Influence { get; set; }
        public string Reputation { get; set; }
        public int MissionID { get; set; }
        public DateTime getExpiryAsDate()
        {
            int yyyy = 1970;
            int mmm = 1;
            int dd = 1;
            int hh = 0;
            int mm = 0;
            int ss = 0;

            Int32.TryParse(Expiry.Substring(0, 4), out yyyy);
            Int32.TryParse(Expiry.Substring(5, 2), out mmm);
            Int32.TryParse(Expiry.Substring(8, 2), out dd);
            Int32.TryParse(Expiry.Substring(11, 2), out hh);
            Int32.TryParse(Expiry.Substring(14, 2), out mm);
            Int32.TryParse(Expiry.Substring(17, 2), out ss);

            DateTime dt = new DateTime(yyyy, mmm, dd, hh, mm, ss, DateTimeKind.Utc);
            return dt;
        }
    }

    public class MissionCompletedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Faction { get; set; }
        public string Name { get; set; }
        public int MissionID { get; set; }
        public string Commodity { get; set; }
        public string Commodity_Localised { get; set; }
        public int Count { get; set; }
        public string DestinationSystem { get; set; }
        public string DestinationStation { get; set; }
        public int Reward { get; set; }
        public List<CommodityReward> CommodityReward { get; set; }
    }

    public class MissionFailedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Name { get; set; }
        public int MissionID { get; set; }
    }

    public class MaterialCollectedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class MaterialDiscardedEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class StartJumpEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string JumpType { get; set; }
        public string StarSystem { get; set; }
        public string StarClass { get; set; }
    }

    public class LoadOutEvent
    {
        public string timestamp { get; set; }
        public string @event { get; set; }
        public string Ship { get; set; }
        public int ShipID { get; set; }
        public string ShipName { get; set; }
        public string ShipIdent { get; set; }
        public List<Module> Modules { get; set; }
    }

    public class Module //Part of LoadOutEvent
    {
        public string Slot { get; set; }
        public string Item { get; set; }
        public bool On { get; set; }
        public int Priority { get; set; }
        public int AmmoInClip { get; set; }
        public int AmmoInHopper { get; set; }
        public double Health { get; set; }
        public int Value { get; set; }
        public string EngineerBlueprint { get; set; }
        public int EngineerLevel { get; set; }
    }

    public class CommodityReward //Part of MissionCompletedEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class ActiveFactions //Part of LocationEvent
    {
        public string Name { get; set; }
        public string FactionState { get; set; }
        public string Government { get; set; }
        public double Influence { get; set; }
        public string Allegiance { get; set; }
    }

    public class Raw //Part of MaterialsEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class Manufactured //Part of MaterialsEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class Encoded //Part of MaterialsEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class Inventory //Part of CargoEvent
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
