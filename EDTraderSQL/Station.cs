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
    
    public partial class Station
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Station()
        {
            this.MarketDetails = new HashSet<MarketDetail>();
        }
    
        public int StationID { get; set; }
        public Nullable<int> SystemID { get; set; }
        public string StationName { get; set; }
        public string StationType { get; set; }
        public string Economy { get; set; }
        public Nullable<bool> BlackMarketAvailable { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<MarketDetail> MarketDetails { get; set; }
        public virtual StarSystem StarSystem { get; set; }
    }
}
