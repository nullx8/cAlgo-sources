using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.IO;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class WriteToFileExample : Robot
    {

//================================================================================
//                                                                            Vars
//================================================================================
        // stream for file
        StreamWriter _fileWriter;
        // desktop folder
        static string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        // path to file
        string filePath = Path.Combine(desktopFolder, "z.txt");

        private string responseFromServer = "";
        private string openPositionsString = "";

        [Parameter("Position label", DefaultValue = "Master Position Label")]
        public string MyLabel { get; set; }

        [Parameter("Position comment", DefaultValue = "Master Position Comment")]
        public string MyComment { get; set; }

        [Parameter("Volume", DefaultValue = 10000)]
        public int Volume { get; set; }

        [Parameter("Volume Max", DefaultValue = 100000)]
        public int VolumeMax { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 30)]
        public int StopLoss { get; set; }

        [Parameter("Take Profit (pips)", DefaultValue = 15)]
        public int TakeProfit { get; set; }

        [Parameter(DefaultValue = 5)]
        public int MaxPositions { get; set; }

//================================================================================
//                                                                         OnStart
//================================================================================
        protected override void OnStart()
        {
            Print("Hello Master ... ");
            Print(File.Exists(filePath) ? "============== File exists." : "================= File does not exist.");

            if (File.Exists(filePath))
            {
                //   File.Delete(filePath);
            }
            _fileWriter = File.AppendText(filePath);
            //creating file
            _fileWriter.AutoFlush = true;
            //file will be saved on each change
            //_fileWriter.WriteLine("All opened positions <---> Server Time: " + Server.Time);
        }

//================================================================================
//                                                                           OnBar
//================================================================================

        protected override void OnTick()
        {


            // get all opened positions with label and put to openPositionsString
            var AllPositions = Positions.FindAll(MyLabel);
            openPositionsString = ";";
            foreach (var position in AllPositions)
            {
                // BUY positions
                if (position.TradeType == TradeType.Buy)
                {
                    // OPENED POSITION STRING
                    openPositionsString += position.EntryTime + "_" + position.Id + "_" + position.SymbolCode + "_TRUE" + "_" + position.Volume + "_" + position.EntryPrice + "_" + position.StopLoss + "_" + position.TakeProfit + "_" + position.Label + "_" + position.Comment + "_##";
                }
                // SELL positions
                if (position.TradeType == TradeType.Sell)
                {
                    // OPENED POSITION STRING
                    openPositionsString += position.EntryTime + "_" + position.Id + "_" + position.SymbolCode + "_FALSE" + "_" + position.Volume + "_" + position.EntryPrice + "_" + position.StopLoss + "_" + position.TakeProfit + "_" + position.Label + "_" + position.Comment + "_##";
                }

            }


            //=============================================================================== save to file

            Print(openPositionsString);
            _fileWriter.WriteLine(openPositionsString);


        }

        /*
 //===========================================================================  read from file
            var dFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var fPath = Path.Combine(dFolder, "z.txt");

            string[] lines = System.IO.File.ReadAllLines(fPath);
            foreach (string line in lines)
            {
                // Use a tab to indent each line of the file.
                Print("Text from file: \t" + line);
            }

        
*/

//================================================================================
//                                                                          OnStop
//================================================================================
        protected override void OnStop()
        {
            Print("Bye Master ...");
            _fileWriter.Close();
        }
    }
}
