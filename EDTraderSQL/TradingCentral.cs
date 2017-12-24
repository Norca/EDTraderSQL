using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EDTraderSQL
{
    public partial class TradingCentral : Form
    {
        private FileSystemWatcher JournalFileWatcher;
        private FileSystemWatcher MarketFileWatcher;
        private bool FileLocSet;
        private string JournalPath;
        private string ActiveJournal;
        private int JournalLineCount;
        private bool JournalDirty;
        private string MarketPath;
        private string MarketDump;
        private bool MarketDirty;
        private string CurrSystem;

        public TradingCentral()
        {
            InitializeComponent();

            FileLocSet = false;
            CheckAndSetPaths();
            DisplayMission();
            JournalLineCount = 1;
            JournalDirty = false;
            MarketDirty = false;
            lblStarSystem.Text = Properties.Settings.Default.LastSystem;
            CurrSystem = Properties.Settings.Default.LastSystem;
            lblStation.Text = Properties.Settings.Default.LastStation;
            lblDBSystems.Text = "0";
            lblDBStations.Text = "0";
           

        } // TradingCentral() End

        private void CheckAndSetPaths()
        {
            string JournalSet = Properties.Settings.Default.EDJournalLocation;
            string MarketSet = Properties.Settings.Default.EDMarketLocation;
            bool jourfolderExists = Directory.Exists(JournalSet);
            bool markfolderExists = Directory.Exists(MarketSet);

            if (jourfolderExists == false)
            {
                JournalPath = "";
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                folderBrowserDialog1.Description = "Select the location of the Elite Journal logs";
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    JournalPath = folderBrowserDialog1.SelectedPath;
                    Properties.Settings.Default.EDJournalLocation = folderBrowserDialog1.SelectedPath;
                    Properties.Settings.Default.Save();
                    FileLocSet = true;
                }
            }
            else
            {
                JournalPath = JournalSet;
                FileLocSet = true;
            }

            if (markfolderExists == false)
            {
                MarketPath = "";
                FolderBrowserDialog folderBrowserDialog2 = new FolderBrowserDialog();
                folderBrowserDialog2.Description = "Select the location of the EDMarket files";
                if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
                {
                    MarketPath = folderBrowserDialog2.SelectedPath;
                    Properties.Settings.Default.EDMarketLocation = folderBrowserDialog2.SelectedPath;
                    Properties.Settings.Default.Save();
                    FileLocSet = true;
                }
            }
            else
            {
                MarketPath = MarketSet;
                FileLocSet = true;
            }
        } // CheckAndSetPaths() End

        private void BtnWatcher_Click(object sender, EventArgs e)
        {
            if (FileLocSet == false) // Path to Journal Logs not set
            {
                //JournalFileWatcher.EnableRaisingEvents = false;
                //JournalFileWatcher.Dispose();
                //MarketFileWatcher.EnableRaisingEvents = false;
                //MarketFileWatcher.Dispose();
                BtnWatcher.BackColor = Color.Red;
                BtnWatcher.Text = "Events not monitored";
            }
            else // Path to Journal Logs is set
            {
                BtnWatcher.BackColor = Color.Green;
                BtnWatcher.Text = "Monitoring Events";

                JournalFileWatcher = new FileSystemWatcher()
                {
                    Filter = "*.log",
                    Path = JournalPath + "\\",
                    IncludeSubdirectories = false,

                    NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                JournalFileWatcher.Created += new FileSystemEventHandler(JournalCreated);
                JournalFileWatcher.EnableRaisingEvents = true;

                MarketFileWatcher = new FileSystemWatcher()
                {
                    Filter = "*.csv",
                    Path = MarketPath + "\\",
                    IncludeSubdirectories = false,

                    NotifyFilter = NotifyFilters.FileName
                };
                MarketFileWatcher.Created += new FileSystemEventHandler(MarketDumpCreated);
                MarketFileWatcher.EnableRaisingEvents = true;
            }
        } // BtnWatcher_Click End

        private void JournalCreated(object sender, FileSystemEventArgs e)
        {
            ActiveJournal = e.FullPath;
            JournalLineCount = 1;
            JournalDirty = true;
            btnJDirty.BackColor = Color.Green;
        } // OnCreated End

        private void MarketDumpCreated(object sender, FileSystemEventArgs e)
        {
            JournalDirty = true;
            btnJDirty.BackColor = Color.Blue;
            MarketDump = e.FullPath;
            MarketDirty = true;
            btnMDirty.BackColor = Color.Green;
        } // MarketDumpCreated End

        private void BtnExit_Click(object sender, EventArgs e)
        {
            // Save Window Location
            Application.Exit();
        } // BtnExit_Click End

        private void ReadJournalLines()
        {
            Stream stream = File.Open(ActiveJournal, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader streamReader = new StreamReader(stream);

            List<string> readJournal = new List<string>();

            while (true)
            {
                string line = streamReader.ReadLine();
                if (line == null)
                {
                    break;
                }
                readJournal.Add(line);
            }
            //streamReader.Dispose();
            stream.Dispose();
            int i = 1;

            //Test for non-live game journal
            string firstline = readJournal.First();
            FileHeaderEvent filehead = JsonConvert.DeserializeObject<FileHeaderEvent>(firstline);
            if (filehead.gameversion.Contains("Beta"))
            {
                UpdateMonitor("Journal - FileHeader Check FAILED");
                return;
            }

            foreach(string line in readJournal)
            {
                JObject Event = JObject.Parse(line);
                if (i <= JournalLineCount)
                {
                    i++;
                }
                else
                {
                    JournalEventHandler(Event, line);
                    i++;
                }
            }
            JournalLineCount = readJournal.Count();
            JournalDirty = false;
            btnJDirty.BackColor = Color.Red;
            btnJDirty.Refresh();
            DisplayMission();
        } // ReadJournalLines() End

        private void ReadMarketLines()
        {
            Dictionary<string, string> CommodityInGroup = new Dictionary<string, string>();
            Dictionary<string, string> RareInGroup = new Dictionary<string, string>();
            using (var db = new EDTSQLEntities())
            {
                foreach (var commod in db.Commodities)
                {
                    CommodityInGroup.Add(commod.CommodityName.ToUpper(), commod.CommodGroupName);
                }
                foreach (var rare in db.RareCommodities)
                {
                    RareInGroup.Add(rare.CommodityName.ToUpper(), rare.CommodGroupName);
                }
                db.Dispose();
            }
            Dictionary<string, int> StockLevels = new Dictionary<string, int>();
            StockLevels.Add("High", 4);
            StockLevels.Add("Med", 2);
            StockLevels.Add("Low", 1);
            StockLevels.Add("", 0);

            using (var sr = new StreamReader(MarketDump))
            {
                var reader = new CsvReader(sr);
                reader.Configuration.HasHeaderRecord = true;
                reader.Configuration.Delimiter = ";";
                List<MarketInfo> records = new List<MarketInfo>();
                bool setdate = false;
                DateTime rectime = DateTime.UtcNow;
                // string strrectime = rectime.ToString();
                int xxx = 0;
                while (reader.Read())
                {
                    if (setdate == false)
                    {
                        string strdate = reader.GetField<string>(9).Substring(0, 10);
                        string strtime = reader.GetField<string>(9).Substring(11, 8);
                        string strdatetime = strdate + " " + strtime;
                        rectime = DateTime.ParseExact(strdatetime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        // strrectime = rectime.ToString();
                        setdate = true;
                    }

                    MarketInfo addrec = new MarketInfo();
                    addrec.StarSystem = reader.GetField<string>(0);
                    addrec.Station = reader.GetField<string>(1);
                    addrec.Commodity = reader.GetField<string>(2);
                    if (int.TryParse(reader.GetField<string>(3), out xxx))
                    {
                        addrec.SellPrice = int.Parse(reader.GetField<string>(3));
                    }
                    else
                    {
                        addrec.SellPrice = xxx;
                    }
                    if (int.TryParse(reader.GetField<string>(4), out xxx))
                    {
                        addrec.BuyPrice = int.Parse(reader.GetField<string>(4));

                    }
                    else
                    {
                        addrec.BuyPrice = xxx;
                    }
                    addrec.DemandLevel = reader.GetField<string>(6);
                    addrec.SupplyLevel = reader.GetField<string>(8);
                    // addrec.LogDate = strrectime;
                    addrec.LogDate = rectime;
                    records.Add(addrec);
                    
                }

                //Read System & Station so previous market entries can be removed
                if (records.Count > 0)
                {
                    MarketInfo getLocation = records.First();
                    using (var db = new EDTSQLEntities())
                    {
                        StarSystem starsystem = db.StarSystems.SingleOrDefault(p => p.SystemName == getLocation.StarSystem);
                        Station station = db.Stations.SingleOrDefault(o => o.StationName == getLocation.Station);
                        UpdateMonitor("Market Data - Update found");

                        if (station == null)
                        {
                            UpdateMonitor("Market Data - Station not yet known");
                            db.Dispose();
                        }
                        else
                        {
                            db.MarketDetails.RemoveRange(db.MarketDetails.Where(x => x.SystemID == starsystem.SystemID && x.StationID == station.StationID));
                            db.SaveChanges();

                            foreach (MarketInfo marketcommodity in records)
                            {
                                if (CommodityInGroup.ContainsKey(marketcommodity.Commodity.ToUpper()))
                                {
                                    //Add new Market detail lines
                                    db.MarketDetails.Add(new MarketDetail()
                                    {
                                        SystemID = starsystem.SystemID,
                                        StationID = station.StationID,
                                        CommodGroupName = CommodityInGroup[marketcommodity.Commodity.ToUpper()],
                                        CommodityName = marketcommodity.Commodity,
                                        SellPrice = marketcommodity.SellPrice,
                                        BuyPrice = marketcommodity.BuyPrice,
                                        DemandStatus = StockLevels[marketcommodity.DemandLevel],
                                        SupplyStatus = StockLevels[marketcommodity.SupplyLevel],
                                        EntryDate = marketcommodity.LogDate
                                    });
                                }
                                else
                                {
                                    //Add new Market detail lines
                                    db.MarketDetails.Add(new MarketDetail()
                                    {
                                        SystemID = starsystem.SystemID,
                                        StationID = station.StationID,
                                        CommodGroupName = RareInGroup[marketcommodity.Commodity.ToUpper()],
                                        CommodityName = marketcommodity.Commodity,
                                        SellPrice = marketcommodity.SellPrice,
                                        BuyPrice = marketcommodity.BuyPrice,
                                        DemandStatus = StockLevels[marketcommodity.DemandLevel],
                                        SupplyStatus = StockLevels[marketcommodity.SupplyLevel],
                                        EntryDate = marketcommodity.LogDate
                                    });
                                }
                            }
                            UpdateMonitor("Market Data - Updated");
                            db.SaveChanges();
                            db.Dispose();
                            MarketDirty = false;
                            btnMDirty.BackColor = Color.Red;
                            btnMDirty.Refresh();
                            //File.Delete(MarketDump);
                        }
                    }
                    sr.Dispose();

                    ObtainTopProductsToBuy();
                    ObtainTopProductsDemanded();
                }
                DisplayCargo();
            }
        } // ReadMarketLines() End

        private void TmrUpdate_Tick(object sender, EventArgs e)
        {
            btnTimerStatus.Text = "Timer Active";
            btnTimerStatus.BackColor = Color.Green;
            btnTimerStatus.Refresh();
            if (JournalDirty)
            {
                ReadJournalLines();
            }
            if (MarketDirty)
            {
                ReadMarketLines();
            }
            btnTimerStatus.Text = "Timer Idle";
            btnTimerStatus.BackColor = Color.Red;
            btnTimerStatus.Refresh();
        } // TmrUpdate_Tick End

        private void TmrJournalRefresh_Tick(object sender, EventArgs e)
        {
            if (ActiveJournal == null)
            {
                JournalDirty = false;
            }
            else
            {
                JournalDirty = true;
                btnJDirty.BackColor = Color.Blue;
                btnJDirty.Refresh();
            }
        }

        private void TradingCentral_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Size.Width == 0 || Properties.Settings.Default.Size.Height == 0)
            {
                this.Width = 1800;
                this.Height = 1040;
            }
            else
            {
                this.WindowState = Properties.Settings.Default.State;
                if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;

                this.Location = Properties.Settings.Default.Location;
                this.Size = Properties.Settings.Default.Size;
            }
            BtnWatcher.PerformClick();
        } // TradingCentral_Load End

        private void JournalEventHandler(JObject jo, string jstr)
        {
            string EventType = (string)jo["event"];

            switch (EventType)
            {
                case "Cargo":
                    JournalCargo(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Cargo");
                    break;

                case "Materials":
                    JournalMaterials(jstr);
                    DisplayMaterial();
                    UpdateMonitor("Journal - Materials");
                    break;

                case "Docked":
                    JournalDocked(jstr);
                    UpdateMonitor("Journal - Docked");
                    break;

                case "FSDJump":
                    JournalFSDJump(jstr);
                    UpdateMonitor("Journal - FSDJump");
                    break;

                case "Location":
                    JournalLocation(jstr);
                    UpdateMonitor("Journal - Location");
                    break;

                case "Undocked":
                    JournalUndocked();
                    UpdateMonitor("Journal - Undocked");
                    break;

                case "Died":
                    JournalDied();
                    UpdateMonitor("Journal - Died");
                    break;

                case "MaterialCollected":
                    JournalMaterialCollected(jstr);
                    DisplayMaterial();
                    UpdateMonitor("Journal - Material Collected");
                    break;

                case "MaterialDiscarded":
                    JournalMaterialDiscarded(jstr);
                    DisplayMaterial();
                    UpdateMonitor("Journal - Material Discarded");
                    break;

                case "CollectCargo":
                    JournalCollectCargo(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Collect Cargo");
                    break;

                case "EjectCargo":
                    JournalEjectCargo(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Eject Cargo");
                    break;

                case "MarketBuy":
                    JournalMarketBuy(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Market Buy");
                    break;

                case "MarketSell":
                    JournalMarketSell(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Market Sell");
                    break;

                case "MiningRefined":
                    JournalMiningRefined(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Mining Refined");
                    break;

                case "MissionAccepted":
                    JournalMissionAccepted(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Mission Accepted looking for Cargo");
                    break;

                case "MissionCompleted":
                    JournalMissionCompleted(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Mission Completed");
                    break;

                case "MissionFailed":
                    JournalMissionFailed(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Mission Failed");
                    break;

                case "MissionRedirected":
                    JournalMissionRedirected(jstr);
                    DisplayCargo();
                    UpdateMonitor("Journal - Mission Redirected");
                    break;

                case "PayFines":
                    JournalPayFines(jstr);
                    UpdateMonitor("Journal - Pay Fines");
                    break;

                case "PayLegacyFines":
                    JournalPayLegacyFines(jstr);
                    UpdateMonitor("Journal - Pay Legacy Fines");
                    break;

                case "ScientificResearch":
                    JournalScientificResearch(jstr);
                    DisplayMaterial();
                    UpdateMonitor("Journal - Scientific Research");
                    break;

                case "CommitCrime":
                    JournalCommitCrime(jstr);
                    UpdateMonitor("Journal - Commit Crime");
                    break;

                case "Synthesis":
                    JournalSynthesis(jstr);
                    DisplayMaterial();
                    UpdateMonitor("Journal - Synthesis");
                    break;
            }
        } // JournalEventHandler End

        private void UpdateMonitor(string Msg)
        {
            lblMonitor3.Text = lblMonitor2.Text;
            lblMonitor2.Text = lblMonitor1.Text;
            lblMonitor1.Text = lblMonitoredEvent.Text;
            lblMonitoredEvent.Text = Msg;
            lblMonitoredEvent.Refresh();
        }


        private void TradingCentral_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.LastSystem = CurrSystem;
            Properties.Settings.Default.LastStation = lblStation.Text;
            Properties.Settings.Default.State = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = this.Location;
                Properties.Settings.Default.Size = this.Size;
            }
            else
            {
                Properties.Settings.Default.Location = this.RestoreBounds.Location;
                Properties.Settings.Default.Size = this.RestoreBounds.Size;
            }
            Properties.Settings.Default.Save();
        }

        private void LvSupply_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LvSupply_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawRectangle(Pens.OrangeRed, e.Bounds);
                e.Graphics.DrawString(e.SubItem.Text, lvSupply.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void LtvNeeded_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LtvNeeded_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawRectangle(Pens.OrangeRed, e.Bounds);
                e.Graphics.DrawString(e.SubItem.Text, lvMaterials.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void ListMaterials_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void ListMaterials_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(e.SubItem.Text, lvMaterials.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void LtvCargo_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LtvCargo_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawRectangle(Pens.OrangeRed, e.Bounds);
                e.Graphics.DrawString(e.SubItem.Text, lvCargo.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void LvRaw_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LvRaw_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(e.SubItem.Text, lvRaw.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void LvEncoded_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LvEncoded_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawString(e.SubItem.Text, lvEncoded.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            string JournalSet = Properties.Settings.Default.EDJournalLocation;
            string MarketSet = Properties.Settings.Default.EDMarketLocation;

            JournalPath = "";
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select the location of the Elite Journal logs";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                JournalPath = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.EDJournalLocation = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                FileLocSet = true;
            }

            MarketPath = "";
            FolderBrowserDialog folderBrowserDialog2 = new FolderBrowserDialog();
            folderBrowserDialog2.Description = "Select the location of the EDMarket files";
            if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
            {
                MarketPath = folderBrowserDialog2.SelectedPath;
                Properties.Settings.Default.EDMarketLocation = folderBrowserDialog2.SelectedPath;
                Properties.Settings.Default.Save();
                FileLocSet = true;
            }
        }

        private void LvMissions_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                using (Font headerfont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold))
                {
                    e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
                    e.Graphics.DrawString(e.Header.Text, headerfont, Brushes.Black, e.Bounds, sf);
                }
            }
        }

        private void LvMissions_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                e.Graphics.DrawRectangle(Pens.OrangeRed, e.Bounds);
                e.Graphics.DrawString(e.SubItem.Text, lvMissions.Font, Brushes.DarkOrange, e.Bounds, sf);
            }
        }
    }
}
