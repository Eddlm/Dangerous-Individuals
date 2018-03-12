using GTA;
using GTA.Native;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using NativeUI;
using GTA.Math;
using System.Drawing;
using System.Linq;
using System.Xml;

namespace LSPDispatch
{
    /// <summary> 
    /// TO IMPROVE
    /// - Add FBI RDE models
    /// - Add Dispatch voices
    /// 
    /// 
    /// LAST FIXED
    /// 0.6
    /// Fixed/Improved:
    /// - Reworked fleeing AI (both driving and on foot)
    /// - Reworked ramming. It now works far more reliably and actually helps the Criminals getting rid of you or AI cops.
    /// - Reworked FSM for both cops and criminals, they will be a little smarter now. Cops shouldn't freeze when their suspect is surrendered anymore.
    /// - Reworked cop carefulness, cop vehicles won't be flung backwards ever again. They also use actual brakes instead of magic
    /// 
    /// Added:
    /// - Added Highway units.
    /// - Added support for RDE 3.0, most of the main RDE vehicles (LSPD/Sheriff/BCSO/State/NoOSE) will be used by Dangerous Individuals. You can use the Backup.xml provided by RDE 3.0 too.
    /// - Added LSPD:FR Backup.xml support. Only models supported, no components or liveries for now.
    /// - Added the player arrest thingie, players can now arrest criminals and bring them to nearby police stations
    /// - Hostile ambient peds will be treated as criminals now. From now on, any ped that's hostile to you will be a criminal that you can arrest
    /// - Added a Random Tuning option for Racer criminals, so every callout will feature different looking cars
    /// - Added the ability to go On/Off duty on police stations. To ease quick setup, you can go on duty on a cop car, but can't go off duty there.
    /// - Added the ability to play with any vehicle or model
    /// - Added a bunch of options to the Settings menu, regarding cop spawning, callout generation and more
    /// 
    /// Removed:
    /// - Racer car tuning utility (for Racer callouts). It has been replaced with an automatic tuning system.
    /// - Removed a Criminal feature where if they stood still they wouldn't trigger LostLOS events. (This was added in earlier versions to make the script less frustrating)
    /// - Removed Criminal's ability to Brake Check. I didn't like the effects, will probably add it again later on if I manage to make it actually usefull for the Criminals.
    /// Known bugs:
    /// - Ambient hostile security guards can be treated as criminals by the script.
    /// - Sometimes cops won't arrest surrendered criminals, specially if they are interrupted by another criminal/hostile peds. This is an issue related to Arrest/SecureArrest organization, to fix it, dismiss them and call another Unit.
    /// 0.5
    /// - Improved Suspect Driving AI, they can now decide to truly go offroad if the situation allows for it.
    /// - Improved Suspect Driving AI regarding their decisions when they're stuck.
    /// - Added new chase: another Bank Robbery, this time the suspects will be using multiple bikes.
    /// - Fixed (I think) suspects not surrendering when tased.
    /// - Fixed the "cop swarm" bug: There's now a limit on how many cop units can spawn when using the Automatic Backup system. You can still spawn as much as you like.
    /// 
    /// 0.4
    /// Release.
    /// 
    /// </summary>
    public class DangerousIndividuals : Script
    {
        bool FootReminder = false;
        Vector3 ReminderReference = Vector3.Zero;

        List<Blip> StationBlips = new List<Blip>();

        void HandleScriptReminders()
        {           
            if (ReminderReference == Vector3.Zero) ReminderReference = Game.Player.Character.Position;

            if (!FootReminder && !Game.Player.Character.IsInRangeOf(ReminderReference, 5f))
            {
                string name = "~b~[" + ScriptName + " " + ScriptVer + "]";
                Util.AddNotification("", "", "", name + "~w~ has loaded.");
                Util.AddNotification("", "", "", "~w~Go to any police stations to go on duty.");
                FootReminder = true;
            }
            
        }
        
        public static int NumberOfCopsNearSuspect(SuspectHandler suspect, float radius)
        {
            int Num = 0;
            Vector3 pos = suspect.Criminal.Position;
            bool AnyCopNear = false;
            if (Game.Player.Character.IsInRangeOf(pos, radius)) Num++;
            foreach (CopUnitHandler unit in CopsChasing)
            {
                if (unit.Leader.IsInRangeOf(pos, radius))
                {
                    AnyCopNear = true;
                }
            }
            if (AnyCopNear)
            {
                foreach (CopUnitHandler unit in CopsChasing)
                {
                    if (unit.Leader.IsInRangeOf(pos, radius * 2)) Num++;
                    foreach (Ped partner in unit.Partners) if (partner.IsInRangeOf(pos, radius * 2)) Num++;
                }
            }

            return Num;
        }
        public static SuspectHandler GetRandomActiveCriminal()
        {
            List<SuspectHandler> criminals = new List<SuspectHandler>();
            foreach (SuspectHandler criminal in CriminalsActive)
            {
                if (!criminal.Surrendered() && !criminal.ShouldRemoveCriminal) criminals.Add(criminal);
            }
            if (criminals.Count > 0) return criminals[Util.RandomInt(0, criminals.Count - 1)];
            else return null;
        }

        public static SuspectHandler GetRandomSurrenderedCriminal(bool AllowBeingArrested)
        {
            List<SuspectHandler> criminals = new List<SuspectHandler>();
            foreach (SuspectHandler criminal in CriminalsActive)
            {
                if (criminal.Surrendered() && !criminal.ShouldRemoveCriminal)
                {
                    //if ((!AllowBeingArrested && criminal.CopArrestingMe == null) || AllowBeingArrested) criminals.Add(criminal);
                    if (AllowBeingArrested)
                    {
                        criminals.Add(criminal);
                    }
                    else if (criminal.CopArrestingMe == null)
                    {
                        criminals.Add(criminal);
                    }
                }
            }
            if (criminals.Count > 0) return criminals[Util.RandomInt(0, criminals.Count - 1)];
            else return null;
        }
        public static bool IsSuspectValid(SuspectHandler suspect)
        {
            return (suspect != null && Util.CanWeUse(suspect.Criminal) && suspect.Criminal.IsAlive && suspect.Criminal.Handle != -1 && suspect.ShouldRemoveCriminal != true);
        }
        public static bool IsCopUnitValid(CopUnitHandler unit)
        {
            return (unit != null && Util.CanWeUse(unit.Leader) && unit.Leader.IsAlive);
        }
        public static CopUnitHandler GetClosestCop(SuspectHandler criminal, bool CanArrest, bool AllowCurrentlyArresting)
        {
            //UI.Notify("closest cop");
            CopUnitHandler finalcop = null;

            if (CopsChasing.Count > 0)
            {
                foreach (CopUnitHandler Unit in CopsChasing)
                {
                    if ((Unit.CanArrest() && Unit.CanArrest()) || (!Unit.CanArrest() && !CanArrest))
                    {
                        if (AllowCurrentlyArresting)
                        {
                            if (finalcop == null) finalcop = Unit;
                            else if (Unit.Leader.Position.DistanceTo(criminal.Criminal.Position) < finalcop.Leader.Position.DistanceTo(criminal.Criminal.Position)) finalcop = Unit;
                        }
                        else if (Unit.State != CopState.Arrest)
                        {
                            if (finalcop == null) finalcop = Unit;
                            else if (Unit.Leader.Position.DistanceTo(criminal.Criminal.Position) < finalcop.Leader.Position.DistanceTo(criminal.Criminal.Position)) finalcop = Unit;
                        }
                    }
                }
            }
            return finalcop;
        }

        public static bool Debug = true;
        static public string ScriptName = "Dangerous Individuals";
        static public string ScriptVer = "v0.6";

        //STATIC INFO
        public static List<string> PettyCrimes = new List<string> { "Animal Abuse", "Domestic Violence", "License Expired", "Illegal Inmigration", "Arson", "Reckless Driving", "Impaired Driving", "Speeding", "Forgery" };
        public static List<string> MediumCrimes = new List<string> { "Robbery", "Drug dealing", "", "", "", "", };
        public static List<string> HighTierCrimes = new List<string> { "", "", "", "", "", "", };
        public static int TickRefTime = Game.GameTime;
        public static int AutoDispatchRefTime = Game.GameTime;
        public static int TickRefTimeLong = Game.GameTime;
        public static string LayoutFile = "Scripts/DangerousIndividuals/RacerLayouts.xml";
        public static string ConfigFilename = "Scripts/DangerousIndividuals/Config.xml";
        public static string CriminalVehicleFile = "Scripts/DangerousIndividuals/CriminalVehicles.xml";
        public static string CopVehsandPedsFile = "Scripts/DangerousIndividuals/backup.xml";
        public static string CopVehsandPedsLSPDFRFile = "lspdfr/backup.xml";

        //KEYS
        public static Keys MainMenuKey = Keys.B;
        public static Keys ForceCalloutKey = Keys.X;


        //PURSUIT VARIABLES
        static public bool PlayerOnDuty = false;
        static public int PlayerOnDutyRef = 0;
        static public string PIT = "~y~PIT maneuvers~w~";
        static public string DeadlyForce = "~r~Deadly force~w~";
        static public int CriminalsRLGroup = World.AddRelationshipGroup("CriminalsRLGroup");
        static public int EnemyGangRLGroup = World.AddRelationshipGroup("EnemyGangRLGroup");
        static public int PublicEnemyRLGroup = World.AddRelationshipGroup("PublicEnemyRLGroup");

        static public int CopsRLGroup = World.AddRelationshipGroup("LSPDCops");
        static public int SurrenderedCriminalsRLGroup = World.AddRelationshipGroup("SurrenderedCriminals");

        static public int LostLOSThreshold = 30;

        public static List<CopUnitHandler> CopsChasing = new List<CopUnitHandler>();
        public static List<CopUnitHandler> CopsToRemove = new List<CopUnitHandler>();

        public static List<SuspectHandler> CriminalsActive = new List<SuspectHandler>();
        public static List<SuspectHandler> CriminalsFleeing = new List<SuspectHandler>();


        public static List<SuspectHandler> CriminalsToRemove = new List<SuspectHandler>();
        public static List<Ped> SplitCriminals = new List<Ped>();
        public static CriminalType SplitCriminalKind = CriminalType.ProffessionalsHeisters;

        public static List<dynamic> CriminalSelection = new List<dynamic> { 0 };
        public static List<dynamic> CopschasingThatCriminal = new List<dynamic> { 0 };

        public static List<SuspectHandler> CriminalsSurrendered = new List<SuspectHandler>();
        public static List<SuspectHandler> CriminalsAlone = new List<SuspectHandler>();


        //NATIVEUI


        private MenuPool _menuPool = new MenuPool();

        //When there are suspects
        private UIMenu mainMenu = new UIMenu("Dispatch", "Select the units to dispatch here.");

        private UIMenuItem PoliceSpawnMenuItem = new UIMenuItem("Police", "Police units.");
        private UIMenu PoliceSpawnMenu = new UIMenu("Police Dispatch", "");

        private UIMenuItem UISpawnLSPDCar = new UIMenuItem("Local Unit", "Average local unit.");
        private UIMenuItem UISpawnBike = new UIMenuItem("Local bike Unit", "A motorcycle unit.");

        private UIMenuItem UISpawnHeli = new UIMenuItem("Air Unit", "Unarmed Maverick. It will remain close to the suspect.");

        private UIMenuItem NooseSpawnMenuItem = new UIMenuItem("NOoSE", "NOoSE Units.");
        private UIMenu NooseSpawnMenu = new UIMenu("NOoSE Dispatch", "");

        private UIMenuItem UISpawnLocalSWAT = new UIMenuItem("Local SWAT", "Granger with 4 SWAT units.");
        private UIMenuItem UISpawnSWAT = new UIMenuItem("SWAT", "Riot with 8 SWAT units.");
        private UIMenuItem UISpawnTransport = new UIMenuItem("Prisoner Transport", "Prisoner van.");


        private UIMenuItem UISpawnNOoSEHeli = new UIMenuItem("NOoSE Air Unit", "Armed Annihilator with four shooting SWAT units that will shoot the suspect from it.");
        private UIMenuItem UISpawnArmoredSWAT = new UIMenuItem("Insurgent SWAT", "An armored Insurgent, SWAT variant. 6 SWAT units."); //RDE

        private UIMenuItem ArmySpawnMenuItem = new UIMenuItem("Army", "Army units.");
        private UIMenu ArmySpawnMenu = new UIMenu("Army Dispatch", "");

        private UIMenuItem UISpawnArmy = new UIMenuItem("Army Insurgent", "Insurgent full of soldiers.");
        private UIMenuItem UISpawnArmyHeli = new UIMenuItem("Army Attack heli", "Armed Attack Helicopter that will shoot at the suspect.");


        public static UIMenuCheckboxItem CloseRoads = new UIMenuCheckboxItem("Close roads", false, "If checked, all roads around you will be closed and won't contain traffic at all.");

        private UIMenuListItem UISelectCriminal = new UIMenuListItem("Suspect", CriminalSelection, 0);
        private UIMenu UICopsChasingSelectedCriminalMenu = new UIMenu("Cops Chasing", "Dismiss any Units chasing this suspect.");
        
        //private UIMenuListItem UIListCopsChasingCriminal = new UIMenuListItem("", CriminalSelection, 0);

        private UIMenuColoredItem UISuspectStatus = new UIMenuColoredItem("Fleeing Suspect", Color.Azure, Color.Wheat);
        private UIMenuColoredItem UISuspectHeading = new UIMenuColoredItem("Headed", Color.DimGray, Color.White);
        private UIMenuColoredItem UIsuspectPos = new UIMenuColoredItem("Last Seen", Color.DimGray, Color.White);



        //When there aren't suspects
        private UIMenu SettingsMenu = new UIMenu("Settings", "Finetune your experience here.");

        private UIMenu RealismMenu = new UIMenu("Realism", "Stuff regarding realism and fairness.");
        private UIMenuItem RealismMenuItem = new UIMenuItem("Realism", "Configure some settings regarding the realism of the script.");
        public static UIMenuCheckboxItem PlayerCareMenu = new UIMenuCheckboxItem("Player Care Module", false, "If checked, the script will take care of all these petty things you would normally need to do manually via Trainer: You'll be healed and your armor replenished while you're in your vehicle and not chasing anyone. Your vehicle will be fully repaired when accepting any callout. You won't get Stars while chasing a suspect/having a cop playermodel.");
        public static UIMenuCheckboxItem RealisticPosDispatch = new UIMenuCheckboxItem("Realistic cop dispatch", true, "Forces the cops to spawn from Police Stations when called. If disabled, call cop units will spawn near the suspect.");




        private UIMenu AddonSelectionMenu = new UIMenu("Vehicle Add-Ons", "Finetune your experience here.");
        private UIMenuItem AddonSelectionMenuItem = new UIMenuItem("Vehicle Add-Ons", "Vehicle Add-Ons.");

        private UIMenuCheckboxItem AllowMadMaxVehicles = new UIMenuCheckboxItem("Load Mad Max Vehicles", true, "Allows some criminals to spawn in Mad Max vehicles.");
        private UIMenuCheckboxItem AllowRDEVehicles = new UIMenuCheckboxItem("Load RDE Vehicles", true, "Allows the cops to spawn in the vehicles Realism Dispatch Enhanced adds to the game.");
        private UIMenuCheckboxItem AllowIVPack = new UIMenuCheckboxItem("Load IVPack Vehicles", true, "Allows criminals to spawn in IVPack vehicles.");
        private UIMenuCheckboxItem AllowLoadingFromFile = new UIMenuCheckboxItem("Load criminal Vehicles fom file", true, "The script will load vehicles from "+CriminalVehicleFile+", and let the criminals use them.");
        private UIMenuCheckboxItem AmbientPedsCanBeCriminals = new UIMenuCheckboxItem("Register hostile peds as criminals", true, "If true, any peds hostile to you (while on duty) will be considered criminals by the script and will use its AI. Allows for more dynamic Callouts and chases.");


        private UIMenu CalloutSelectionMenu = new UIMenu("Callout Selection", "Select the kind of callouts that can happen.");
        private UIMenuItem CalloutSelectionMenuItem = new UIMenuItem("Callout Selection", "Select the kind of callouts that can happen.");

        private UIMenuCheckboxItem AllowAverageCallouts = new UIMenuCheckboxItem("Generic Criminals", true, "Average callouts range from Jaywalking to store robberies. Expect anything from footchases to shootouts.");
        private UIMenuCheckboxItem AllowRaceCallouts = new UIMenuCheckboxItem("Illegal Races", true, "High speed chases await.");
        private UIMenuCheckboxItem AllowHeistCallouts = new UIMenuCheckboxItem("Heists", true, "These kind of criminals are specialized in big robberies, have a well planned escape plan and don't fear the police.");
        private UIMenuCheckboxItem AllowGangRiot = new UIMenuCheckboxItem("Gang activity", true, "Gang related shootouts, mostly. Hard to arrest, these people hate the pigs with all their heart.");
        private UIMenuCheckboxItem AllowMilitary = new UIMenuCheckboxItem("Military vehicles", true, "Someone stole something powerful.");
        private UIMenuCheckboxItem AllowTerrorism = new UIMenuCheckboxItem("Terrorists", true, "Resolve this callout quickly, because this kind of criminal actively tries to murder people. Beware of these fellas, they prefer to die rather than be arrested.");
        private UIMenuCheckboxItem AllowCargoStealers = new UIMenuCheckboxItem("Cargo Steal", true, "This criminal has stolen a commercial vehicle, most likely to resell its contents overseas or something like that.");
        private UIMenuCheckboxItem AllowEscapedPrisoner = new UIMenuCheckboxItem("Escaped Prisoners", true, "These people really want to dissapear for a while.");
        private UIMenuCheckboxItem AllowMainCharacters = new UIMenuCheckboxItem("Main Characters", true, "These callouts involve the Main Characters. You probably know what to expect.");

        public static UIMenuCheckboxItem AllowChaseNotifications = new UIMenuCheckboxItem("Chase status Notifications", true, "Updates you on the state of the cops/suspects involved in the current chase.");
        public static UIMenuCheckboxItem DebugNotifications = new UIMenuCheckboxItem("Debug mode", true, "Shows debug notifications meant for testing. '-' will spawn a harmless suspect near you and DEL will delete all criminals/cops.");
        public static UIMenuCheckboxItem AllowAutomaticDispatch = new UIMenuCheckboxItem("Automatic Dispatch", true, "When chasing suspects, cop units will be dispatched as they are needed. Else, you'll have to spawn them manually.");
        public static UIMenuCheckboxItem AllowOngoingChase = new UIMenuCheckboxItem("Allow Ongoing Chases", true, "This allows suspects to have other units already chasing them by the time you join in.");
        public static UIMenuCheckboxItem HelpNotifications = new UIMenuCheckboxItem("Help text", true, "While checked, the script will guide you around by displaying context-aware help text.");
        public static UIMenuCheckboxItem AutomaticCallouts = new UIMenuCheckboxItem("Get callouts automatically", true, "While checked, you'll get Callouts automatically, without having to press the ["+ForceCalloutKey+"] key.");



        private UIMenuItem UtilitiesMenuItem = new UIMenuItem("Utilities", "Some utilities to enhance your experience.");
        private UIMenu UtilitiesMenu = new UIMenu("Utilities", "Some utilities to enhance your experience.");
        //private UIMenuItem SaveCurrentVehicleLayout = new UIMenuItem("Save current vehicle Tuning Layout", "Use this option to add your current vehicle layout (color, liveries, mods, etc) to a text file. When responding to a Racing-related Callout, these Layouts will be randomly applied to the racer's vehicles.");
        //private UIMenuItem LoadRandomLayout = new UIMenuItem("Load random Tuning Layout", "Load a random layout from the Tuning Layouts list.");


        private UIMenuItem UISaveVehToFileMenuItem = new UIMenuItem("Custom Vehicles for criminals", "");
        private UIMenu UISaveVehToFileMenu = new UIMenu("Criminal Vehicles", "");

        private UIMenuListItem SaveVehToFileCategory = new UIMenuListItem("Category: ", Info.VehicleCategories,0,"Select the vehicle category you want the model to save to.");
        private UIMenuItem SaveVehToFile = new UIMenuItem("Save Vehicle", "This utility will save your current vehicle model to a file, in the category selected above. Once you have restarted the script the criminals will use these vehicles.");
        public static UIMenuCheckboxItem ForceUse = new UIMenuCheckboxItem("Only use these vehicles", false, "If enabled, criminals will only use the vehicles from the custom vehicle list. If disabled, criminals are able to use both Custom vehicles and vanilla vehicles.");


        private UIMenuItem UIMenuKey = new UIMenuItem("Change Menu key [B]", "Change the current key to open this menu.");
        private UIMenuItem UICalloutKey = new UIMenuItem("Change Force Callout key [B]", "Change the current key to force callouts.");

        public static List<CriminalType> CalloutPool = new List<CriminalType> { };


        void AddNewVehicles()
        {
            int i = 0;


            
            bool RDE = false;

            string CopFileResult = "backup.xml: ";

            string file =CopVehsandPedsLSPDFRFile;

            if (File.Exists(@"" + file))
            {

            }
            else
            {
                if (DangerousIndividuals.DebugNotifications.Checked) Util.WarnPlayer(ScriptName, "NO LSPD:FR BACKUP.XML FOUND", "~r~Backup.xml not found on" + file + ".");
                file = CopVehsandPedsFile;
            }



            if (File.Exists(@"" + file))
            {
                if (file == CopVehsandPedsFile) CopFileResult +="~b~"+ScriptName+"~w~~n~"; else CopFileResult += "~b~LSPD:FR~w~~n~";

                XmlDocument originalXml = new XmlDocument();
                originalXml.Load(@"" + file);

                //Local LSPD
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSPDCars);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSPDModels);

                //State Patrol (Highways) -  Simplified to a single model list from Los Santos
                foreach (XmlElement element in originalXml.SelectNodes("//StatePatrol/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.HighwayCars);
                foreach (XmlElement element in originalXml.SelectNodes("//StatePatrol/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.HighwayModels);



                //Local LSSD (Sheriff)
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSSDCars);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSSDModels);

                //Local BSCO (Blaine County)
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/BlaineCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.BCSOCars);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/BlaineCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.BCSOModels);


                //LSPD SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSPDLocalSWAT);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalPatrol/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);


                //LSSD SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//LocalSWAT/LosSantosCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSSDLocalSWAT);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalSWAT/LosSantosCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);

                //BCSO SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//LocalSWAT/BlaineCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.BCSOLocalSWAT);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalSWAT/BlaineCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);

                //LSPD NOOSE SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSPDSWATCars);
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);

                //LSSD NOOSE SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSSDSWATCars);
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);

                //BCSO NOOSE SWAT
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.BCSOSWATCars);
                foreach (XmlElement element in originalXml.SelectNodes("//NooseSWAT/LosSantosCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);

                //Local LSPD Air
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSPDHelis);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.HeliModels);

                //Local LSSD Air
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/LosSantosCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.LSSDHelis);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/LosSantosCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.HeliModels);

                //Local LSSD Air
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/BlaineCounty/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.BCSOHelis);
                foreach (XmlElement element in originalXml.SelectNodes("//LocalAir/BlaineCounty/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.HeliModels);

                //NOOSE Air - Simplified to a single model list from Los Santos
                foreach (XmlElement element in originalXml.SelectNodes("//NooseAir/LosSantosCity/VehicleSet/Vehicles/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.NOoSEHelis);
                foreach (XmlElement element in originalXml.SelectNodes("//NooseAir/LosSantosCity/VehicleSet/Peds/*")) if (new Model(element.InnerText).IsValid) Info.AddModelToList(element.InnerText, Info.SWATModels);


            }
            else
            {
                if (DangerousIndividuals.DebugNotifications.Checked) Util.WarnPlayer(ScriptName, "NO BACKUP.XML FOUND", "~r~Backup.xml not found on"+file+". All models returned to Default.");
                CopFileResult += "~b~Not found~w~~n~";
            }

            //Making sure all lists have at least one model

            if (Info.HwyPatrol.Count == 0) { Info.AddModelToList("POLICE4", Info.HighwayCars); }
            if (Info.LSPDModels.Count == 0) { Info.AddModelToList("s_m_y_hwaycop_01", Info.HighwayModels); }

            if (Info.LSPDCars.Count == 0) { Info.AddModelToList("POLICE", Info.LSPDCars); Info.AddModelToList("POLICE2", Info.LSPDCars); Info.AddModelToList("POLICE3", Info.LSPDCars); }
            if (Info.LSPDModels.Count == 0) { Info.AddModelToList("s_m_y_cop_01", Info.LSPDModels); Info.AddModelToList("s_f_y_cop_01", Info.LSPDModels); }

            if (Info.LSSDCars.Count == 0) { Info.AddModelToList("SHERIFF", Info.LSSDCars); }
            if (Info.LSSDModels.Count == 0) { Info.AddModelToList("s_m_y_sheriff_01", Info.LSSDModels); Info.AddModelToList("s_f_y_sheriff_01", Info.LSSDModels); }

            if (Info.BCSOCars.Count == 0) { Info.AddModelToList("SHERIFF", Info.BCSOCars); }
            if (Info.BCSOModels.Count == 0) { Info.AddModelToList("s_m_y_sheriff_01", Info.BCSOModels); Info.AddModelToList("s_f_y_sheriff_01", Info.BCSOModels); }

            if (Info.LSPDLocalSWAT.Count == 0) { Info.AddModelToList("FBI2", Info.LSPDLocalSWAT); }

            if (Info.LSSDLocalSWAT.Count == 0) { Info.AddModelToList("sheriff2", Info.LSSDLocalSWAT); }

            if (Info.BCSOLocalSWAT.Count == 0) { Info.AddModelToList("sheriff2", Info.BCSOLocalSWAT); }

            if (Info.LSPDSWATCars.Count == 0) { Info.AddModelToList("RIOT", Info.LSPDSWATCars); }

            if (Info.LSSDSWATCars.Count == 0) { Info.AddModelToList("RIOT", Info.LSSDSWATCars); }

            if (Info.BCSOSWATCars.Count == 0) { Info.AddModelToList("RIOT", Info.BCSOSWATCars); }

            if (Info.SWATModels.Count == 0) { Info.AddModelToList("s_m_y_swat_01", Info.SWATModels); }

            if (Info.LSPDHelis.Count == 0) { Info.AddModelToList("polmav", Info.LSPDHelis); }

            if (Info.LSSDHelis.Count == 0) { Info.AddModelToList("polmav", Info.LSSDHelis); }

            if (Info.BCSOHelis.Count == 0) { Info.AddModelToList("polmav", Info.BCSOHelis); }

            if (Info.BCSOCars.Count == 0) { Info.AddModelToList("SHERIFF", Info.BCSOCars); }

            if (Info.HeliModels.Count == 0) { Info.AddModelToList("s_m_y_pilot_01", Info.HeliModels); }









            if (File.Exists(@"" + CriminalVehicleFile))
            {
                XmlDocument originalXml = new XmlDocument();
                originalXml.Load(@"" + CriminalVehicleFile);

                foreach (XmlElement element in originalXml.SelectNodes("//Average/*"))
                {
                    bool Cleaned = false;
                    Model test = Info.TranslateToVehicleModel(element.InnerText);
                    if (test.IsValid)
                    {
                        i++;
                        if (ForceUse.Checked && !Cleaned)
                        {
                            Info.AverageVehs.Clear();
                            Cleaned = true;
                        }
                        Info.AddVehicleToList(test, Info.AverageVehs);
                    }
                }


                
                foreach (XmlElement element in originalXml.SelectNodes("//Race/*"))
                {
                    bool Cleaned = false;
                    Model test = Info.TranslateToVehicleModel(element.InnerText);
                    if (test.IsValid)
                    {
                        i++;
                        if (ForceUse.Checked && !Cleaned)
                        {
                            Info.SportVehs.Clear();
                            Cleaned = true;
                        }
                        Info.AddVehicleToList(test, Info.SportVehs);
                    }
                }

                foreach (XmlElement element in originalXml.SelectNodes("//Robbery/*"))
                {
                    bool Cleaned = false;
                    Model test = Info.TranslateToVehicleModel(element.InnerText);
                    if (test.IsValid)
                    {
                        i++;
                        if (ForceUse.Checked && !Cleaned)
                        {
                            Info.SUVs.Clear();
                            Cleaned = true;
                        }
                        Info.AddVehicleToList(test, Info.SUVs);
                    }
                }
                foreach (XmlElement element in originalXml.SelectNodes("//Heist/*"))
                {
                    bool Cleaned = false;
                    Model test = Info.TranslateToVehicleModel(element.InnerText);
                    if (test.IsValid)
                    {
                        i++;
                        if (ForceUse.Checked && !Cleaned)
                        {
                            Info.HeistVehs.Clear();
                            Cleaned = true;
                        }
                    }
                    Info.AddVehicleToList(test, Info.HeistVehs);
                }
                foreach (XmlElement element in originalXml.SelectNodes("//Military/*"))
                {
                    bool Cleaned = false;
                    Model test = Info.TranslateToVehicleModel(element.InnerText);
                    if (test.IsValid)
                    {
                        i++;
                        if (ForceUse.Checked && !Cleaned)
                        {
                            Info.MilitaryGradeVehs.Clear();
                            Cleaned = true;
                        }
                    }
                    Info.AddVehicleToList(test, Info.MilitaryGradeVehs);
                }
            }
            else
            {
                Util.WarnPlayer(ScriptName + " " + ScriptVer, "No custom vehicle file found", "No vehicle file has been found in "+ CriminalVehicleFile+".");
            }


            if (AllowMadMaxVehicles.Checked)
            {
                List<Model> vehs = new List<Model> { "deathdune", "gigahorse", "gonzo", "prock", "doofwagon", "warrig", };
                foreach (Model veh in vehs)
                {
                    if (veh.IsValid) { i++; Info.AddVehicleToList(veh, Info.MilitaryGradeVehs);
                    }
                }
            }
            if (AllowIVPack.Checked)
            {
                List<Model> vehs = new List<Model> { "sabre", "hakumai", "pinnacle", "esperanto", "interceptor", "vincent", "uranus", "willard", "pmp600", "chavos", "rebla", "admiral", "marbelle", "buccaneer3", "bobcat", "df8", "huntley2", "perennial", "pres", "sabre2", "solair", "schafter", "sentinel3", "sultan2", "stanier2", "fortune" };
                foreach (Model veh in vehs)
                {
                    if (veh.IsValid)
                    {
                        i++;
                        Info.AddVehicleToList(veh, Info.AverageVehs);
                    }
                }
                vehs = new List<Model> { "contender", "steed", "fxt", "huntley2", };
                foreach (Model veh in vehs)
                {
                    if (veh.IsValid)
                    {
                        i++;
                        Info.AddVehicleToList(veh, Info.HeavyVehs);
                    }
                }

                vehs = new List<Model> { "supergt", "cheetah2", "coquette4", "feltzer", "pres2", "turismo2", };
                foreach (Model veh in vehs)
                {
                    if (veh.IsValid)
                    {
                        i++;
                        Info.AddVehicleToList(veh, Info.SportVehs);
                    }
                }
                /*
                vehs = new List<Model> { "brickade", };
                foreach (Model veh in vehs)
                {
                    if (veh.IsValid)
                    {
                        i++;
                        Info.AddVehicleToList(veh, Info.StolenCashTrucks);
                    }
                }
                */
            }

            if (AllowRDEVehicles.Checked)
            {

                //RDE
                i++;
                Info.AddVehicleToList("police5", Info.LSPDCars);

                i++;
                Info.AddVehicleToList("pranger2", Info.SAPRCars);

                i++;
                Info.AddVehicleToList("sheriff3", Info.LSSDCars);

                i++;
                Info.AddVehicleToList("sheriffriot", Info.BCSOSWATCars);

                i++;
                Info.AddVehicleToList("nooseriot", Info.LSSDSWATCars);
                Info.RemoveVehicleFromLsit("nooseriot", Info.LSSDSWATCars);

                i++;
                Info.AddVehicleToList("nooseannihilator", Info.NOoSEHelis);
                Info.RemoveVehicleFromLsit("annihilator", Info.NOoSEHelis);

                i++;
                Info.AddVehicleToList("sheriffinsurgent",Info.ArmoredNOoSESheriff);
                //Info.RemoveVehicleFromLsit("insurgent", Info.ArmyCarsSheriff);

                i++;
                Info.AddVehicleToList("nooseinsurgent", Info.ArmoredNOoSE);
                //Info.RemoveVehicleFromLsit("insurgent", Info.ArmyCarsCity);


                //RDExtended Addon

                //Federal Law Addon

                //The Sheriffs of Blaine Addon
                i++;
                Info.AddVehicleToList("lssheriff", Info.LSSDCars);

                i++;
                Info.AddVehicleToList("lssheriff2", Info.LSSDCars);

                i++;
                Info.AddVehicleToList("lssheriff3", Info.LSSDCars);

                i++;
                Info.AddVehicleToList("lssheriff4", Info.LSSDCars);

                i++;
                Info.AddVehicleToList("sheriff3", Info.BCSOCars);

                i++;
                Info.AddVehicleToList("sheriff4", Info.BCSOCars);

                i++;
                Info.AddVehicleToList("sheriffmav2", Info.LSSDHelis);
                Info.RemoveVehicleFromLsit("polmav", Info.LSSDHelis);

                i++;
                Info.AddVehicleToList("sheriffmav", Info.BCSOHelis);
                Info.RemoveVehicleFromLsit("polmav", Info.BCSOHelis);

                i++;
                Info.AddVehicleToList("sheriffriot2", Info.LSSDSWATCars);

                i++;
                Info.AddVehicleToList("policeb2", Info.LSPDBikes);
                CopFileResult += "RDE: ";
                if (new Model("nooseriot").IsValid) CopFileResult += "~g~Detected"; else CopFileResult +="Not installed";
            }

            Util.WarnPlayer(ScriptName + " " + ScriptVer, "SCRIPT LOADED" , CopFileResult);
        }


        void ManageCalloutPool()
        {
            CalloutPool.Clear();

            if (AllowAverageCallouts.Checked)
            {
                CalloutPool.Add(CriminalType.SmartThug);
                CalloutPool.Add(CriminalType.AggresiveThug);
                CalloutPool.Add(CriminalType.AmateurRobbers);
                CalloutPool.Add(CriminalType.ExperiencedRobbers);
                CalloutPool.Add(CriminalType.PacificFleeingFoot);
            }
            if (AllowHeistCallouts.Checked)
            {
                CalloutPool.Add(CriminalType.NormalHeisters);
                CalloutPool.Add(CriminalType.ProffessionalsHeisters);
                CalloutPool.Add(CriminalType.MotorbikeHeisters);

                CalloutPool.Add(CriminalType.CashTruckStealers);
            }
            if (AllowGangRiot.Checked)
            {
                CalloutPool.Add(CriminalType.ViolentGang);
                CalloutPool.Add(CriminalType.ViolentBigGang);

                CalloutPool.Add(CriminalType.ViolentGangFamilies);
            }
            if (AllowRaceCallouts.Checked)
            {
                CalloutPool.Add(CriminalType.AmateurRacers);
                CalloutPool.Add(CriminalType.ProRacers);
            }
            if (AllowMilitary.Checked)
            {
                CalloutPool.Add(CriminalType.MilitaryStealers);
            }
            if (AllowTerrorism.Checked)
            {
                CalloutPool.Add(CriminalType.Terrorists);
            }
            if (AllowCargoStealers.Checked)
            {               
                CalloutPool.Add(CriminalType.CargoStealers);
                CalloutPool.Add(CriminalType.CommertialVanStealers);
            }
            if (AllowEscapedPrisoner.Checked)
            {
                CalloutPool.Add(CriminalType.EscapedPrisoner);
            }
            if (AllowMainCharacters.Checked)
            {
                CalloutPool.Add(CriminalType.MainCharacters);
            }

        }

        //GeneralInfo
        public string GenerateContextForSuspect(CriminalType type)
        {
            if (new[] { CriminalType.AggresiveThug, CriminalType.SmartThug, }.Contains(type)) return Info.GenericMediumCrime[Util.RandomInt(0, Info.GenericMediumCrime.Count - 1)];

            if (new[] { CriminalType.AmateurRobbers }.Contains(type)) return Info.GenericMediumCrime[Util.RandomInt(0, Info.GenericMediumCrime.Count - 1)];

            if (new[] { CriminalType.ExperiencedRobbers, }.Contains(type)) return Info.VehicleGenericMediumCrime[Util.RandomInt(0, Info.VehicleGenericMediumCrime.Count - 1)];

            if (new[] { CriminalType.AmateurRacers, CriminalType.ProRacers }.Contains(type)) return Info.RacerTitle[Util.RandomInt(0, Info.RacerTitle.Count - 1)];

            if (new[] { CriminalType.ProffessionalsHeisters }.Contains(type)) return Info.BigHeistTitle[Util.RandomInt(0, Info.BigHeistTitle.Count - 1)];

            if (new[] { CriminalType.NormalHeisters, CriminalType.MotorbikeHeisters }.Contains(type)) return Info.NormalHeistTitle[Util.RandomInt(0, Info.NormalHeistTitle.Count - 1)];

            if (new[] { CriminalType.CashTruckStealers }.Contains(type)) return Info.CashTruckSteal[Util.RandomInt(0, Info.CashTruckSteal.Count - 1)];

            if (new[] { CriminalType.ViolentGang, CriminalType.ViolentBigGang, CriminalType.ViolentGangFamilies }.Contains(type)) return Info.FootGangTitle[Util.RandomInt(0, Info.FootGangTitle.Count - 1)];

            if (new[] { CriminalType.MilitaryStealers }.Contains(type)) return Info.MilitaryVehicleTitle[Util.RandomInt(0, Info.MilitaryVehicleTitle.Count - 1)];

            if (new[] { CriminalType.PacificFleeingFoot }.Contains(type)) return Info.FootGenericSmallCrime[Util.RandomInt(0, Info.FootGenericSmallCrime.Count - 1)];

            if (new[] { CriminalType.Terrorists }.Contains(type)) return Info.TerroristActivityTitle[Util.RandomInt(0, Info.TerroristActivityTitle.Count - 1)];

            if (new[] { CriminalType.CargoStealers }.Contains(type)) return Info.CargoStealersTitle[Util.RandomInt(0, Info.CargoStealersTitle.Count - 1)];
            if (new[] { CriminalType.CommertialVanStealers }.Contains(type)) return Info.CommercialVanStealersTitle[Util.RandomInt(0, Info.CommercialVanStealersTitle.Count - 1)];

            if (new[] { CriminalType.EscapedPrisoner }.Contains(type)) return Info.EscapedPrisonerTitle[Util.RandomInt(0, Info.EscapedPrisonerTitle.Count - 1)];

            if (new[] { CriminalType.MainCharacters }.Contains(type)) return Info.GenericWantedPersonTitle[Util.RandomInt(0, Info.GenericWantedPersonTitle.Count - 1)];


            //return "No Context found for " + type.ToString();
            return "Wanted person";
        }

        static public string GenerateContextDetailsForSuspect(SuspectHandler suspect)
        {
            string details = "";
            string accompanied = "";
            if (suspect.Partners.Count > 0) accompanied = "s";
            if (Util.CanWeUse(suspect.VehicleChosen))
            {
                details += "Suspect" + accompanied + " seen in a " + (SimplifyColorString(suspect.VehicleChosen.PrimaryColor.ToString())).ToLowerInvariant() + " ~b~" + GetVehicleClassName(suspect.VehicleChosen) + "~w~, heading " + Util.GetWhereIsHeaded(suspect.Criminal, false) + ". ";
                if (suspect.Flags.Contains(CriminalFlags.CAN_DRIVEBY)) details += "~r~Shots fired~w~, proceed with caution.";
            }
            else
            {
                details += "Suspect" + accompanied + " last seen in " + World.GetStreetName(suspect.Criminal.Position) + ".";
                if (suspect.Criminal.Weapons.Current.Hash != WeaponHash.Unarmed)
                {
                    if (suspect.Flags.Contains(CriminalFlags.CAN_STANDOFF_CAUTIOUS) && Util.RandomInt(0, 10) < 20) details += " Suspect" + accompanied + " might be armed, proceed with caution.";
                    if (suspect.Flags.Contains(CriminalFlags.CAN_STANDOFF_AGGRESIVE) && Util.RandomInt(0, 10) < 90) details += " ~r~Gunfire has been reported in the vicinity.";
                }
            }
            return details;
        }

        static public string SimplifyColorString(string color)
        {

            if (color.Contains("red")) return "red";
            if (color.Contains("blue")) return "blue";
            if (color.Contains("black")) return "black";
            if (color.Contains("orange")) return "orange";
            if (color.Contains("white")) return "white";
            if (color.Contains("gray")) return "gray";


            string newcolor = color;
            string[] replace = { "Seafoam", "Poly", "Anthracite", "Sunrise", "Graphite", "Pueblo", "Golden", "Metallic", "Matte", "Pure", "PoliceCar", "Util", "Worn", "Dark", "Light", "Taxi", "Desert", "Foliage", "Hot", "Hunter", "Midnight", "Marine", "Formula", "Frost", "Garnet", "Epsilon", "Moss", "Olive", "Util", "Ultra", "Salmon", "Gasoline" };
            foreach (string curr in replace)
            {
                newcolor = newcolor.Replace(curr, string.Empty);
            }


            return newcolor;
        }
        static public string GetVehicleClassName(Vehicle veh)
        {
            if (new Model[] { VehicleHash.Kuruma2, VehicleHash.Insurgent, VehicleHash.Cognoscenti2, VehicleHash.Cog552, VehicleHash.Schafter5, VehicleHash.Schafter6, VehicleHash.Baller5, VehicleHash.Baller6, VehicleHash.Limo2 }.Contains(veh.Model)) return "Armored Vehicle";
            if (veh.Model == VehicleHash.Stockade) return "Securicar";
            if (veh.Model == VehicleHash.PBus) return "Prison Bus";
            if (veh.Model.IsBike) return "Bike";
            if (veh.Model.IsHelicopter) return "Helicopter";
            if (veh.Model.IsPlane) return "Plane";

            switch (veh.ClassType)
            {
                case VehicleClass.SUVs:
                    {
                        return "SUV";
                    }
                case VehicleClass.Boats:
                    {
                        return "Boat";
                    }
                case VehicleClass.Emergency:
                    {
                        return "Emergency Vehicle";
                    }
                case VehicleClass.Commercial:
                    {
                        return "Commercial Vehicle";
                    }
                case VehicleClass.Compacts:
                    {
                        return "Compact";
                    }
                case VehicleClass.Coupes:
                    {
                        return "Coupe";
                    }
                case VehicleClass.Muscle:
                    {
                        return "Muscle";
                    }
                case VehicleClass.OffRoad:
                    {
                        return "4X4";
                    }
                case VehicleClass.Sedans:
                    {
                        return "Sedan";
                    }
                case VehicleClass.Sports:
                    {
                        return "Sports Vehicle";
                    }
                case VehicleClass.Super:
                    {
                        return "High End Vehicle";
                    }
                case VehicleClass.Motorcycles:
                    {
                        return "Motorcycle";
                    }
                case VehicleClass.Vans:
                    {
                        return "Van";
                    }
            }
            return "unidentified vehicle";

        }

        public static int UnitsPatrolling = 3;
        public static string debugpath = "scripts\\DangerousIndividuals/debug.txt";
        public DangerousIndividuals()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            LoadSettings(ConfigFilename);

            File.WriteAllText(@""+debugpath, "\n - "+DateTime.Now);

            //UI.Notify("~b~[" + ScriptName + " " + ScriptVer + "] (Closed beta) ~w~loaded.");

            foreach (Vector3 station in Info.AllPoliceStations)
            {
                Blip stationblip = World.CreateBlip(station);
                stationblip.Sprite = BlipSprite.PoliceStation;
                stationblip.IsShortRange = true;
                StationBlips.Add(stationblip);
            }
            Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "WEB_LOSSANTOSPOLICEDEPT", true);
            Function.Call(Hash.REQUEST_ANIM_DICT, "MP_ARRESTING");
            Function.Call(Hash.REQUEST_ANIM_DICT, "mp_bank_heist_1");

            //Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "WEB_LOSSANTOSPOLICEDEPT", true);


            //Cops love cops, cops love me
            Util.SetRelationshipBetweenGroups(CopsRLGroup, Game.Player.Character.RelationshipGroup, Relationship.Companion, true);
            Util.SetRelationshipBetweenGroups(CopsRLGroup, Game.GenerateHash("COP"), Relationship.Companion, true);

            //Cops love surrendered criminals
            Util.SetRelationshipBetweenGroups(CopsRLGroup, SurrenderedCriminalsRLGroup, Relationship.Companion, true);
            Util.SetRelationshipBetweenGroups(CriminalsRLGroup, SurrenderedCriminalsRLGroup, Relationship.Companion, true);
            Util.SetRelationshipBetweenGroups(Game.GenerateHash("COP"), SurrenderedCriminalsRLGroup, Relationship.Companion, true);

            //Criminals hate criminals from the othergroup
            Util.SetRelationshipBetweenGroups(EnemyGangRLGroup, CriminalsRLGroup, Relationship.Hate, true);


            //Criminals hate player and cops
            Util.SetRelationshipBetweenGroups(CriminalsRLGroup, CopsRLGroup, Relationship.Hate, true);
            Util.SetRelationshipBetweenGroups(CriminalsRLGroup, Game.GenerateHash("PLAYER"), Relationship.Hate, true);

            Util.SetRelationshipBetweenGroups(PublicEnemyRLGroup, CopsRLGroup, Relationship.Hate, true);
            Util.SetRelationshipBetweenGroups(PublicEnemyRLGroup, Game.GenerateHash("CIVMALE"), Relationship.Hate, true);
            Util.SetRelationshipBetweenGroups(PublicEnemyRLGroup, Game.GenerateHash("CIVFEMALE"), Relationship.Hate, true);

            Util.SetRelationshipBetweenGroups(PublicEnemyRLGroup, Game.GenerateHash("PLAYER"), Relationship.Hate, true);

            Util.SetRelationshipBetweenGroups(EnemyGangRLGroup, CopsRLGroup, Relationship.Hate, true);
            Util.SetRelationshipBetweenGroups(EnemyGangRLGroup, Game.GenerateHash("PLAYER"), Relationship.Hate, true);


            Game.Player.Character.IsPriorityTargetForEnemies = false;


            //Menu colors
            Sprite banner = new Sprite("","",Point.Empty, Size.Empty);
            banner.Color = Color.SteelBlue;
            UICopsChasingSelectedCriminalMenu.SetBannerType(banner);

            SettingsMenu.SetBannerType(banner);
            mainMenu.SetBannerType(banner);


            _menuPool.Add(mainMenu);

            
            _menuPool.Add(SettingsMenu);
            _menuPool.Add(UICopsChasingSelectedCriminalMenu);
            _menuPool.Add(PoliceSpawnMenu);
            _menuPool.Add(NooseSpawnMenu);
            _menuPool.Add(ArmySpawnMenu);

            _menuPool.Add(RealismMenu);
            _menuPool.Add(UtilitiesMenu);
            _menuPool.Add(CalloutSelectionMenu);
            _menuPool.Add(AddonSelectionMenu);
            _menuPool.Add(UISaveVehToFileMenu);

            //UtilitiesMenu.AddItem(SaveCurrentVehicleLayout);
            //UtilitiesMenu.AddItem(LoadRandomLayout);


            SettingsMenu.AddItem(CalloutSelectionMenuItem);
            SettingsMenu.BindMenuToItem(CalloutSelectionMenu, CalloutSelectionMenuItem);
            CalloutSelectionMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);

            SettingsMenu.AddItem(RealismMenuItem);
            SettingsMenu.BindMenuToItem(RealismMenu, RealismMenuItem);
            RealismMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);


            SettingsMenu.AddItem(AddonSelectionMenuItem);
            SettingsMenu.BindMenuToItem(AddonSelectionMenu, AddonSelectionMenuItem);
            AddonSelectionMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);

            //SettingsMenu.AddItem(UtilitiesMenuItem);
            SettingsMenu.BindMenuToItem(UtilitiesMenu, UtilitiesMenuItem);
            UtilitiesMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);

            AddonSelectionMenu.AddItem(AllowMadMaxVehicles);
            AddonSelectionMenu.AddItem(AllowRDEVehicles);
            AddonSelectionMenu.AddItem(AllowIVPack);

            AddonSelectionMenu.AddItem(UISaveVehToFileMenuItem);
            AddonSelectionMenu.BindMenuToItem(UISaveVehToFileMenu, UISaveVehToFileMenuItem);

            UISaveVehToFileMenu.AddItem(SaveVehToFileCategory);
            UISaveVehToFileMenu.AddItem(SaveVehToFile);
            UISaveVehToFileMenu.AddItem(ForceUse);
            ForceUse.Checked = false;
            UISaveVehToFileMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);


            CalloutSelectionMenu.AddItem(AllowAverageCallouts);
            CalloutSelectionMenu.AddItem(AllowRaceCallouts);
            CalloutSelectionMenu.AddItem(AllowHeistCallouts);
            CalloutSelectionMenu.AddItem(AllowGangRiot);
            CalloutSelectionMenu.AddItem(AllowMilitary);
            CalloutSelectionMenu.AddItem(AllowTerrorism);
            CalloutSelectionMenu.AddItem(AllowCargoStealers);
            CalloutSelectionMenu.AddItem(AllowEscapedPrisoner);
            CalloutSelectionMenu.AddItem(AllowMainCharacters);

            SettingsMenu.AddItem(HelpNotifications);
            SettingsMenu.AddItem(AllowChaseNotifications);
            SettingsMenu.AddItem(AmbientPedsCanBeCriminals);
            SettingsMenu.AddItem(AutomaticCallouts);


            RealismMenu.AddItem(PlayerCareMenu);

            RealismMenu.AddItem(RealisticPosDispatch);

            // Don't add Debug Mode in the released version
            SettingsMenu.AddItem(DebugNotifications);
            DebugNotifications.Checked = false;
            SettingsMenu.AddItem(AllowAutomaticDispatch);
            AllowAutomaticDispatch.Checked = false;
            SettingsMenu.AddItem(AllowOngoingChase);

            SettingsMenu.AddItem(UIMenuKey);
            SettingsMenu.AddItem(UICalloutKey);

            mainMenu.AddItem(UISelectCriminal);



            mainMenu.AddItem(PoliceSpawnMenuItem);
            mainMenu.BindMenuToItem(PoliceSpawnMenu, PoliceSpawnMenuItem);
            PoliceSpawnMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);
            PoliceSpawnMenu.AddItem(UISpawnLSPDCar);
            PoliceSpawnMenu.AddItem(UISpawnBike);
            PoliceSpawnMenu.AddItem(UISpawnHeli);
            PoliceSpawnMenu.AddItem(UISpawnTransport);


            mainMenu.AddItem(NooseSpawnMenuItem);
            mainMenu.BindMenuToItem(NooseSpawnMenu, NooseSpawnMenuItem);
            NooseSpawnMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);
            NooseSpawnMenu.AddItem(UISpawnLocalSWAT);
            NooseSpawnMenu.AddItem(UISpawnSWAT);
            NooseSpawnMenu.AddItem(UISpawnNOoSEHeli);
            //if (AllowRDEVehicles.Checked ) NooseSpawnMenu.AddItem(UISpawnArmoredSWAT);


            mainMenu.AddItem(ArmySpawnMenuItem);
            mainMenu.BindMenuToItem(ArmySpawnMenu, ArmySpawnMenuItem);
            ArmySpawnMenuItem.SetLeftBadge(UIMenuItem.BadgeStyle.Michael);
            ArmySpawnMenu.AddItem(UISpawnArmy);
            ArmySpawnMenu.AddItem(UISpawnArmyHeli);


            mainMenu.AddItem(CloseRoads);

            mainMenu.BindMenuToItem(UICopsChasingSelectedCriminalMenu, UISelectCriminal);
            /*
            UICopsChasingSelectedCriminalMenu.AddItem(UISuspectHeading);
            UICopsChasingSelectedCriminalMenu.AddItem(UIsuspectPos);
            UICopsChasingSelectedCriminalMenu.AddItem(UISuspectStatus);
            */
            foreach (UIMenu menu in _menuPool.ToList())
            {
                menu.RefreshIndex();
                menu.OnItemSelect += OnItemSelect;
                menu.OnIndexChange += OnIndexChange;
            }


            UIMenuKey.Text = "Change Menu key [" + (MainMenuKey).ToString() + "]";
            UICalloutKey.Text = "Change Force Callout key [" + (ForceCalloutKey).ToString() + "]";



            AddNewVehicles();
        }


        public static int UnitsNearSuspect(SuspectHandler suspect, float radius)
        {
            int Number = 0;
            foreach (CopUnitHandler Unit in CopsChasing)
            {
                if (Unit.Leader.Position.DistanceTo(suspect.Criminal.Position) < radius) Number++;
            }
            return Number;
        }
        public string GetSuspectStatus(SuspectHandler suspect, bool State, bool Street, bool Area, bool direction)
        {
            string desc = "";
            if (State)
            {
                if (suspect.State == CriminalState.Surrendering || suspect.State == CriminalState.Arrested || suspect.State == CriminalState.DealtWith)
                {
                    desc += "In custody";
                }
                else
                {
                    if (suspect.Criminal.IsInCombat) desc += "shootout";
                    else
                    {
                        if (Util.CanWeUse(suspect.Criminal.CurrentVehicle)) desc += "" + suspect.Criminal.CurrentVehicle.FriendlyName + ""; else desc += "Fleeing (foot)";
                    }
                    //if (suspect.Partners.Count > 0) desc += ", ~y~Accompanied";
                }
            }
            if (direction) desc += " - " + Util.GetWhereIsHeaded(suspect.Criminal, true);
            if (Street) desc += " - " + World.GetStreetName(suspect.Criminal.Position);
            if (Area) desc += " - " + World.GetZoneName(suspect.Criminal.Position);

            return desc;
        }



        public void HandleSuspectSelectionMenu()
        {
            if (CriminalsActive.Count > 0)
            {
                if (!_menuPool.IsAnyMenuOpen())
                {
                    int CriminalNumber = 0;

                    CriminalSelection.Clear();
                    for (int i = 0; i < CriminalsActive.Count; i++)
                    {
                        CriminalNumber++;
                        CriminalSelection.Add(CriminalNumber + " " + GetSuspectStatus(CriminalsActive[i], true, true, false, true));
                    }
                    int pos = 0;
                    if (mainMenu.MenuItems.Contains(UISelectCriminal))
                    {
                        pos = mainMenu.MenuItems.IndexOf(UISelectCriminal);
                        mainMenu.RemoveItemAt(mainMenu.MenuItems.IndexOf(UISelectCriminal));
                        UISelectCriminal = new UIMenuListItem("", CriminalSelection, 0);
                        //UISelectCriminal.Description = "Headed " + Util.GetWhereIsHeaded(CriminalsActive[UISelectCriminal.Index].Criminal, false);
                        //UISuspectHeading.Text = "Headed " + Util.GetWhereIsHeaded(CriminalsActive[UISelectCriminal.Index].Criminal, false);
                        //UIsuspectPos.Text = "Last Seen in " + World.GetStreetName(CriminalsActive[UISelectCriminal.Index].Criminal.Position);
                        //UISuspectStatus.Text = CriminalsActive[UISelectCriminal.Index].State.ToString();


                        mainMenu.AddItem(UISelectCriminal);
                        mainMenu.RemoveItemAt(mainMenu.MenuItems.IndexOf(UISelectCriminal));
                        mainMenu.MenuItems.Insert(pos, UISelectCriminal);
                    }
                    else
                    {
                        UISelectCriminal = new UIMenuListItem("", CriminalSelection, 0);
                        mainMenu.AddItem(UISelectCriminal);
                    }

                    mainMenu.BindMenuToItem(UICopsChasingSelectedCriminalMenu, UISelectCriminal);

                }
                //mainMenu.CounterPretext= "Headed " + Util.GetWhereIsHeaded(CriminalsActive[UISelectCriminal.Index].Criminal);

            }

        }


        public void OnIndexChange(UIMenu sender, int index)
        {
            /*
            if(index == sender.MenuItems.IndexOf(UISelectCriminal))
            {
                if (!mainMenu.MenuItems.Contains(UISuspectHeading))  mainMenu.AddItem(UISuspectHeading);
            }
            else
            {
               if(mainMenu.MenuItems.Contains(UISuspectHeading)) mainMenu.RemoveItemAt(mainMenu.MenuItems.IndexOf(UISuspectHeading));
            }
            */
        }
        public void UpdateCopsChasingCriminals()
        {

        }
        public void OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == SaveVehToFile)
            {
                if (Util.CanWeUse(Game.Player.Character.CurrentVehicle))
                {
                    string Vehicle = Game.Player.Character.CurrentVehicle.Model.Hash.ToString();
                    Model Vehicle2 = int.Parse(Vehicle);
                    if (Vehicle2.IsVehicle)
                    {
                        SaveCriminalVehicle(CriminalVehicleFile, SaveVehToFileCategory.IndexToItem(SaveVehToFileCategory.Index), Game.Player.Character.CurrentVehicle);
                    }
                    else
                    {
                        Util.WarnPlayer(ScriptName + " " + ScriptVer, "MODEL ERROR", "Cannot save this vehicle. " + Vehicle + " Doesn't seem to be a valid vehicle hash.");
                        Util.WarnPlayer(ScriptName + " " + ScriptVer, "MODEL ERROR", "Please, edit " + CriminalVehicleFile + " yourself and add the vehicle name/hash manually.");
                    }
                }
            }
            //Keys
            if (selectedItem == UIMenuKey)
            {
                bool CouldParse = Enum.TryParse<Keys>(Game.GetUserInput(30), out MainMenuKey);

                if (!CouldParse)
                {
                    UI.Notify("~y~That key does not exist. ~w~Make sure that capital letters match (Add instead of add).");
                }
                else
                {
                    UIMenuKey.Text = "Change Main Menu key [" + (MainMenuKey).ToString() + "]";
                }
            }

            if (selectedItem == UICalloutKey)
            {
                bool CouldParse = Enum.TryParse<Keys>(Game.GetUserInput(30), out ForceCalloutKey);

                if (!CouldParse)
                {
                    UI.Notify("~y~That key does not exist. ~w~Make sure that capital letters match (Add instead of add).");
                }
                else
                {
                    UICalloutKey.Text = "Change Force Callout key [" + (ForceCalloutKey).ToString() + "]";

                }
            }

            //Vehicle Layout utility
            /* Removed because of Util.RandomTuning existence
            if (selectedItem == SaveCurrentVehicleLayout)
            {
                Vehicle veh = Game.Player.Character.CurrentVehicle;
                if (Util.CanWeUse(veh))
                {
                    string Error = Util.SaveVehicleLayoutToFile(veh, LayoutFile);
                    if (Error != "") UI.Notify("~o~Error applying Layout: ~w~" + Error);
                    else UI.Notify("Vehicle layout saved to " + LayoutFile + ".");
                }
                else UI.Notify("~o~You need to be in a vehicle to do that.");
            }
            if (selectedItem == LoadRandomLayout)
            {
                Vehicle veh = Game.Player.Character.CurrentVehicle;
                if (Util.CanWeUse(veh))
                {
                    string Error = Util.ApplyRandomVehicleLayoutFromFile(veh, LayoutFile);
                    if (Error != "") UI.Notify("~o~Error applying Layout: ~w~" + Error);
                }
                else UI.Notify("~o~You need to be in a vehicle to do that.");
            }*/




            if (CriminalsActive.Count > 0)
            {

                if (selectedItem == UISelectCriminal)
                {

                    RefreshBackupList();
                    /*
                    for (int i = 0; i < UICopsChasingSelectedCriminalMenu.MenuItems.Count; i++)
                    {
                        UICopsChasingSelectedCriminalMenu.Clear();
                    }*/

                    /*
                    UICopsChasingSelectedCriminalMenu.Clear();
                    foreach (string text in GetCopschasingIt(CriminalsActive[UISelectCriminal.Index]))
                    {
                        UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuItem(text));
                    }
                    */
                    //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Status: " + CriminalsActive[UISelectCriminal.Index].State.ToString(), Color.SteelBlue, Color.SteelBlue));
                    //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Headed " + Util.GetWhereIsHeaded(CriminalsActive[UISelectCriminal.Index].Criminal, false), Color.SteelBlue, Color.SteelBlue));
                    //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Last Seen: " + World.GetStreetName(CriminalsActive[UISelectCriminal.Index].Criminal.Position), Color.SteelBlue, Color.SteelBlue));


                }

                if (sender == UICopsChasingSelectedCriminalMenu && UICopsChasingSelectedCriminalMenu.MenuItems.Count > 0)
                {
                    SuspectHandler criminal = CriminalsActive[UISelectCriminal.Index];     
                    if(criminal.CopsChasingMe.Count != UICopsChasingSelectedCriminalMenu.MenuItems.Count)
                    {
                        UI.Notify("~o~Item mismatch, please go back and open this menu again.");
                        return;
                    }
                    CopUnitHandler cop = criminal.CopsChasingMe[index];


                    if (selectedItem.Enabled)
                    {
                        cop.ShouldRemoveCopUnit = true;
                        selectedItem.Text += " - Dismissed";
                        selectedItem.Enabled = false;
                    }
                    else
                    {
                        UI.Notify("Item already dismissed");
                    }

                 // RefreshBackupList();

                    /*
                    selectedItem.Enabled = false;

                        selectedItem.Text = "Dismised";
                        UI.Notify("Index: " + index + " | Cops: " + CriminalsActive[UISelectCriminal.Index].CopsChasingMe.Count);

                         Script.Wait(1000);


                        if (index < CriminalsActive[UISelectCriminal.Index].CopsChasingMe.Count - 1)
                    {
                        CriminalsActive[UISelectCriminal.Index].CopsChasingMe[index].ShouldRemoveCopUnit = true;
                    }
                        

                    */

                    /*
                    if (CopsChasing.Count <= correctedindex)
                    {
                        CriminalsActive[UISelectCriminal.Index].CopsChasingMe[correctedindex].ShouldRemoveCopUnit = true;

                        Util.AddNotification("web_lossantospolicedept", "~b~" + CopsChasing[correctedindex].CopVehicle.FriendlyName + " unit", "Unit Dismissed", "I'm no longer required.");
                        CopsChasing[correctedindex].ShouldRemoveCopUnit = true;
                        //CopsChasing[index].FindNewTarget();
                        UICopsChasingSelectedCriminalMenu.RemoveItemAt(index);
                    }
                    */

                    return;
                }



                List<Vector3> Stations = new List<Vector3>();
                Stations.AddRange(Info.LSSDPoliceStations);
                Stations.AddRange(Info.LSPDPoliceStations);

                //Vector3 pos = World.GetNextPositionOnStreet(Util.GetClosestLocation(CriminalsActive[UISelectCriminal.Index].Criminal.Position, Stations)).Around(5f);
                Vector3 desiredPosition = CriminalsActive[UISelectCriminal.Index].Criminal.Position;
                if (Game.IsWaypointActive) desiredPosition = World.GetWaypointPosition();
                if (selectedItem == UISpawnLSPDCar)
                {
                    if (UnitsPatrolling > 0)
                    {
                        UnitsPatrolling--;
                        if (UnitsPatrolling == 0)
                        {
                            if (AllowChaseNotifications.Checked) Util.Notify("web_lossantospolicedept", "~b~DISPATCH", "NO NEARBY UNITS", "Any extra patrol units called will come from the closest Station.");
                        }
                    }
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.AveragePolice, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.AveragePolice, desiredPosition), true));
                }

                if (selectedItem == UISpawnBike)
                {
                    if (UnitsPatrolling > 0)
                    {
                        UnitsPatrolling--;
                        if (UnitsPatrolling == 0)
                        {
                            if (AllowChaseNotifications.Checked) Util.Notify("web_lossantospolicedept", "~b~DISPATCH", "NO NEARBY UNITS", "Any new patrol units called will come from the closest Station.");
                        }
                    }
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.Bike, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.Bike, desiredPosition), true));
                }
                if (selectedItem == UISpawnLocalSWAT)
                {
                    if (CriminalsActive.Count > 0) CopsChasing.Add(new CopUnitHandler(CopUnitType.LocalNoose, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.LocalNoose, desiredPosition), true));
                }
                if (selectedItem == UISpawnTransport)
                {
                    if (CriminalsActive.Count > 0) CopsChasing.Add(new CopUnitHandler(CopUnitType.PrisonerTransporter, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.PrisonerTransporter, desiredPosition), true));
                }
                if (selectedItem == UISpawnArmoredSWAT)
                {
                    if (CriminalsActive.Count > 0) CopsChasing.Add(new CopUnitHandler(CopUnitType.InsurgentNoose, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.InsurgentNoose, desiredPosition), true));
                }
                if (selectedItem == UISpawnNOoSEHeli)
                {
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.NOoSEAirUnit, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.NOoSEAirUnit, desiredPosition), true));
                }
                if (selectedItem == UISpawnHeli)
                {
                    //UI.Notify("Criminal pos:" + desiredPosition);
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.AirUnit, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.AirUnit, desiredPosition), true));
                }
                if (selectedItem == UISpawnSWAT)
                {
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.NOoSE, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.NOoSE, desiredPosition), true));
                }
                if (selectedItem == UISpawnArmy)
                {
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.Army, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.Army, desiredPosition), true));
                }
                if (selectedItem == UISpawnArmyHeli)
                {
                    if (CriminalsActive.Count > 0) DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.ArmyAirUnit, CriminalsActive[UISelectCriminal.Index], Info.GetSpawnpointFor(CopUnitType.ArmyAirUnit, desiredPosition), true));
                }
            }
        }

        List<string> GetCopschasingIt(SuspectHandler Suspect)
        {
            List<string> CopList = new List<string>();

            CopschasingThatCriminal.Clear();
            foreach (CopUnitHandler unit in CopsChasing)
            {
                if (unit.Suspect == Suspect)
                {

                    string t = unit.CopVehicle.FriendlyName + " Unit";

                    if (unit.ShouldRemoveCopUnit) t += " - Dismissed";
                    CopList.Add(t);
                    CopschasingThatCriminal.Add(t);
                }
            }
            return CopList;
        }
        void RefreshBackupList()
        {
            /*
            for (int i = 0; i < UICopsChasingSelectedCriminalMenu.MenuItems.Count; i++)
            {
                UICopsChasingSelectedCriminalMenu.RemoveItemAt(0);

            }
            */
            UICopsChasingSelectedCriminalMenu.Clear();

            //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Status: " + CriminalsActive[UISelectCriminal.Index].State.ToString(), Color.SteelBlue, Color.SteelBlue));
            //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Headed " + Util.GetWhereIsHeaded(CriminalsActive[UISelectCriminal.Index].Criminal, false), Color.SteelBlue, Color.SteelBlue));
            //UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuColoredItem("Last Seen: " + World.GetStreetName(CriminalsActive[UISelectCriminal.Index].Criminal.Position), Color.SteelBlue, Color.SteelBlue));

            foreach (string text in GetCopschasingIt(CriminalsActive[UISelectCriminal.Index]))
            {

                UIMenuItem d= new UIMenuItem(text);
                //if (text.Contains("Dismissed")) d.Enabled = false;
                UICopsChasingSelectedCriminalMenu.AddItem(new UIMenuItem(text));

            }
        }
        static public int NumberOfUnitsDispatched(CopUnitType type)
        {
            int Number = 0;
            foreach (CopUnitHandler unit in CopsChasing)
            {
                if (unit.UnitType == type) Number++;
            }
            return Number;
        }

        static public int NumberOfUnitsDispatched(SuspectHandler suspect, CopUnitType type)
        {
            int Number = 0;
            foreach (CopUnitHandler unit in suspect.CopsChasingMe)
            {
                if (unit.UnitType == type) Number++;
            }
            return Number;
        }
        public bool IsPlayerChasingSuspect(SuspectHandler suspect)
        {
            if (suspect == null) return false;
            else return Game.Player.Character.IsInRangeOf(suspect.Criminal.Position, 100f);
        }
        public void HandleAutomaticCopDispatch()
        {
            //Only handled if cops don't already double the criminals, capped at 10 cop units
            if (CriminalsActive.Count > 0 && CopsChasing.Count < (CriminalsActive.Count*2) && CopsChasing.Count<10)
            {
                foreach (SuspectHandler Suspect in CriminalsActive)
                {
                    bool ShouldDispatch = false;
                    CopUnitType UnitType = CopUnitType.AveragePolice;

                    if (Suspect.Surrendered() && (NumberOfUnitsDispatched(CopUnitType.AveragePolice) + NumberOfUnitsDispatched(CopUnitType.Patrol) + NumberOfUnitsDispatched(CopUnitType.PrisonerTransporter)) * 2 < CriminalsActive.Count) //&& !IsPlayerChasingSuspect(Suspect)
                    {
                        ShouldDispatch = true;
                        UnitType = CopUnitType.PrisonerTransporter;
                    }
                    if (!Suspect.Surrendered())
                    {
                        //Patrol Dispatch
                        if (Suspect.Auth_DeadlyForce)// && NumberOfUnitsDispatched(Suspect, CopUnitType.AveragePolice) < 2)|| (!IsPlayerChasingSuspect(Suspect) && NumberOfUnitsDispatched(Suspect, CopUnitType.AveragePolice) == 0)
                        {
                            if (NumberOfUnitsDispatched(Suspect, CopUnitType.AveragePolice) < 2)
                            {
                                ShouldDispatch = true;
                                UnitType = CopUnitType.AveragePolice;
                            }
                        }
                        else
                        {
                            if (NumberOfUnitsDispatched(Suspect, CopUnitType.Patrol) < 1 && !IsPlayerChasingSuspect(Suspect))
                            {
                                ShouldDispatch = true;
                                UnitType = CopUnitType.AveragePolice;
                            }
                        }

                        //Air Units dispatch
                        if (NumberOfUnitsDispatched(CopUnitType.AirUnit) < 2 && Suspect.LOSTreshold > LostLOSThreshold + 30)
                        {
                            if (Util.CanWeUse(Suspect.VehicleChosen) && Suspect.VehicleChosen.Speed > 20f && !IsPlayerChasingSuspect(Suspect))
                            {
                                CopUnitHandler closestcop = GetClosestCop(Suspect, true, false);
                                if (closestcop == null || !closestcop.Leader.IsInRangeOf(Suspect.Criminal.Position, 100f))
                                {
                                    ShouldDispatch = true;
                                    UnitType = CopUnitType.AirUnit;
                                }
                            }
                        }

                        //Noose Dispatch
                        if (Suspect.CopsKilled > 2 && NumberOfUnitsDispatched(Suspect, CopUnitType.LocalNoose) < 1)
                        {
                            ShouldDispatch = true;
                            UnitType = CopUnitType.LocalNoose;
                        }
                        if (Suspect.CopsKilled > 5 && NumberOfUnitsDispatched(Suspect, CopUnitType.NOoSE) < 1)
                        {
                            ShouldDispatch = true;
                            UnitType = CopUnitType.NOoSE;
                        }
                    }

                    if (ShouldDispatch)
                    {
                        Vector3 pos = Info.GetSpawnpointFor(UnitType, Suspect.Criminal.Position);
                        CopsChasing.Add(new CopUnitHandler(UnitType, Suspect, pos, true));
                        break;
                    }
                }
            }
        }


        //Make sure all criminals have at least one cop chasing them
        void HandleCopDistribution()
        {
            foreach (SuspectHandler criminal in CriminalsActive)
            {
                criminal.CopsChasingMe.Clear();
                int copsChasingIt = 0;

                //If player is chasing criminal
                if (IsPlayerChasingSuspect(criminal)) copsChasingIt++;

                foreach (CopUnitHandler unit in CopsChasing)
                {
                    if (unit.Suspect.Criminal.Handle == criminal.Criminal.Handle)
                    {
                        copsChasingIt++;
                        criminal.CopsChasingMe.Add(unit);
                    }
                }

                if (copsChasingIt == 0 && !criminal.Surrendered() && CopsChasing.Count >= CriminalsActive.Count)
                {
                    //UI.Notify("A cop has been relocated for that criminal");
                    CopUnitHandler relocated = GetClosestCop(criminal, true, true); // CopsChasing[Util.RandomInt(0, CopsChasing.Count - 1)];
                    if (relocated != null)
                    {
                        relocated.Suspect = criminal;
                        relocated.Leader.Task.ClearAll();
                        //if (AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + relocated.CopVehicle.FriendlyName + " unit", "Chase Update", "I'm going after them.");
                    }
                    //if(relocated.CopVehicle.Model.IsHelicopter) Function.Call(Hash.TASK_HELI_CHASE, relocated.Leader, relocated.Suspect.Criminal, 20f, 20f, 10f);
                }
            }
        }
        Vector2 World3DToScreen2d(Vector3 pos)
        {
            var x2dp = new OutputArgument();
            var y2dp = new OutputArgument();

            Function.Call<bool>(Hash._WORLD3D_TO_SCREEN2D, pos.X, pos.Y, pos.Z, x2dp, y2dp);
            return new Vector2(x2dp.GetResult<float>(), y2dp.GetResult<float>());
        }

        void HandlePlayerEquipment()
        {
            if (Game.Player.Character.IsInPoliceVehicle && CriminalsActive.Count==0)
            {
                if (!Game.Player.Character.Weapons.HasWeapon(WeaponHash.StunGun)) Game.Player.Character.Weapons.Give(WeaponHash.StunGun, -1, false, true);
                if (Game.Player.Character.Health < Game.Player.Character.MaxHealth) Game.Player.Character.Health += 5;
                if (Game.Player.Character.Armor < 100) Game.Player.Character.Armor += 100;

            }
        }
        void SaveCar(Vehicle v)
        {


        }
        void OnTick(object sender, EventArgs e)
        {

         //   Vehicle dsdas = Util.GetVehicleInDirection(Game.Player.Character.CurrentVehicle, Game.Player.Character.CurrentVehicle.ForwardVector * -10);


           // UI.ShowSubtitle(Util.CanWeUse(dsdas).ToString());
            if (WasCheatStringJustEntered("even")) UI.ShowSubtitle("~r~"+Util.TerrainIsEven(Game.Player.Character.Position,float.Parse(Game.GetUserInput(2))).ToString());
            if (WasCheatStringJustEntered("sound"))
            {

                Vehicle v = Game.Player.Character.CurrentVehicle;

                if (Util.CanWeUse(v))
                {
                    string name = Game.GetUserInput(10);
                    Model modelname = name;
                    if(modelname.IsValid)
                    {
                        Function.Call<Vector3>(Hash._0x4F0C413926060B38, v, name);
                        UI.Notify("~b~" + name + " sound applied on " + v.FriendlyName + ".");
                    }
                    else
                    {
                        UI.Notify("~o~" + name + " is not a valid vehicle.");

                    }

                }
                else
                {
                    UI.Notify("~o~Do it in a car.");
                }
            }
            if (WasCheatStringJustEntered("di clear"))
            {
                UI.Notify("All cops and criminals clear.");
                //Clear criminals and cops;
                foreach (CopUnitHandler unit in CopsChasing) unit.ShouldRemoveCopUnit = true;
                foreach (SuspectHandler unit in CriminalsActive) unit.ShouldRemoveCriminal = true;
            }
            if (WasCheatStringJustEntered("check"))
            {
               Util.SetUpCopUnitLoadout(null, Game.Player.Character.Position);
            }
            ////File.AppendAllText(@"" + debugpath, "\n");

            ////File.AppendAllText(@"" + debugpath, "\n - ontick");

            //World.DrawSpotLightWithShadow(CopVehicle.Position + new Vector3(0, 0, -2), CopVehicle.Position - Suspect.Criminal.Position, System.Drawing.Color.Beige, 100f, 1f, 1f, 10f, 90f);
            //World.DrawSpotLightWithShadow(Game.Player.Character.Position + new Vector3(0, 0, 2), GameplayCamera.Direction, System.Drawing.Color.White, 100f, 1f, 1f, 10f, 90f);

            /*
            if (Game.IsControlJustPressed(2, GTA.Control.Context))
            {
                Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, Game.Player.Character, Game.Player.Character.CurrentVehicle, 8, 2000);

            }


                string flags="";

                for (int i = 140; i < 200; i++)
                {
                   if(Util.IsSubttaskActive(Game.Player.Character, (Util.Subtask)i)) flags += " "+i.ToString();
                }
            Util.DisplayHelpTextThisFrame(flags);
                        */

            if (Game.Player.WantedLevel == 0)
            {



                foreach (Vector3 pos in Info.AllPoliceStations)
                {
                    if (Game.Player.Character.Position.DistanceTo(pos) < 20f && Game.Player.Character.Velocity.Length() < 0.4f)
                    {
                        if (PlayerOnDuty)
                        {
                            if (PlayerOnDutyRef < Game.GameTime)
                            {
                                Util.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to go off Duty.");
                                if (Game.IsControlJustPressed(2, GTA.Control.Context))
                                {
                                    PlayerOnDuty = false;
                                    Game.Player.Character.RelationshipGroup = Game.GenerateHash("PLAYER");
                                    Util.AddQueuedHelpText("You're now ~b~Off duty~w~.");

                                    //Clear criminals and cops;
                                    foreach (CopUnitHandler unit in CopsChasing) unit.ShouldRemoveCopUnit = true;
                                    foreach (SuspectHandler unit in CriminalsActive) unit.ShouldRemoveCriminal = true;

                                }
                            }
                        }
                        else
                        {
                            Util.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to go on Duty.");
                            if (Game.IsControlJustPressed(2, GTA.Control.Context))
                            {
                                foreach (Vehicle veh in World.GetNearbyVehicles(Game.Player.Character, 70))
                                {
                                    if (Util.IsPoliceVehicle(veh)) veh.LockStatus = VehicleLockStatus.Unlocked;
                                }
                                PlayerOnDuty = true;
                                Game.Player.Character.RelationshipGroup = CopsRLGroup;
                                PlayerOnDutyRef = Game.GameTime + 30000;

                                Util.AddQueuedHelpText("You're now ~b~On duty~w~. Stand still or enter a vehicle to get Callouts.");

                                if (HelpNotifications.Checked)
                                {
                                    Util.AddQueuedHelpText("You'll start being notified of ongoing pursuits.");
                                    Util.AddQueuedHelpText("Accept them by pressing [ENTER] or decline by waiting for the notification to dissapear.");
                                    Util.AddQueuedHelpText("[" + MainMenuKey.ToString() + "] Will open the Settings/Backup menu, [" + ForceCalloutKey.ToString() + "] will force a new Callout.");

                                }
                            }
                            }
                        }
                    }
                


                if (!Info.IsPlayerOnDuty())
                {
                    if (!Game.Player.Character.IsOnFoot)
                    {
                        Vehicle playercar = Game.Player.Character.CurrentVehicle;
                        if (Util.CanWeUse(playercar) && Util.IsPoliceVehicle(playercar) && playercar.IsStopped)
                        {
                            Util.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to go on Duty.");
                            if (Game.IsControlJustPressed(2, GTA.Control.Context))
                            {
                                PlayerOnDuty = true;
                                Game.Player.Character.RelationshipGroup = CopsRLGroup;
                                PlayerOnDutyRef = Game.GameTime + 30000;

                                Util.AddQueuedHelpText("You're now ~b~On duty~w~. Stand still or enter a vehicle to get Callouts.");
                                if (HelpNotifications.Checked)
                                {
                                    Util.AddQueuedHelpText("You'll start being notified of ongoing pursuits.");
                                    Util.AddQueuedHelpText("Accept them by pressing [ENTER] or decline by waiting for the notification to dissapear.");
                                    Util.AddQueuedHelpText("[" + MainMenuKey.ToString() + "] Will open the Settings/Backup menu, [" + ForceCalloutKey.ToString() + "] will force a new Callout.");
                                }
                            }
                        }
                    }
                }
            }
            


            if (CloseRoads.Checked && CriminalsActive.Count == 0) CloseRoads.Checked = false;

            if (CloseRoads.Checked)
            {
                Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.2f);
                Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.2f);
                //Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
            }

            if (_menuPool.IsAnyMenuOpen() && CriminalsActive.Count>0)
            {
                Function.Call(Hash._START_SCREEN_EFFECT, "SwitchHUDOut", 0, true);
                Function.Call(Hash.SET_GAME_PAUSED, true);
            }
            else
            {
                Function.Call(Hash.SET_GAME_PAUSED, false);
                if (Function.Call<int>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, "SwitchHUDOut") != 0) Function.Call(Hash._STOP_ALL_SCREEN_EFFECTS);
            }

            if (Game.GameTime > AutoDispatchRefTime)
            {
                AutoDispatchRefTime = Game.GameTime + 10000;
                if (AllowAutomaticDispatch.Checked) HandleAutomaticCopDispatch();
            }

            if (Game.GameTime > TickRefTime)
            {
                //File.AppendAllText(@"" + debugpath, "\n - TickSecond");

                TickRefTime = Game.GameTime + 1000;




                if (AutomaticCallouts.Checked && CalloutNotification == -1 && CriminalsActive.Count == 0 && Info.IsPlayerOnDuty() && Game.Player.Character.IsStopped && !_menuPool.IsAnyMenuOpen() && Util.RandomInt(0, 10) < 2) GenerateCallout();
                if (Info.IsPlayerOnDuty())
                {
                    Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
                    if (Game.Player.WantedLevel > 0) Game.Player.WantedLevel = 0;
                }
                else Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);

               if(HelpNotifications.Checked) HandleScriptReminders();
                HandlePlayerEquipment();
                HandleCopDistribution();
                if (UnitsPatrolling < 3 && Util.RandomInt(0, 10) < 3)
                {
                    UnitsPatrolling++;
                    if (AllowChaseNotifications.Checked && UnitsPatrolling == 1) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "NEAR UNITS AVAILABLE", "There are units in standby near your position.");
                }


                CriminalsFleeing.Clear();
                CriminalsAlone.Clear();
                CriminalsSurrendered.Clear();
                foreach (SuspectHandler Suspect in CriminalsActive)
                {
                    if (Suspect.Surrendered()) CriminalsSurrendered.Add(Suspect); else CriminalsFleeing.Add(Suspect);
                    if (Suspect.CopsChasingMe.Count == 0) CriminalsAlone.Add(Suspect);
                }



                if (SplitCriminals.Count > 0)
                {
                    if (Util.CanWeUse(SplitCriminals[0]))
                    {
                        if (SplitCriminals[0].IsAlive)
                        {
                            CriminalsActive.Add(new SuspectHandler(SplitCriminalKind, Vector3.Zero, SplitCriminals[0]));
                            SplitCriminals.RemoveAt(0);
                        }
                        else
                        {
                            SplitCriminals[0].MarkAsNoLongerNeeded();
                        }
                    }
                }


                Util.HandleMessages();
                Util.HandleNotifications();


            }


            HandleSuspectSelectionMenu();

            _menuPool.ProcessMenus();
            if (Info.IsPlayerOnDuty() && Game.IsKeyPressed(MainMenuKey) && !_menuPool.IsAnyMenuOpen())
            {

                if (CriminalsActive.Count > 0)
                {

                    mainMenu.Visible = !mainMenu.Visible;
                }
                else
                {
                    SettingsMenu.Visible = !SettingsMenu.Visible;
                }
                //mainMenu.RefreshIndex();

            }



            //if (Game.Player.Character.Weapons.Current.Hash == WeaponHash.StunGun) Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, -900f);        else Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, 0f);
            //Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, -900f);




            string states = "";

            // for (int i = 0; i <= CopsChasing.Count-1; i++)
            //{
            //CopUnitHandler unit = CopsChasing[i];

            foreach (CopUnitHandler unit in CopsChasing)
            {
                if (DebugNotifications.Checked)
                {
                    Vector2 screeninfo = World3DToScreen2d(unit.Leader.Position + new Vector3(0, 0, 1.5f));
                    Function.Call(Hash._SET_TEXT_ENTRY, "STRING");
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_COLOUR, 0, 100, 0, 255);
                    Function.Call(Hash.SET_TEXT_SCALE, 1f, 0.2f);
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, unit.State.ToString());
                    Function.Call(Hash._DRAW_TEXT, screeninfo.X, screeninfo.Y);
                }
                states += unit.State.ToString() + ",";
                if (unit.ShouldRemoveCopUnit)
                {
                    unit.Clear();
                    CopsToRemove.Add(unit);
                }
                else
                {
                    //File.AppendAllText(@"" + debugpath, "\n - UpdateTick " + unit.UnitType.ToString());
                    unit.UpdateOnTick();
                    //Script.Wait(0);
                    if (Game.GameTime > unit.RefTime + 1000)
                    {
                        unit.RefTime = Game.GameTime;
                        //File.AppendAllText(@"" + debugpath, "\n - Update " + unit.UnitType.ToString());
                        unit.Update();
                      //  Script.Wait(0);
                    }
                }
            }
            foreach (CopUnitHandler toremove in CopsToRemove) CopsChasing.Remove(toremove);

            states += "~n~";

            if (TickRefTimeLong < Game.GameTime)
            {
                TickRefTimeLong = Game.GameTime + 5000;


                //Look for cars to steal
                List<SuspectHandler> Foot = new List<SuspectHandler>();
                foreach (SuspectHandler suspectfoot in CriminalsActive)
                {
                    if (suspectfoot.State == CriminalState.Fleeing_Foot)
                    {
                        suspectfoot.VehiclesConsidered.Clear();
                        Foot.Add(suspectfoot);
                    }
                }

                if (Foot.Count > 0)
                {
                    foreach (Vehicle v in World.GetAllVehicles())
                    {
                        foreach (SuspectHandler suspectonfoot in Foot)
                        {
                            if (suspectonfoot.Criminal.IsInRangeOf(v.Position, 100f)) suspectonfoot.VehiclesConsidered.Add(v);
                        }
                    }
                }

                //Ambient criminals dynamically added
                if (Info.IsPlayerOnDuty() && AmbientPedsCanBeCriminals.Checked)
                {
                    foreach (Ped ped in World.GetNearbyPeds(Game.Player.Character, 50f))
                    {
                        if (ped.IsOnFoot && !Util.IsCop(ped) && !ped.IsPersistent && Util.IsSubttaskActive(ped, Util.Subtask.AIMED_SHOOTING_ON_FOOT))
                        {
                            CriminalsActive.Add(new SuspectHandler(CriminalType.Dynamic, ped.Position, ped));
                        }
                    }
                }
            }

            foreach (SuspectHandler Suspect in CriminalsActive)
            {
                if (DebugNotifications.Checked)
                {
                    string BeingArrested = "";
                    if (Suspect.CopArrestingMe != null) BeingArrested = "Being Arrested";
                    Vector2 screeninfo = World3DToScreen2d(Suspect.Criminal.Position + new Vector3(0, 0, 1.5f));
                    Function.Call(Hash._SET_TEXT_ENTRY, "STRING");
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_COLOUR, 100, 100, 0, 255);
                    Function.Call(Hash.SET_TEXT_SCALE, 1f, 0.2f);
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, Suspect.State.ToString() + "~n~" + BeingArrested);
                    Function.Call(Hash._DRAW_TEXT, screeninfo.X, screeninfo.Y);
                }
                states += Suspect.State.ToString() + "-";

                //File.AppendAllText(@"" + debugpath, "\n - UpdateTick Criminal");
                Suspect.UpdateFast();
                if (Game.GameTime > Suspect.RefTime + 500)
                {
                    if (Suspect.LOSTreshold == LostLOSThreshold)
                    {
                        if (AllowChaseNotifications.Checked)
                        {
                            if (Suspect.CopsChasingMe.Count > 0)
                            {
                                CopUnitHandler Unit = GetClosestCop(Suspect, true, true);//Suspect.CopsChasingMe[Util.RandomInt(0, Suspect.CopsChasingMe.Count - 1)];
                                if (Unit != null)
                                {
                                    Util.AddNotification("web_lossantospolicedept", "~b~" + Unit.CopVehicle.FriendlyName, "SUSPECT SIGHT LOST", "We have lost sight of the suspect.");
                                }
                            }
                        }
                    }
                    if(Suspect.LOSTreshold == LostLOSThreshold + 20)
                    {
                        if (AllowChaseNotifications.Checked)
                        {
                            if (Suspect.CopsChasingMe.Count > 0)
                            {
                                CopUnitHandler Unit = GetClosestCop(Suspect, true, true);//Suspect.CopsChasingMe[Util.RandomInt(0, Suspect.CopsChasingMe.Count - 1)];
                                if (Unit != null)
                                {
                                    Util.AddNotification("web_lossantospolicedept", "~b~" + Unit.CopVehicle.FriendlyName, "SUSPECT LAST SEEN", "Last Seen: ~y~"+World.GetStreetName(Suspect.LostLOSBlip.Position)+"~w~~n~Headed: ~b~"+Util.GetWhereIsHeaded(Suspect.Criminal,false));
                                }
                            }
                        }
                    }

                    if (Suspect.LOSTreshold > 100)
                    {
                        Suspect.Clear();
                        CriminalsToRemove.Add(Suspect);
                        if (CriminalsFleeing.Count > 1)
                        {
                            Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT LOST", "A suspect has evaded all pursuing units. There are " + CriminalsFleeing.Count.ToString() + " more suspects on the loose.");
                        }
                        else if(CriminalsActive.Count == 1)
                        {
                            Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT LOST", "Suspect has evaded all pursuing units.~n~All units, cease pursuit and return to patrol.");
                        }
                    }
                    else
                    if (Suspect.ShouldRemoveCriminal)
                    {
                        if (Suspect.Criminal.IsAlive)
                        {
                            Game.Player.Money += 500;
                            if ((CriminalsFleeing.Count + Suspect.Partners.Count) < 3)
                            {
                                if ((CriminalsFleeing.Count > 0 || Suspect.Partners.Count > 0)) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT APREHENDED", "One of the suspects has been ~g~arrested.~w~~n~There are " + (CriminalsFleeing.Count  + Suspect.Partners.Count).ToString() + " suspects still on the loose.");
                                else Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT APREHENDED", "~g~All suspects have been arrested~w~~n~Good job, everyone.");
                            }
                            Suspect.Clear();
                            CriminalsToRemove.Add(Suspect);
                        }
                        else
                        {

                         if(!Util.DecorExistsOn("HandledByCoroner", Suspect.Criminal)) Util.SetDecorInt("HandledByCoroner", Suspect.Criminal, 0);

                            Suspect.Clear();
                            CriminalsToRemove.Add(Suspect);
                            if ((CriminalsFleeing.Count + Suspect.Partners.Count) < 4)
                            {
                                if ((CriminalsFleeing.Count > 1 || Suspect.Partners.Count > 0)) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT DEAD", "One of the suspects ~r~is down.~w~~n~There are " + (CriminalsFleeing.Count + Suspect.Partners.Count).ToString() + " suspects still on the loose.");
                                else Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "LAST SUSPECT DEAD", "~g~All suspects have been taken care of.~w~~n~All units, return to patrol.");
                            }
                        }
                    }
                    else
                    {
                        //File.AppendAllText(@"" + debugpath, "\n - Update Criminal");
                        Suspect.Update();
                        Suspect.RefTime = Game.GameTime;
                    }
                }
            }
            foreach (SuspectHandler toremove in CriminalsToRemove) CriminalsActive.Remove(toremove);

            if (DebugNotifications.Checked) Util.DisplayHelpTextThisFrame(states);

            if (Game.GameTime > CalloutNotificationTimeout && CalloutNotificationActive()) CleanCalloutNotification();

        }
        void OnKeyDown(object sender, KeyEventArgs e)
        {

        }

        public static bool WasCheatStringJustEntered(string cheat)
        {
            return Function.Call<bool>(Hash._0x557E43C447E700A8, Game.GenerateHash(cheat));
        }


        void OnKeyUp(object sender, KeyEventArgs e)
        {

            if (_menuPool.IsAnyMenuOpen())  SaveSettings(ConfigFilename);

            if (e.KeyCode == Keys.Delete && DebugNotifications.Checked)
            {
                foreach (CopUnitHandler unit in CopsChasing) unit.ShouldRemoveCopUnit = true;
                foreach (SuspectHandler unit in CriminalsActive) unit.ShouldRemoveCriminal = true;
            }
            if (e.KeyCode == Keys.OemMinus && DebugNotifications.Checked)
            {
                CriminalsActive.Add(new SuspectHandler(CriminalType.Test, Game.Player.Character.Position.Around(10), null));
            }

            if (e.KeyCode == Keys.Space && DebugNotifications.Checked)
            {

                if (Game.Player.Character.Velocity.Length()==0 && CriminalsActive.Count > 0)
                {

                    Vehicle veh = Game.Player.Character.CurrentVehicle;
                    if (Util.CanWeUse(veh))
                    {
                        veh.Position = CriminalsActive[0].Criminal.Position + (CriminalsActive[0].Criminal.ForwardVector * -7);
                        veh.Heading = CriminalsActive[0].Criminal.Heading;
                        veh.Velocity = CriminalsActive[0].Criminal.Velocity;
                        Game.FadeScreenOut(0);
                        Game.FadeScreenIn(1000);
                    }
                }


            }

            if (CalloutNotificationActive() && e.KeyCode == Keys.Enter)
            {
                CriminalsActive.Add(new SuspectHandler(CalloutCriminal, CalloutPlace, null));
                CleanCalloutNotification();

                if (CalloutCriminal == CriminalType.ViolentGangFamilies)
                {
                    CriminalsActive.Add(new SuspectHandler(CriminalType.ViolentGangBallas, CalloutPlace.Around(8f), null));
                }



                if (CalloutCriminal == CriminalType.AmateurRacers || CalloutCriminal == CriminalType.ProRacers)
                {
                    for (int i = 0; i <= Util.RandomInt(0, 3); i++)
                    {
                        CriminalsActive.Add(new SuspectHandler(CalloutCriminal, CalloutPlace.Around(8f), null));
                    }
                }


                if (CalloutCriminal == CriminalType.MotorbikeHeisters)
                {
                    for (int i = 0; i <= Util.RandomInt(0, 4); i++)
                    {
                        CriminalsActive.Add(new SuspectHandler(CalloutCriminal, CalloutPlace.Around(8f), null));
                    }
                }


                if (PlayerCareMenu.Checked)
                {
                    Vehicle veh = Util.GetLastVehicle(Game.Player.Character);
                    if (Util.CanWeUse(veh))
                    {
                        Function.Call(Hash.SET_VEHICLE_FIXED,veh);
                        Function.Call(Hash.SET_VEHICLE_DEFORMATION_FIXED, veh);

                    }
                }
            };

            if(e.KeyCode == ForceCalloutKey)
            {
                if ((DebugNotifications.Checked || CriminalsActive.Count == 0) && Info.IsPlayerOnDuty()) if (CalloutNotificationActive()) { CleanCalloutNotification(); } else GenerateCallout();

                foreach(SuspectHandler criminal in CriminalsActive)
                {
                    if (criminal.LOSTreshold < 10 && Game.Player.Character.Position.DistanceTo(criminal.Criminal.Position) < 200f)
                    {
                        //Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "LAST SUSPECT DEAD", "~g~All suspect have been taken care of.~w~~n~All units, return to patrol.");
                        string text ="";
                        if(criminal.Auth_DeadlyForce || criminal.Auth_Ramming)
                        {
                            text += "Auth: ";
                            if (criminal.Auth_Ramming) text += PIT;
                            if (criminal.Auth_Ramming) text += " - " + DeadlyForce + "~n~";
                        }

                        text += Util.GetImmersiveCriminalStatus(criminal);
                        Util.Notify("web_lossantospolicedept", "~b~DISPATCH", "REMINDER", text);
                    }
                }
            }
        }

        bool CalloutNotificationActive()
        {
            return CalloutNotification != -1;
        }

        CriminalType CalloutCriminal = CriminalType.PacificFleeingFoot;
        Vector3 CalloutPlace = Vector3.Zero;
        public void GenerateCallout()
        {
            Util.CleanNotifications();
            ManageCalloutPool();
            if (CalloutPool.Count > 0)
            {
                if (CalloutNotification == -1)
                {
                    CalloutCriminal = (CriminalType)CalloutPool[Util.RandomInt(0, CalloutPool.Count - 1)]; // (CriminalType)Util.RandomInt(0,Enum.GetValues(typeof(CriminalType)).Length-1);
                    CalloutPlace = Util.GenerateSpawnPos(World.GetNextPositionOnStreet(Game.Player.Character.Position).Around(Util.RandomInt(300, 500)), Util.Nodetype.AnyRoad, true); //Util.FindSpawnpointInDirection(Game.Player.Character.Position.Around(200f), 300f, 30);
                    while (CalloutPlace.DistanceTo(Game.Player.Character.Position) < 300 || CalloutPlace == Vector3.Zero)
                    {
                        Script.Wait(100);
                        CalloutPlace = Util.GenerateSpawnPos(World.GetNextPositionOnStreet(Game.Player.Character.Position).Around(Util.RandomInt(300, 500)), Util.Nodetype.AnyRoad, true); //Util.FindSpawnpointInDirection(Game.Player.Character.Position.Around(200f), 300f, 30);
                    }

                    if (CalloutPlace == Vector3.Zero)
                    {
                        if (DebugNotifications.Checked) Util.WarnPlayer(ScriptName, "~R~ERROR", "There was an GenerateSpawnpoint error.");
                        CalloutPlace = World.GetNextPositionOnStreet(Game.Player.Character.Position).Around(400f);
                    }
                    NotifyCallout("web_lossantospolicedept", "~b~DISPATCH", "SUSPECT FLEEING", "Callout: ~b~" + GenerateContextForSuspect(CalloutCriminal) + "~w~~n~Reported in: ~y~" + World.GetZoneName(CalloutPlace) + "~w~.");
                }
                else
                {
                    CleanCalloutNotification();
                }
            }
            else
            {
                UI.Notify("There are no callouts to in the Callout pool.");
            }
        }

        void CleanCalloutNotification()
        {
            //UI.Notify("Callout removed");
            Function.Call(Hash._REMOVE_NOTIFICATION, CalloutNotification);
            CalloutNotification = -1;

        }


        public static int CalloutNotificationTimeout;
        public static int CalloutNotification = -1;
        public static void NotifyCallout(string avatar, string author, string title, string message)
        {
            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            //Function.Call(Hash._0x17430B918701C342, 200,200,255,0);
            CalloutNotification = Function.Call<int>(Hash._SET_NOTIFICATION_MESSAGE, avatar, avatar, true, 0, title, author);
            CalloutNotificationTimeout = Game.GameTime + 10000;
        }


        protected override void Dispose(bool dispose)
        {
            Game.Player.Character.RelationshipGroup = Game.GenerateHash("PLAYER");

            foreach (CopUnitHandler unit in CopsChasing)
            {
                unit.Leader.MarkAsNoLongerNeeded();
                foreach (Ped ped in unit.Partners) ped.MarkAsNoLongerNeeded();

            }
            foreach (SuspectHandler unit in CriminalsActive)
            {
                unit.Criminal.MarkAsNoLongerNeeded();
                foreach (Ped ped in unit.Partners) ped.MarkAsNoLongerNeeded();
            }

            foreach (Blip blip in StationBlips) blip.Remove();
            StationBlips.Clear();
        }
        void SaveCriminalVehicle(string file, string section, Vehicle veh)
        {
            if (File.Exists(@"" + file))
            {

                XmlDocument originalXml = new XmlDocument();
                originalXml.Load(@"" + file);

                XmlNode changes = originalXml.SelectSingleNode("//" + section);


                //Prevent duplications;
                foreach (XmlElement element in originalXml.SelectNodes("//" + section+"/*"))
                {
                    if (element.InnerText == veh.Model.Hash.ToString())
                    {
                        Util.WarnPlayer(ScriptName + " " + ScriptVer, "MODEL ALREADY EXISTED", "This vehicle model already exists in this category.");
                        return;
                    }
                }

                XmlElement Info = originalXml.CreateElement("Model");
                Info.InnerText = veh.Model.Hash.ToString();
                changes.AppendChild(Info);



                XmlAttribute Version = originalXml.CreateAttribute("Name");
                if (veh.FriendlyName.Length > 0)
                {
                    Version.Value = veh.FriendlyName;
                }
                else
                {
                    Version.Value = veh.DisplayName;
                }
                Info.Attributes.Append(Version);
                
                /*
                Vector3 pos = Game.Player.Character.Position + (Game.Player.Character.ForwardVector * 10);
                Vehicle test = World.CreateVehicle(int.Parse(Info.InnerText), pos);            
                test.IsPersistent = false; */


                originalXml.Save(@"" + file);
                Util.WarnPlayer(ScriptName + " " + ScriptVer, "MODEL SAVED", "Model saved.");

            }
        }
        void GenerateConfigFile(string file)
        {
            File.WriteAllText(@"" + file, "<Data></Data>");

            XmlDocument originalXml = new XmlDocument();
            originalXml.Load(@"" + file);

            XmlNode changes = originalXml.SelectSingleNode("//Data");

            
            XmlElement Info = originalXml.CreateElement("AllowAverageCallouts");
            Info.InnerText = AllowAverageCallouts.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowRaceCallouts");
            Info.InnerText = AllowRaceCallouts.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowHeistCallouts");
            Info.InnerText = AllowHeistCallouts.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowGangRiot");
            Info.InnerText = AllowGangRiot.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowMilitary");
            Info.InnerText = AllowMilitary.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowTerrorism");
            Info.InnerText = AllowTerrorism.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowCargoStealers");
            Info.InnerText = AllowCargoStealers.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowEscapedPrisoner");
            Info.InnerText = AllowEscapedPrisoner.Checked.ToString();
            changes.AppendChild(Info);


            Info = originalXml.CreateElement("AllowMainCharacters");
            Info.InnerText = AllowMainCharacters.Checked.ToString();
            changes.AppendChild(Info);


            Info = originalXml.CreateElement("DebugNotifications");
            Info.InnerText = DebugNotifications.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowAutomaticDispatch");
            Info.InnerText = AllowAutomaticDispatch.Checked.ToString();
            changes.AppendChild(Info);


            Info = originalXml.CreateElement("AllowOngoingChase");
            Info.InnerText = AllowOngoingChase.Checked.ToString();
            changes.AppendChild(Info);


            Info = originalXml.CreateElement("AllowRDEVehicles");
            Info.InnerText = AllowRDEVehicles.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowMadMaxVehicles");
            Info.InnerText = AllowMadMaxVehicles.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AllowIVPack");
            Info.InnerText = AllowIVPack.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("ForceUse");
            Info.InnerText = ForceUse.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("MainMenuKey");
            Info.InnerText = MainMenuKey.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("ForceCalloutKey");
            Info.InnerText = ForceCalloutKey.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("PlayerCareMenu");
            Info.InnerText = PlayerCareMenu.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("HelpNotifications");
            Info.InnerText = HelpNotifications.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AmbientPedsCanBeCriminals");
            Info.InnerText = AmbientPedsCanBeCriminals.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("AutomaticCallouts");
            Info.InnerText = AutomaticCallouts.Checked.ToString();
            changes.AppendChild(Info);

            Info = originalXml.CreateElement("RealisticPosDispatch");
            Info.InnerText = RealisticPosDispatch.Checked.ToString();
            changes.AppendChild(Info);

            //Version saving
            XmlAttribute Version = originalXml.CreateAttribute("version");
            Version.Value = ScriptVer;
            changes.Attributes.Append(Version);
                        
            originalXml.Save(@"" + file);
        }
        void SaveSettings(string file)
        {
            if (!File.Exists(@"" + file))
            {
                GenerateConfigFile(ConfigFilename);
            }

            if (File.Exists(@"" + file))
            {
                XmlDocument ScriptInfo = new XmlDocument();
                ScriptInfo.Load(@"" + file);
                XmlElement root = ScriptInfo.DocumentElement;


                //Callout filters
                root.SelectSingleNode("//AllowAverageCallouts").InnerText = AllowAverageCallouts.Checked.ToString();
                root.SelectSingleNode("//AllowRaceCallouts").InnerText = AllowRaceCallouts.Checked.ToString();

                root.SelectSingleNode("//AllowHeistCallouts").InnerText = AllowHeistCallouts.Checked.ToString();
                root.SelectSingleNode("//AllowGangRiot").InnerText = AllowGangRiot.Checked.ToString();
                root.SelectSingleNode("//AllowMilitary").InnerText = AllowMilitary.Checked.ToString();
                root.SelectSingleNode("//AllowTerrorism").InnerText = AllowTerrorism.Checked.ToString();
                root.SelectSingleNode("//AllowCargoStealers").InnerText = AllowCargoStealers.Checked.ToString();
                root.SelectSingleNode("//AllowEscapedPrisoner").InnerText = AllowEscapedPrisoner.Checked.ToString();
                root.SelectSingleNode("//AllowMainCharacters").InnerText = AllowMainCharacters.Checked.ToString();


                //Config
                root.SelectSingleNode("//AllowRDEVehicles").InnerText = AllowRDEVehicles.Checked.ToString();
                root.SelectSingleNode("//AllowMadMaxVehicles").InnerText = AllowMadMaxVehicles.Checked.ToString();
                root.SelectSingleNode("//AllowIVPack").InnerText = AllowIVPack.Checked.ToString();

                root.SelectSingleNode("//DebugNotifications").InnerText = DebugNotifications.Checked.ToString();
                root.SelectSingleNode("//AllowAutomaticDispatch").InnerText = AllowAutomaticDispatch.Checked.ToString();
                root.SelectSingleNode("//AllowOngoingChase").InnerText = AllowOngoingChase.Checked.ToString();
                root.SelectSingleNode("//ForceUse").InnerText = ForceUse.Checked.ToString();
                root.SelectSingleNode("//PlayerCareMenu").InnerText = PlayerCareMenu.Checked.ToString();
                root.SelectSingleNode("//HelpNotifications").InnerText = HelpNotifications.Checked.ToString();
                root.SelectSingleNode("//AutomaticCallouts").InnerText = AutomaticCallouts.Checked.ToString();
                root.SelectSingleNode("//AmbientPedsCanBeCriminals").InnerText = AmbientPedsCanBeCriminals.Checked.ToString();
                root.SelectSingleNode("//RealisticPosDispatch").InnerText = RealisticPosDispatch.Checked.ToString();

                

                root.SelectSingleNode("//MainMenuKey").InnerText = MainMenuKey.ToString();
                root.SelectSingleNode("//ForceCalloutKey").InnerText = ForceCalloutKey.ToString();

                ScriptInfo.Save(@"" + file);
            }
            else
            {
                Util.WarnPlayer(ScriptName + " " + ScriptVer, "CONFIG NOT FOUND", "Config file for " + ScriptName + " not found. Cannot save config.");
            }
        }
        void LoadSettings(string file)
        {
            if (!File.Exists(@"" + file))
            {
                GenerateConfigFile(ConfigFilename);
            }

            if (File.Exists(@"" + file))
            {
                XmlDocument ScriptInfo = new XmlDocument();
                ScriptInfo.Load(@"" + file);
                XmlElement root = ScriptInfo.DocumentElement;

                if (!root.HasAttribute("version") || root.GetAttribute("version") != ScriptVer)
                {
                    Util.WarnPlayer(ScriptName + " " + ScriptVer, "OUTDATED CONFIG", "The configuration file is outdated and has been re-generated to avoid crashes.");
                    Util.WarnPlayer(ScriptName + " " + ScriptVer, "OUTDATED CONFIG", "~r~You have lost your current configuration in the process.");

                    File.Delete(@"" + file);
                    GenerateConfigFile(ConfigFilename);
                    ScriptInfo = new XmlDocument();

                    ScriptInfo.Load(@"" + file);
                    root = ScriptInfo.DocumentElement;
                }

                //Callout filters
                if(root.SelectSingleNode("//AllowAverageCallouts") != null) AllowAverageCallouts.Checked = bool.Parse(root.SelectSingleNode("//AllowAverageCallouts").InnerText);
                if (root.SelectSingleNode("//AllowRaceCallouts") != null) AllowRaceCallouts.Checked = bool.Parse(root.SelectSingleNode("//AllowRaceCallouts").InnerText);
                if (root.SelectSingleNode("//AllowHeistCallouts") != null) AllowHeistCallouts.Checked = bool.Parse(root.SelectSingleNode("//AllowHeistCallouts").InnerText);
                if (root.SelectSingleNode("//AllowGangRiot") != null) AllowGangRiot.Checked = bool.Parse(root.SelectSingleNode("//AllowGangRiot").InnerText);
                if (root.SelectSingleNode("//AllowMilitary") != null) AllowMilitary.Checked = bool.Parse(root.SelectSingleNode("//AllowMilitary").InnerText);
                if (root.SelectSingleNode("//AllowTerrorism") != null) AllowTerrorism.Checked = bool.Parse(root.SelectSingleNode("//AllowTerrorism").InnerText);
                if (root.SelectSingleNode("//AllowCargoStealers") != null) AllowCargoStealers.Checked = bool.Parse(root.SelectSingleNode("//AllowCargoStealers").InnerText);
                if (root.SelectSingleNode("//AllowEscapedPrisoner") != null) AllowEscapedPrisoner.Checked = bool.Parse(root.SelectSingleNode("//AllowEscapedPrisoner").InnerText);
                if (root.SelectSingleNode("//AllowMainCharacters") != null) AllowMainCharacters.Checked = bool.Parse(root.SelectSingleNode("//AllowMainCharacters").InnerText);
                if (root.SelectSingleNode("//ForceUse") != null) ForceUse.Checked = bool.Parse(root.SelectSingleNode("//ForceUse").InnerText);

                if (root.SelectSingleNode("//AllowRDEVehicles") != null) AllowRDEVehicles.Checked = bool.Parse(root.SelectSingleNode("//AllowRDEVehicles").InnerText);
                if (root.SelectSingleNode("//AllowIVPack") != null) AllowIVPack.Checked = bool.Parse(root.SelectSingleNode("//AllowIVPack").InnerText);
                if (root.SelectSingleNode("//AllowMadMaxVehicles") != null) AllowMadMaxVehicles.Checked = bool.Parse(root.SelectSingleNode("//AllowMadMaxVehicles").InnerText);


                //Config
                if (root.SelectSingleNode("//DebugNotifications") != null) DebugNotifications.Checked = bool.Parse(root.SelectSingleNode("//DebugNotifications").InnerText);
                if (root.SelectSingleNode("//AllowAutomaticDispatch") != null) AllowAutomaticDispatch.Checked = bool.Parse(root.SelectSingleNode("//AllowAutomaticDispatch").InnerText);
                if (root.SelectSingleNode("//AllowOngoingChase") != null) AllowOngoingChase.Checked = bool.Parse(root.SelectSingleNode("//AllowOngoingChase").InnerText);
                if (root.SelectSingleNode("//MainMenuKey") != null) MainMenuKey = (Keys)Enum.Parse(typeof(Keys), root.SelectSingleNode("//MainMenuKey").InnerText, true);
                if (root.SelectSingleNode("//ForceCalloutKey") != null) ForceCalloutKey = (Keys)Enum.Parse(typeof(Keys), root.SelectSingleNode("//ForceCalloutKey").InnerText, true);

                if (root.SelectSingleNode("//AutomaticCallouts") != null) AutomaticCallouts.Checked = bool.Parse(root.SelectSingleNode("//AutomaticCallouts").InnerText);

                if (root.SelectSingleNode("//AmbientPedsCanBeCriminals") != null) AmbientPedsCanBeCriminals.Checked = bool.Parse(root.SelectSingleNode("//AmbientPedsCanBeCriminals").InnerText);

                if (root.SelectSingleNode("//RealisticPosDispatch") != null) RealisticPosDispatch.Checked = bool.Parse(root.SelectSingleNode("//RealisticPosDispatch").InnerText);


                if (root.SelectSingleNode("//PlayerCareMenu") != null) PlayerCareMenu.Checked = bool.Parse(root.SelectSingleNode("//PlayerCareMenu").InnerText);
                if (root.SelectSingleNode("//HelpNotifications") != null) HelpNotifications.Checked = bool.Parse(root.SelectSingleNode("//HelpNotifications").InnerText);
            }
            else
            {
                Util.WarnPlayer(ScriptName + " " + ScriptVer, "CONFIG NOT FOUND", "Config file for " + ScriptName + " not found. A new one will be generated. All features enabled by default.");
            }
        }
        public static bool IsPotentiallySliding(Vehicle veh, float threshold)
        {
            return Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_ROTATION_VELOCITY, veh, true).Z) > threshold;
        }

        public static void DrawLine(Vector3 from, Vector3 to)
        {
            Function.Call(Hash.DRAW_LINE, from.X, from.Y, from.Z, to.X, to.Y, to.Z, 255, 255, 0, 255);
        }
    }
}