using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;

namespace LSPDispatch
{

    public enum CriminalType
    {

        AggresiveThug, SmartThug,
        AmateurRobbers, ExperiencedRobbers,
        NormalHeisters, ProffessionalsHeisters, MotorbikeHeisters, CashTruckStealers,
        ViolentGang, ViolentBigGang,
        ViolentGangBallas, ViolentGangFamilies,
        AmateurRacers, ProRacers,

        //Michael, Trevor, Franklin,
        MainCharacters,

        PacificVehicleFleeing, NormalVehicleFleeing, AggresiveVehicleFleeing,
        PacificFleeingFoot, NormalFleeingFoot, AggresiveFleeingFoot,
        MilitaryStealers,
        CargoStealers, CommertialVanStealers,
        Test,
        Terrorists,
        EscapedPrisoner,

        Dynamic //for dynamically added peds

    } // Defines the kind of car, kind of weapons, Personality & Nº Of friends.


    public enum CriminalDriveStrategy { Highways, Sideroads, Offroad, OffroadToFarRoad };

    public enum CriminalState { FightingCops, Fleeing_Vehicle, Fleeing_Foot, Surrendering, Arrested, DealtWith }

    public enum CopUnitType {Bike, Patrol, PrisonerTransporter, AveragePolice, AirUnit, LocalNoose, NOoSE, InsurgentNoose, NOoSEAirUnit, Army, ArmyAirUnit }
    public enum CopState { ChaseFoot, ChaseVehicle, FightCriminals, Arrest, AwaitingOrders, TaseAttempt, SecureArrest }


    public enum CriminalFlags
    {
        SURRENDERS_IF_AIMED_AT, SURRENDERS_IF_FORCED_OUT_OF_VEHICLE, SURRENDERS_WHEN_RAGDOLL, SURRENDERS_WHEN_STUNNED, SURRENDERS_IF_HURT, SURRENDERS_IF_SURROUNDED, SURRENDERS_IF_SHIT_EXPLODES, SURRENDERS_IF_SHOT_AT_EASY, SURRENDERS_IF_SHOT_AT_HARD, //Define when will the suspect surrender
        CAN_STEAL_PARKED_VEHICLES, CAN_STEAL_OCCUPIED_VEHICLES, CAN_STEAL_POLICE_VEHICLES, //Define what kind of vehicles the Suspect can steal
        PREFERS_FAST_VEHICLES, PREFERS_HEAVY_VEHICLES, PREFERS_HEIST_VEHICLES, CAN_BRAKECHECK, CAN_RAM, CAN_DRIVEBY, //Define vehicle steal priority && driving abilities
        CUSTOM_VEHICLE_SPAWN_CASHTRUCKS, CUSTOM_VEHICLE_SPAWN_MILITARY, CUSTOM_VEHICLE_SPAWN_HEIST, //Define custom vehicle spawns (independent of vehicle steal preference)
        POOR_DRIVING_ABILITY, AVERAGE_DRIVING_ABILITY, GREAT_DRIVING_ABILITY, //Define how good is the suspect at driving
        PREFERS_OFFROAD, PREFERS_ROAD, PREFERS_ALLEYWAYS, //Define which kind of roads will the suspect use.
        CAREFUL_DRIVING, //Define the Driving Style (avoids/brakes for peds,cars,objects, etc).
        DYNAMIC_EVASION_BEHAVIOR, //Define if the criminal can use special fleeing AI.
        CAN_STANDOFF_CAUTIOUS, CAN_STANDOFF_AGGRESIVE, CAN_STANDOFF_ALWAYS, WILL_NOT_FLEE_ALWAYS_STANDOFF, HATES_EVERYONE, //Define when the Suspect can decide to fight the cops instead of fleeing (only applicable when on foot)
        USES_THUG_GUNS, USES_PRO_GUNS, USES_AVERAGE_GUNS, USES_HEAVY_GUNS, //Define which kind of guns the Suspect gets
        HAS_1_PARTNER, HAS_2_PARTNERS, HAS_3_PARTNERS, SPAWNS_SMALL_GANG, SPAWNS_BIG_GANG, //Define if the Suspect has partners, and how many
        CARES_FOR_FRIENDS_LOW, CARES_FOR_FRIENDS_HIGH, //Define if the suspect waits for partners (when fleeing or getting into a vehicle). If the partner gets too far away, it will be reconfigured as a standalone suspect
        HAS_BODY_ARMOR, //Defines if the suspect has body armor or not.
        CHEAT_HAS_LOTS_OF_AMMO, CHEAT_NO_RAGDOLL_WHEN_SHOT, CHEAT_CHANGES_VEHICLES_WHEN_HIDDEN, CHEAT_HIDES_ON_FOOT_WHEN_HIDDEN, CHEAT_CAN_EASILY_DISSAPEAR_WHEN_HIDDEN, CHEAT_DRIVEBY_STICKY_MINES, CHEAT_NITRO, CHEAT_EMP, //Define what kind of tricks can this suspect use.
        ONGOING_CHASE_PATROL, ONGOING_CHASE_AVERAGE, ONGOING_CHASE_AIRUNIT, ONGOING_CHASE_SWAT, //Define what kind of cops start off chasing the suspect, if any.
        IMPORTANT_VEHICLE_CASH, IMPORTANT_VEHICLE_IMPOUND, IMPORTANT_VEHICLE_DRUGS, //Defines if the vehicle is important to recover, and why.
    }

    public enum CopUnitFlags
    {
        LEAVES_IF_VEH_DAMAGED, LEAVES_IF_HURT, //Define when will the cop leave the pursuit.
        CAN_COMANDEER_VEHICLES, PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR,
        CAN_RAM, LEADER_CAN_USE_VEHICLE_WEAPONS, LEADER_CAN_DRIVEBY, PARTNERS_CAN_DRIVEBY,
        PARTNERS_BECOME_LEADER_IF_LEADER_DIES,
        AGGRESIVE_IN_STANDOFF,
        HAS_AVERAGE_BODY_ARMOR,
        HAS_HIGH_BODY_ARMOR,
        CANT_ARREST,
        ATTEMPTS_TASING,
    }


    public static class Info
    {


        //CriminalVehicles.xml
        public static List<dynamic> VehicleCategories = new List<dynamic> { "Average", "Race", "Robbery", "Heist", "Military", };


        //Gang models

        public static List<String> BallasModels = new List<String> { "g_f_y_ballas_01", "g_m_y_ballaeast_01", "g_m_y_ballaorig_01", "g_m_y_ballasout_01", };
        public static List<String> FamiliesModels = new List<String> { "mp_m_famdd_01", "g_f_y_families_01", "g_m_y_famca_01", "g_m_y_famdnf_01", "g_m_y_famfor_01" };
        public static List<String> LostModels = new List<String> { "g_m_y_lost_01", "g_m_y_lost_02", "g_m_y_lost_02", "g_f_y_lost_01", };
        public static List<String> RedneckModels = new List<String> { "a_m_m_hillbilly_01", "a_m_m_hillbilly_02" };
        public static List<String> PrisonerModels = new List<String> { "IG_rashcosvki", "s_m_y_PrisMuscl_01" };



        //Callout Names
        public static List<string> FootGangTitle = new List<string> { "Gang activity", "Gang shootout", "Homicide", };

        public static List<string> RacerTitle = new List<string> { "Illegal race", "Vehicles racing", "Reckless driver", "Possible Illegal race in progress" };

        public static List<string> NormalHeistTitle = new List<string> { "Bank robbery", "Units chasing possible Heist suspect", };
        public static List<string> CashTruckSteal = new List<string> { "Securicar Hijacking" };

        public static List<string> BigHeistTitle = new List<string> { "Bank robbery", "Units chasing possible Heist suspect", "Heavy vehicle fleeing", };

        public static List<string> GenericSmallCrime = new List<string> { "Shoplifting", "Mugging", };
        public static List<string> GenericMediumCrime = new List<string> { "Mugging", "Homicide", "Assault with a deadly weapon", };
        public static List<string> GenericDangerousCrime = new List<string> { "Homicide", "Assault with a deadly weapon", "Suspicious Person", };

        public static List<string> FootGenericSmallCrime = new List<string> { "Shoplifting", "Jaywalking", "Mugging", "Assault" };
        public static List<string> FootGenericMediumlCrime = new List<string> { "Robbery", "Attack on a civilian", "Shots fired", };
        public static List<string> FootGenericDangerousCrime = new List<string> { "Homicide", "Assault with a deadly weapon", };

        public static List<string> VehicleGenericSmallCrime = new List<string> { "Speeding", "Hit and Run", "Drunk driver", "Reckless driver", "Expired license" };
        public static List<string> VehicleGenericMediumCrime = new List<string> { "Grand Theft Auto", "Hit and Run", "Attack on a civilian", "Vehicle Hijacking", "Possible Drive-By", "Suspicious Vehicle", "Attack on an officer" };
        public static List<string> VehicleGenericDangerousCrime = new List<string> { "Pursuit in progresss" }; //NOT USED YET

        public static List<string> MilitaryVehicleTitle = new List<string> { "Stolen Military-Grade vehicle" };

        public static List<string> TerroristActivityTitle = new List<string> { "Terrorist activity", "Terrorism", "Unkown Explosion" };

        public static List<string> CargoStealersTitle = new List<string> { "Stolen cargo", };
        public static List<string> CommercialVanStealersTitle = new List<string> { "Stolen commercial vehicle", };

        public static List<string> EscapedPrisonerTitle = new List<string> { "Escaped prisoner", };


        public static List<string> DrugDealtitle = new List<string> { "Drug deal", };

        public static List<string> GenericWantedPersonTitle = new List<string> { "Wanted person spotted", "Distress call", "Homicide", "Assault with a deadly weapon", };


        //VEHICLE TYPES
        public static List<Model> AverageVehs = new List<Model>
    {
        VehicleHash.Asea,VehicleHash.Baller,VehicleHash.BJXL,VehicleHash.Blista,VehicleHash.Blista2,VehicleHash.BobcatXL,VehicleHash.Dilettante,VehicleHash.Rebel,VehicleHash.Dukes,
        VehicleHash.Baller,VehicleHash.Cavalcade,VehicleHash.Cavalcade2,VehicleHash.Chino,VehicleHash.Dominator,VehicleHash.Dubsta,VehicleHash.Dubsta3,
        VehicleHash.Enduro,VehicleHash.Felon2,VehicleHash.Fugitive,VehicleHash.Huntley,VehicleHash.Kuruma,VehicleHash.Granger,VehicleHash.Mesa
    };
        public static List<Model> SportVehs = new List<Model>
    {

        VehicleHash.Bullet,VehicleHash.Buffalo2,VehicleHash.Kuruma,VehicleHash.Banshee,VehicleHash.Buccaneer,
        VehicleHash.Sultan,VehicleHash.Infernus,VehicleHash.Adder,VehicleHash.Alpha,VehicleHash.Voltic,VehicleHash.Schafter2,
        VehicleHash.Furoregt,   VehicleHash.Futo,VehicleHash.RapidGT,VehicleHash.CogCabrio,VehicleHash.Exemplar,VehicleHash.F620,VehicleHash.Felon,
        VehicleHash.Oracle,VehicleHash.Sentinel,VehicleHash.Zion,VehicleHash.Buccaneer,  VehicleHash.Dominator  ,VehicleHash.Dukes,VehicleHash.Gauntlet,
            VehicleHash.Hotknife,VehicleHash.Nightshade,VehicleHash.Phoenix,VehicleHash.RatLoader2,VehicleHash.Ruiner,VehicleHash.SabreGT,VehicleHash.SabreGT2,
            VehicleHash.Stalion,VehicleHash.Vigero,VehicleHash.Tampa,VehicleHash.Warrener,


            VehicleHash.Carbonizzare,VehicleHash.Cheetah,VehicleHash.Coquette,VehicleHash.EntityXF,VehicleHash.F620,VehicleHash.Feltzer2,
            VehicleHash.Adder,VehicleHash.Banshee2,VehicleHash.Osiris,VehicleHash.Voltic,VehicleHash.SultanRS,VehicleHash.Vacca,VehicleHash.Mamba
    };

        /*
        public static List<Model> SuperVehs = new List<Model>
        {
        TO ADD IN FUTURE UPDATE
        };
        */
        public static List<Model> HeavyVehs = new List<Model>
    {
        VehicleHash.Rubble,VehicleHash.TipTruck,VehicleHash.Trash
    };
        public static List<Model> SUVs = new List<Model>
    {
        VehicleHash.Granger,VehicleHash.Gresley,VehicleHash.Burrito,VehicleHash.Baller2
    };

        public static List<Model> MilitaryGradeVehs = new List<Model>
    {
       VehicleHash.Rhino,VehicleHash.Technical,"insurgent", "insurgent2", "limo2"
    };

        public static List<Model> HeistVehs = new List<Model>
    {
        VehicleHash.Dukes2,VehicleHash.Kuruma2,VehicleHash.Schafter5,VehicleHash.Cognoscenti2,VehicleHash.Cog552,VehicleHash.Baller5, VehicleHash.Baller6,VehicleHash.Dubsta2,VehicleHash.Cavalcade2,VehicleHash.Dubsta3,VehicleHash.Insurgent2,
    };

        public static List<Model> StolenCashTrucks = new List<Model>
    {
        VehicleHash.Stockade,
    };

        public static List<Model> Truck = new List<Model>
    {
        VehicleHash.Packer,VehicleHash.Pounder,VehicleHash.Hauler,VehicleHash.Phantom,"phantom2",
    };
        public static List<Model> Cargo = new List<Model>
    {
        VehicleHash.Tanker,VehicleHash.ArmyTanker,VehicleHash.Tanker2,VehicleHash.TVTrailer,VehicleHash.Trailers,VehicleHash.Trailers2,VehicleHash.Trailers3
    };

        public static List<Model> CommercialVans = new List<Model>
    {
        VehicleHash.Taco,VehicleHash.Burrito,VehicleHash.Burrito2,VehicleHash.Burrito4,VehicleHash.Pony,VehicleHash.Pony2,VehicleHash.Rumpo,VehicleHash.Rumpo2,VehicleHash.Bison2,VehicleHash.Benson,VehicleHash.Mule,VehicleHash.Mule2

        };

        public static List<Model> Bikes = new List<Model>
    {
        VehicleHash.Sanchez
    };

        public static List<Model> HeistBikes = new List<Model>
    {
        VehicleHash.Akuma,VehicleHash.CarbonRS,VehicleHash.Vader,
    };


        public static List<string> HwyPatrol = new List<string> { "Olympic Fwy", "Del Perro Fwy", "Palomino Fwy", "Senora Fwy", "Great Ocean Hwy", "La Puerta Fwy", "Los Santos Freeway", "Elysian Fields Fwy" };
        public static List<string> NorthernWilderness = new List<string> { "PALFOR", "CMSW", "CANNY", "MTJOSE", "MTGORDO", "SANCHIA" };
        public static List<string> SouthernWilderness = new List<string> { "PALHIGH", "TATAMO" };
        public static List<string> Sheriff = new List<string> { "WINDF", "PALMPOW", "GREATC", "CHIL", "TONGVAH", "TONGVAV", "RGLEN", "BANHAMC", "CHU", "RICHM", "WVINE", "DAVIS", "RANCHO", "CYPRE", "EBURO", "PBLUFF" };
        public static List<string> PortAuthority = new List<string> { "ELYSIAN", "TERMINA", "ZP_ORT" }; //Not used yet
        public static List<string> PillboxHill = new List<string> { "PBOX" }; //Not used yet

        //MTCHIL, PALETO, SANDY


        public static void AddModelToList(Model modelo, List<Model> List)
        {
            if (modelo.IsValid && !List.Contains(modelo))
            {
                //if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(modelo +" added to list("+List.Count+")");
                List.Add(modelo);
            }
        }
        public static void RemoveModelFromList(Model Model, List<Model> List)
        {
            if (List.Contains(Model) && List.Count > 1)
            {
                List.Remove(Model);
            }
        }


        public static void AddVehicleToList(Model modelo, List<Model> List)
        {
            if (modelo.IsValid && !List.Contains(modelo))
            {

                //if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(modelo +" added to list("+List.Count+")");
                List.Add(modelo);
            }
        }
        public static void AddPedToList(Model modelo, List<Model> List)
        {
            if (modelo.IsValid && !List.Contains(modelo))
            {

                //if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(modelo +" added to list("+List.Count+")");
                List.Add(modelo);
            }
        }

        public static void RemoveVehicleFromLsit(Model Model, List<Model> List)
        {
            if (List.Contains(Model) && List.Count > 1)
            {
                List.Remove(Model);
            }
        }
        public static Vector3 GetSpawnpointFor(CopUnitType UnitType, Vector3 desiredArea)
        {
            Vector3 FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

            if (!DangerousIndividuals.RealisticPosDispatch.Checked)
            {
                
                return Util.GenerateSpawnPos(desiredArea.Around(100f), Util.Nodetype.Road, false); ;
            }

            switch (UnitType)
            {
                case CopUnitType.Patrol:
                    {
                        if (DangerousIndividuals.UnitsPatrolling > 0)
                        {
                            FinalPos = Util.GenerateSpawnPos(desiredArea.Around(300f), Util.Nodetype.Road, false);
                        }
                        else
                        {
                            FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);
                        }
                        break;
                    }
                case CopUnitType.PrisonerTransporter:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);
                        break;
                    }
                case CopUnitType.AveragePolice | CopUnitType.Bike:
                    {
                        if (DangerousIndividuals.UnitsPatrolling > 0)
                        {
                            FinalPos = Util.GenerateSpawnPos(desiredArea.Around(300), Util.Nodetype.Road, false);
                        }
                        else
                        {
                            FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);
                        }
                        break;
                    }
                case CopUnitType.AirUnit:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
                case CopUnitType.LocalNoose:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
                case CopUnitType.NOoSE:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
                case CopUnitType.NOoSEAirUnit:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
                case CopUnitType.Army:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
                case CopUnitType.ArmyAirUnit:
                    {
                        FinalPos = World.GetNextPositionOnStreet(Util.GetClosestLocation(desiredArea, AllPoliceStations)).Around(5f);

                        break;
                    }
            }

            return FinalPos;
        }

        //Police 
        public static List<Model> HighwayModels = new List<Model> { };
        public static List<Model> HighwayCars = new List<Model> { };


        public static List<Model> SWATModels = new List<Model> { };
        public static List<Model> LSPDModels = new List<Model> {  };

        public static List<Model> LSPDBikeModels = new List<Model> { "s_m_y_hwaycop_01", };

        public static List<Model> LSSDModels = new List<Model> { };
        public static List<Model> BCSOModels = new List<Model> { };
        public static List<Model> SAPRModels = new List<Model> { "s_m_y_ranger_01", "s_f_y_ranger_01", };
        public static List<Model> NYSTModels = new List<Model> { "s_m_m_snowcop_01", };

        public static List<Model> ArmyModels = new List<Model> { "s_m_y_marine_03", "s_m_y_marine_02", "s_m_y_marine_01", };

        public static List<Model> HeliModels = new List<Model> { };

        //POLICE 
        public static List<Model> LSPDBikes = new List<Model> { "POLICEB" }; //Los Santos

        public static List<Model> LSPDCars = new List<Model> { }; //Los Santos - "POLICE", "POLICE2", "POLICE3", 
        public static List<Model> LSPDLocalSWAT = new List<Model> { };
        public static List<Model> LSPDSWATCars = new List<Model> { };
        public static List<Model> LSPDHelis = new List<Model> { };


        public static List<Model> LSSDCars = new List<Model> { }; // Los Santos County
        public static List<Model> LSSDLocalSWAT = new List<Model> { };
        public static List<Model> LSSDSWATCars = new List<Model> {  };
        public static List<Model> LSSDHelis = new List<Model> { };


        public static List<Model> BCSOCars = new List<Model> {}; //Blaine County
        public static List<Model> BCSOLocalSWAT = new List<Model> {  };
        public static List<Model> BCSOSWATCars = new List<Model> {  };
        public static List<Model> BCSOHelis = new List<Model> {  };

        public static List<Model> SAPRCars = new List<Model> { "pranger", }; //Park Ranger
        public static List<Model> SAPRLocalSWAT = new List<Model> { "pranger", };
        public static List<Model> SAPRSWATCars = new List<Model> { "RIOT", };
        public static List<Model> SAPRHelis = new List<Model> { "polmav", };


        public static List<Model> NYSTCars = new List<Model> { "policeold", "policeold2", }; // North Yankton, not used
        public static List<Model> PortCars = new List<Model> { "POLICE", "POLICE2", "POLICE3", }; //Port Authority, not used

        public static List<Model> NOoSEHelis = new List<Model> { "annihilator", };

        public static List<Model> ArmoredNOoSE = new List<Model> { "nooseinsurgent", };
        public static List<Model> ArmoredNOoSESheriff = new List<Model> { "sheriffinsurgent", };
        public static List<Model> ArmoredNOoSEBSCO = new List<Model> { "sheriffinsurgent", };

        public static List<Model> ArmyCarsCity = new List<Model> { "insurgent", };
        public static List<Model> ArmyCarsSheriff = new List<Model> { "insurgent", };
        //public static List<string> ArmyCars = new List<string> { "insurgent", };


        public static List<Model> ArmyHelis = new List<Model> { "valkyrie", };

        //Police Stations
        public static List<Vector3> AllPoliceStations = new List<Vector3>
    {
        new Vector3(610,17,87), //Vinewood
        new Vector3(433,-986,30), // MissionRow
        new Vector3(-1093,-810,19), // Vespucci
        new Vector3(825,-1289,28), //La Mesa
        new Vector3(-440,6038,31), //Paleto Bay
        new Vector3(1855,3683,34), // Sandy Shores
    };

        public static List<Vector3> LSPDPoliceStations = new List<Vector3>
    {
        new Vector3(610,17,87), //Vinewood
        new Vector3(433,-986,30), // MissionRow
        new Vector3(-1093,-810,19), // Vespucci
        new Vector3(825,-1289,28), //La Mesa
    };
        public static List<Vector3> LSSDPoliceStations = new List<Vector3>
    {
        new Vector3(-440,6038,31), //Paleto Bay
        new Vector3(1855,3683,34), // Sandy Shores
    };


        
        public static bool IsPlayerOnDuty()
        {

            return DangerousIndividuals.PlayerOnDuty;
            //if ((Game.Player.Character.IsInPoliceVehicle || Util.IsCop(Game.Player.Character) || DangerousIndividuals.CriminalsActive.Count > 0) && Game.Player.WantedLevel == 0) return true;
            //return false;
        }
        public static string ClosestCopUnitName(SuspectHandler suspect)
        {
            string name = "";
            CopUnitHandler Unit = DangerousIndividuals.GetClosestCop(suspect, true, true);
            if (Unit != null)
            {
                name = Unit.CopVehicle.FriendlyName + " unit";
            }
            return name;
        }
        public static bool IsValidVehicleModel(string name)
        {
            Model VehModel = name;

            if (VehModel.IsValid) return true;
            else
            {
                int IntModel;
                int.TryParse(VehModel.ToString(), out IntModel);
                VehModel = IntModel;
                if (VehModel.IsValid) return true;
            }

            return false;
        }

        public static Model TranslateToVehicleModel(string name)
        {
            Model VehModel = name;

            if (VehModel.IsValid) return VehModel;
            else
            {
                int IntModel;
                int.TryParse(VehModel.ToString(), out IntModel);
                VehModel = IntModel;
                if (VehModel.IsValid) return VehModel;
            }

            return null;
        }


        public static bool CanBeSeenByCops(SuspectHandler suspect)
        {
            foreach (CopUnitHandler Unit in DangerousIndividuals.CopsChasing)
            {
                if (Unit.Leader.IsInRangeOf(suspect.Criminal.Position, 300f) && Util.CanPedSeePed(Unit.Leader, suspect.Criminal, false)) return true;
            }
            return false;
        }

        public static void GetCorrectUnitForArea(CopUnitHandler Unit, Vector3 pos)
        {
            String MapArea = Util.GetMapAreaAtCoords(pos);
            List<Model> Vehicle = LSPDCars;
            List<Model> Ped = LSPDModels;

            switch (Unit.UnitType)
            {


                case CopUnitType.Bike:
                    {
                        Ped = LSPDBikeModels;
                        Vehicle = LSPDBikes;
                        break;
                    }

                case CopUnitType.Patrol | CopUnitType.AveragePolice:
                    {
                        Vehicle = LSPDCars;

                        if (MapArea == "city")
                        {
                            Ped = LSPDModels;
                            Vehicle = LSPDCars;

                        }
                        if (MapArea == "countryside")
                        {
                            Vehicle = LSSDCars;
                            Ped = LSSDModels;
                        }


                        if (NorthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            //if (HwyPatrol.Contains(World.GetStreetName(pos)) || PaletoArea.Contains(World.GetStreetName(pos)))
                            if (pos.Z < 70f)
                            {
                                Vehicle = LSSDCars;
                                Ped = LSSDModels;

                            }
                            else
                            {
                                Vehicle = SAPRCars;
                                Ped = SAPRModels;
                            }
                        }
                        if (SouthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = BCSOCars;
                            Ped = BCSOModels;
                        }
                        if (Sheriff.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSSDCars;
                            Ped = LSSDModels;
                        }
                        if (PortAuthority.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSSDCars;
                            Ped = LSSDModels;
                        }

                        if (Util.IsInNorthYankton(Game.Player.Character))
                        {
                            Vehicle = NYSTCars;
                            Ped = NYSTModels;
                        }
                        if (HwyPatrol.Contains(World.GetStreetName(pos)))
                        {
                            Vehicle = HighwayCars;
                            Ped = HighwayModels;
                        }

                        break;
                    }
                case CopUnitType.PrisonerTransporter:
                    {
                        Vehicle = new List<Model> { "policet" };
                        Ped = LSPDModels;
                        break;
                    }
                case CopUnitType.AirUnit:
                    {
                        Vehicle = LSPDHelis;
                        Ped = HeliModels;

                        if (MapArea == "city")
                        {
                            Vehicle = LSPDHelis;
                        }
                        if (MapArea == "countryside")
                        {
                            Vehicle = LSSDHelis;
                        }
                        if (Util.IsInNorthYankton(Game.Player.Character))
                        {
                            Vehicle = LSPDHelis;
                        }
                        if (NorthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            if (pos.Z < 70f)
                            {
                                Vehicle = LSSDHelis;
                            }
                            else
                            {
                                Vehicle = SAPRHelis;
                            }
                        }
                        if (SouthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = BCSOHelis;
                        }
                        if (Sheriff.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSSDHelis;
                        }
                        if (PortAuthority.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSPDCars;
                        }

                        break;
                    }
                case CopUnitType.LocalNoose:
                    {
                        Vehicle = LSPDLocalSWAT;
                        Ped = SWATModels;

                        if (MapArea == "city")
                        {
                            Vehicle = LSPDLocalSWAT;
                            Ped = SWATModels;
                        }
                        if (MapArea == "countryside")
                        {
                            Vehicle = LSSDLocalSWAT;
                            Ped = SWATModels;
                        }

                        if (NorthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            //if (HwyPatrol.Contains(World.GetStreetName(pos)) || PaletoArea.Contains(World.GetStreetName(pos)))
                            if (pos.Z < 50f)
                            {
                                Vehicle = LSSDLocalSWAT;
                                Ped = SWATModels;
                            }
                            else
                            {
                                Vehicle = SAPRLocalSWAT;
                                Ped = SWATModels;
                            }
                        }
                        if (SouthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = BCSOLocalSWAT;
                            Ped = SWATModels;

                        }
                        if (Sheriff.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSSDLocalSWAT;
                            Ped = SWATModels;

                        }
                        if (PortAuthority.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSPDCars;
                            Ped = SWATModels;
                        }

                        break;
                    }
                case CopUnitType.NOoSE:
                    {
                        Vehicle = LSPDSWATCars;
                        Ped = SWATModels;

                        if (MapArea == "city")
                        {
                            Vehicle = LSPDSWATCars;
                            Ped = SWATModels;
                        }
                        if (MapArea == "countryside")
                        {
                            Vehicle = LSSDSWATCars;
                            Ped = SWATModels;
                        }

                        if (NorthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            //if (HwyPatrol.Contains(World.GetStreetName(pos)) || PaletoArea.Contains(World.GetStreetName(pos)))
                            if (pos.Z < 50f)
                            {
                                Vehicle = LSSDSWATCars;
                                Ped = SWATModels;
                            }
                            else
                            {
                                Vehicle = SAPRSWATCars;
                                Ped = SWATModels;
                            }
                        }
                        if (SouthernWilderness.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = BCSOSWATCars;
                            Ped = SWATModels;
                        }
                        if (Sheriff.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSSDSWATCars;
                            Ped = SWATModels;
                        }
                        if (PortAuthority.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = LSPDSWATCars;
                            Ped = SWATModels;
                        }

                        break;
                    }
                case CopUnitType.NOoSEAirUnit:
                    {
                        Vehicle = NOoSEHelis;
                        Ped = SWATModels;
                        break;
                    }
                case CopUnitType.InsurgentNoose:
                    {
                        
                        Vehicle = ArmoredNOoSE;
                        Ped = SWATModels;

                        if (Sheriff.Contains(World.GetZoneNameLabel(pos)))
                        {
                            Vehicle = ArmoredNOoSESheriff;
                        }
                        break;
                    }
                case CopUnitType.Army:
                    {
                        Vehicle = ArmyCarsCity;
                        Ped = ArmyModels;

                        break;
                    }
                case CopUnitType.ArmyAirUnit:
                    {
                        Vehicle = ArmyHelis;
                        Ped = ArmyModels;
                        break;
                    }
            }

            //Makes sure the script doesn't crash due to lack of models
            if (Vehicle.Count == 0) Vehicle = LSPDCars;
            if (Ped.Count == 0) Ped = LSPDModels;

            Unit.VehicleModel = Vehicle[Util.RandomInt(0, Vehicle.Count - 1)];
            Unit.LeaderModel = Ped[Util.RandomInt(0, Ped.Count - 1)];
            Unit.PartnerModels = Ped[Util.RandomInt(0, Ped.Count - 1)];

            //String Veh = Vehicle[Util.RandomInt(0, Vehicle.Count - 1)];
            if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(Unit.UnitType.ToString() +" - "+ Unit.VehicleModel.ToString() + " from " + World.GetZoneNameLabel(pos));
        }


        public static List<string> Exhausts = new List<string>
    {
        "exhaust","exhaust_2","exhaust_3","exhaust_4","exhaust_5","exhaust_6","exhaust_7"
    };

        public static void ForceNitro(Vehicle veh)
        {
            if (Util.CanWeUse(veh) && veh.Speed > 20f && !Util.IsSliding(veh, 3f))
            {
                Function.Call(Hash._SET_VEHICLE_ENGINE_TORQUE_MULTIPLIER, veh, 50f);

                if (Function.Call<bool>(Hash._0x8702416E512EC454, "scr_carsteal4"))
                {
                    float direction = veh.Heading;
                    float pitch = Function.Call<float>(Hash.GET_ENTITY_PITCH, veh);

                    foreach (string exhaust in Exhausts)
                    {
                        Vector3 offset = GetBoneCoords(veh, GetBoneIndex(veh, exhaust));
                        Function.Call(Hash._0x6C38AF3693A69A91, "scr_carsteal4");
                        Function.Call<int>(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "scr_carsteal5_car_muzzle_flash", offset.X, offset.Y, offset.Z, 0f, pitch, direction - 90f, 1.0f, false, false, false);
                    }
                }
                else
                {
                    Function.Call(Hash._0xB80D8756B4668AB6, "scr_carsteal4");
                }
            }
        }
        public static int GetBoneIndex(Entity entity, string value)
        {
            return GTA.Native.Function.Call<int>(Hash._0xFB71170B7E76ACBA, entity, value);
        }

        public static Vector3 GetBoneCoords(Entity entity, int boneIndex)
        {
            return GTA.Native.Function.Call<Vector3>(Hash._0x44A8FCB8ED227738, entity, boneIndex);
        }


    }
}
