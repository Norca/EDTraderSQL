//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EDTraderSQL
{
    using System;
    using System.Collections.Generic;
    
    public partial class MarketDetail
    {
        public int MarketEntryID { get; set; }
        public Nullable<int> SystemID { get; set; }
        public Nullable<int> StationID { get; set; }
        public string CommodityName { get; set; }
        public Nullable<int> SellPrice { get; set; }
        public Nullable<int> BuyPrice { get; set; }
        public Nullable<int> DemandStatus { get; set; }
        public Nullable<int> SupplyStatus { get; set; }
        public System.DateTime EntryDate { get; set; }
        public string CommodGroupName { get; set; }
    
        public virtual StarSystem StarSystem { get; set; }
        public virtual Station Station { get; set; }
    }
}
