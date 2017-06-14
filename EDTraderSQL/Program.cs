using Newtonsoft.Json;
using System;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace EDTraderSQL
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            PopulateDefaultData(); //Checks that all static data tables are populated

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TradingCentral());
        } // End of main code

        // Start of functions
        internal static double SystemDistance(double FromX, double FromY, double FromZ, double ToX, double ToY, double ToZ)
        {
            double CalcX = ToX - FromX;
            double CalcY = ToY - FromY;
            double CalcZ = ToZ - FromZ;

            double LYResult = Math.Sqrt((CalcX * CalcX) + (CalcY * CalcY) + (CalcZ * CalcZ));

            return Math.Round(LYResult, 2);
        }

        // Default Data functions
        private static void PopulateDefaultData()
        {
            // Clear default tables and adds starting data from textfiles
            using (var db = new EDTSQLEntities())
            {
                //Delete Commodities
                db.Commodities.RemoveRange(db.Commodities);
                db.SaveChanges();
                //Delete Rares
                db.RareCommodities.RemoveRange(db.RareCommodities);
                db.SaveChanges();
                //Delete CommodityGroups
                db.CommodityGroups.RemoveRange(db.CommodityGroups);
                db.SaveChanges();
                //Delete Materials
                db.MaterialLists.RemoveRange(db.MaterialLists);
                db.SaveChanges();

                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "";
                //Read and process the standard Commodities file
                resourceName = "EDTraderSQL.DefaultCommodities.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        CommodityData cd = JsonConvert.DeserializeObject<CommodityData>(line);
                        db.CommodityGroups.Add(new CommodityGroup() { CommodGroupName = cd.Name });
                        db.SaveChanges();

                        foreach (var item in cd.Commodities)
                        {

                            db.Commodities.Add(new Commodity() { CommodGroupName = cd.Name, CommodityName = item.Name, EDCodeName = item.EDCode });
                        }
                    }
                    db.SaveChanges();
                    reader.Dispose();
                    stream.Dispose();
                }

                //Read and process the Rare Commodities file
                resourceName = "EDTraderSQL.DefaultRares.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = null;

                    //Added to cover the Rares that we do not know the group of
                    db.CommodityGroups.Add(new CommodityGroup() { CommodGroupName = "NotKnown" });
                    db.SaveChanges();

                    while ((line = reader.ReadLine()) != null)
                    {
                        RaresData rd = JsonConvert.DeserializeObject<RaresData>(line);

                        foreach (var item in rd.Rares)
                        {
                            db.RareCommodities.Add(new RareCommodity() { CommodGroupName = rd.Name, CommodityName = item.Name, EDCodeName = item.EDCode });
                        }
                    }
                    db.SaveChanges();
                    reader.Dispose();
                    stream.Dispose();
                }

                //Read and process the Materials file
                resourceName = "EDTraderSQL.DefaultMaterials.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        MaterialData md = JsonConvert.DeserializeObject<MaterialData>(line);

                        foreach (var item in md.Items)
                        {
                            db.MaterialLists.Add(new MaterialList() { MaterialGroup = md.Name, MaterialName = item.Name, EDCodeName = item.EDCode });
                        }
                    }
                    db.SaveChanges();
                    reader.Dispose();
                    stream.Dispose();
                }
                db.Dispose();
            }
        }
    }
}