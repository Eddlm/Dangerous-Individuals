using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using static LSPDispatch.Info;

namespace LSPDispatch
{
    public class DefineUnits
    {

        public static void DefineCriminal(SuspectHandler suspect, Vector3 spawnpos, CriminalType type, bool transferredCriminal)
        {
            Model VehModel = null;

            List<string> PedModels = new List<string>();

            //PHASE 1: DEFINE CRIMINAL FLAGS
            switch (type)
            {
                case CriminalType.Dynamic:
                    {

                        Util.Notify("web_lossantospolicedept", "~b~DISPATCH", "CIVILIAN ENGAGING COPS", "Officers are under attack.");

                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                            CriminalFlags.CAN_RAM,

                        };

                        break;
                    }

                case CriminalType.MainCharacters:
                    {
                        //suspect.Auth_DeadlyForce = true;

                        suspect.Flags = new List<CriminalFlags>
                        {
                            CriminalFlags.SURRENDERS_IF_HURT,

                            CriminalFlags.CAN_DRIVEBY,
                            CriminalFlags.CAN_RAM,
                            CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                            CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                            CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.PREFERS_FAST_VEHICLES,
                            CriminalFlags.HAS_BODY_ARMOR,
                            CriminalFlags.DYNAMIC_EVASION_BEHAVIOR
                        };

                        int RandomInt = Util.RandomInt(1, 3);

                        if (RandomInt == 1)
                        {
                            PedModels.Add("player_zero"); //Michael;
                            suspect.Flags.Add(CriminalFlags.USES_PRO_GUNS);
                            suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_CAUTIOUS);

                            if (Util.RandomInt(1, 10) < 6) VehModel = Util.GetRandomVehicleHash();

                        }
                        if (RandomInt == 2)
                        {
                            PedModels.Add("player_one"); //Franklin;
                            suspect.Flags.Add(CriminalFlags.USES_THUG_GUNS);
                            suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_CAUTIOUS);

                            if (Util.RandomInt(1, 10) < 6) VehModel = Util.GetRandomVehicleHash();

                        }
                        if (RandomInt == 3)
                        {
                            PedModels.Add("player_two"); //Trevor;
                            suspect.Flags.Add(CriminalFlags.USES_HEAVY_GUNS);
                            suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_ALWAYS);

                            if (Util.RandomInt(1, 10) < 7) VehModel = Util.GetRandomVehicleFromList(Info.MilitaryGradeVehs);
                        }
                        break;
                    }

                case CriminalType.Test:
                    {
                        //suspect.Auth_DeadlyForce = true;

                        suspect.Flags = new List<CriminalFlags>
                        {
                                                        CriminalFlags.SURRENDERS_IF_AIMED_AT,
                                                CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                            CriminalFlags.SURRENDERS_IF_HURT,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                            CriminalFlags.SURRENDERS_IF_SHOT_AT_EASY,

                        };
                        break;
                    }

                case CriminalType.AmateurRacers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.SportVehs);
                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_AIMED_AT,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.PREFERS_FAST_VEHICLES,
                            CriminalFlags.PREFERS_ALLEYWAYS,
                            CriminalFlags.SURRENDERS_IF_SHOT_AT_EASY,
                                                    CriminalFlags.CHEAT_NITRO,
CriminalFlags.ONGOING_CHASE_PATROL,
CriminalFlags.IMPORTANT_VEHICLE_IMPOUND,
                        };
                        break;
                    }
                case CriminalType.ProRacers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.SportVehs);

                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.PREFERS_ALLEYWAYS,
                            CriminalFlags.SURRENDERS_IF_AIMED_AT,
                                                        CriminalFlags.SURRENDERS_IF_SHOT_AT_HARD,
                        CriminalFlags.CHEAT_NITRO,

                            CriminalFlags.CAN_RAM,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.PREFERS_FAST_VEHICLES,
                            CriminalFlags.CHEAT_EMP,
                            CriminalFlags.CAN_BRAKECHECK,
                            CriminalFlags.ONGOING_CHASE_PATROL,
                            CriminalFlags.IMPORTANT_VEHICLE_IMPOUND,
                        };
                        break;
                    }
                case CriminalType.ViolentGangBallas:
                    {
                        PedModels = BallasModels;
                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.CAN_DRIVEBY,
                            CriminalFlags.CAN_RAM,
                            CriminalFlags.USES_THUG_GUNS,
                            CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                            CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                            CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                            CriminalFlags.SPAWNS_SMALL_GANG,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                    };

                        break;
                    }
                case CriminalType.ViolentGangFamilies:
                    {
                        PedModels = Info.FamiliesModels;
                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.CAN_DRIVEBY,
                            CriminalFlags.CAN_RAM,
                            CriminalFlags.USES_THUG_GUNS,
                            CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                            CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                            CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                            CriminalFlags.SPAWNS_SMALL_GANG,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                            CriminalFlags.ONGOING_CHASE_PATROL
                    };
                        if (Util.RandomInt(1, 10) < 7) suspect.Flags.Add(CriminalFlags.ONGOING_CHASE_SWAT);

                        break;
                    }
                case CriminalType.ViolentBigGang:
                    {
                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.CAN_DRIVEBY,
                            CriminalFlags.CAN_RAM,
                            CriminalFlags.USES_THUG_GUNS,
                            CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF,
                            CriminalFlags.GREAT_DRIVING_ABILITY,
                            CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                            CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                            CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                            CriminalFlags.SPAWNS_BIG_GANG,
                                                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
CriminalFlags.ONGOING_CHASE_PATROL
                    };
                        if (Util.RandomInt(1, 10) < 7) suspect.Flags.Add(CriminalFlags.ONGOING_CHASE_SWAT);

                        break;
                    }
                case CriminalType.PacificFleeingFoot:
                    {
                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_AIMED_AT,
                    CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                    CriminalFlags.SURRENDERS_IF_HURT,
                    CriminalFlags.SURRENDERS_IF_SHIT_EXPLODES,
                    CriminalFlags.SURRENDERS_IF_SURROUNDED,
                    CriminalFlags.SURRENDERS_WHEN_STUNNED,
                    CriminalFlags.SURRENDERS_WHEN_RAGDOLL,
                    CriminalFlags.CAREFUL_DRIVING,
                    CriminalFlags.AVERAGE_DRIVING_ABILITY,
                    CriminalFlags.SURRENDERS_IF_SHOT_AT_EASY,
                    };
                        break;
                    }
                case CriminalType.AggresiveThug:
                    {

                        if (Util.RandomInt(1, 10) > 5) VehModel = Util.GetRandomVehicleFromList(Info.AverageVehs);

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                        CriminalFlags.CAN_STANDOFF_AGGRESIVE,
                        CriminalFlags.CAN_DRIVEBY,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.USES_THUG_GUNS,
                        CriminalFlags.POOR_DRIVING_ABILITY,

                                            CriminalFlags.SURRENDERS_IF_HURT,
                                                                CriminalFlags.SURRENDERS_WHEN_STUNNED,

                    };
                        if (Util.RandomInt(1, 10) > 5) suspect.Flags.Add(CriminalFlags.HAS_1_PARTNER);
                        if (Util.RandomInt(1, 10) > 3) suspect.Flags.Add(CriminalFlags.HAS_3_PARTNERS);

                        if (Util.RandomInt(1, 10) < 3) suspect.Flags.Add(CriminalFlags.ONGOING_CHASE_AVERAGE);

                        break;
                    }
                case CriminalType.SmartThug:
                    {
                        if (Util.RandomInt(1, 10) <= 5) VehModel = Util.GetRandomVehicleFromList(Info.AverageVehs);

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_IF_SURROUNDED,
                        CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.PREFERS_FAST_VEHICLES,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.AVERAGE_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.CAN_BRAKECHECK,
                        CriminalFlags.SURRENDERS_IF_SHOT_AT_HARD,
                            CriminalFlags.SURRENDERS_IF_HURT,

                    };

                        if (Util.RandomInt(1, 10) > 5) suspect.Flags.Add(CriminalFlags.USES_THUG_GUNS);
                        else suspect.Flags.Add(CriminalFlags.USES_AVERAGE_GUNS);

                        if (Util.RandomInt(1, 10) < 7) suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_CAUTIOUS);
                        if (Util.RandomInt(1, 10) < 5) suspect.Flags.Add(CriminalFlags.SURRENDERS_WHEN_RAGDOLL);
                        if (Util.RandomInt(1, 10) < 5) suspect.Flags.Add(CriminalFlags.CAN_DRIVEBY);
                        if (Util.RandomInt(1, 10) < 3) suspect.Flags.Add(CriminalFlags.CHEAT_HIDES_ON_FOOT_WHEN_HIDDEN);
                        if (Util.RandomInt(1, 10) < 8) suspect.Flags.Add(CriminalFlags.CAN_RAM);

                        if (Util.RandomInt(1, 10) < 7) suspect.Flags.Add(CriminalFlags.ONGOING_CHASE_AVERAGE);

                        break;
                    }
                case CriminalType.AmateurRobbers:
                    {
                        if (Util.RandomInt(1, 10) < 2) { VehModel = Util.GetRandomVehicleFromList(Info.AverageVehs); suspect.Auth_Ramming = true; }

                        suspect.Flags = new List<CriminalFlags>
                    {
                    CriminalFlags.SURRENDERS_WHEN_STUNNED,
                    CriminalFlags.SURRENDERS_WHEN_RAGDOLL,
                    CriminalFlags.SURRENDERS_IF_SURROUNDED,
                    CriminalFlags.CAN_RAM,
                    CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                    CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                    CriminalFlags.CHEAT_HIDES_ON_FOOT_WHEN_HIDDEN,
                        CriminalFlags.AVERAGE_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.SURRENDERS_IF_SHOT_AT_HARD,
                        CriminalFlags.HAS_1_PARTNER,
                            CriminalFlags.SURRENDERS_IF_HURT,
                                                    CriminalFlags.ONGOING_CHASE_PATROL,
                    };
                        if (Util.RandomInt(1, 10) < 5) suspect.Flags.Add(CriminalFlags.USES_THUG_GUNS); else suspect.Flags.Add(CriminalFlags.USES_AVERAGE_GUNS);
                        if (Util.RandomInt(1, 10) < 5) suspect.Flags.Add(CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES);
                        break;
                    }
                case CriminalType.ExperiencedRobbers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.AverageVehs);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                    CriminalFlags.SURRENDERS_WHEN_STUNNED,
                    CriminalFlags.SURRENDERS_WHEN_RAGDOLL,
                    CriminalFlags.SURRENDERS_IF_SURROUNDED,
                            CriminalFlags.CAN_BRAKECHECK,
                    CriminalFlags.CAN_RAM,
                    CriminalFlags.PREFERS_FAST_VEHICLES,
                    CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                    CriminalFlags.CHEAT_HIDES_ON_FOOT_WHEN_HIDDEN,
                    CriminalFlags.USES_AVERAGE_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                                    CriminalFlags.SURRENDERS_IF_HURT,
                                                            CriminalFlags.ONGOING_CHASE_PATROL,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,

                    };
                        if (Util.RandomInt(0, 10) < 5) suspect.Flags.Add(CriminalFlags.HAS_1_PARTNER); else suspect.Flags.Add(CriminalFlags.HAS_3_PARTNERS);
                        if (Util.RandomInt(0, 10) < 7) suspect.Flags.Add(CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES);
                        if (Util.RandomInt(0, 10) < 6) suspect.Flags.Add(CriminalFlags.CHEAT_HIDES_ON_FOOT_WHEN_HIDDEN);
                        if (Util.RandomInt(0, 10) < 5) suspect.Flags.Add(CriminalFlags.CHEAT_CHANGES_VEHICLES_WHEN_HIDDEN);
                        if (Util.RandomInt(0, 10) < 8) suspect.Flags.Add(CriminalFlags.CAN_DRIVEBY);
                        break;

                    }

                case CriminalType.CashTruckStealers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.StolenCashTrucks);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_HURT,
                            CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                        CriminalFlags.USES_PRO_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.CAN_BRAKECHECK,
                            CriminalFlags.CAN_RAM,
                            CriminalFlags.CAN_DRIVEBY,
                            CriminalFlags.HAS_3_PARTNERS,
                            CriminalFlags.ONGOING_CHASE_AIRUNIT,
                            CriminalFlags.ONGOING_CHASE_AVERAGE,
                            CriminalFlags.CAN_STANDOFF_CAUTIOUS,
                            CriminalFlags.IMPORTANT_VEHICLE_CASH,
                            
                    };
                        if (Util.RandomInt(1, 10) < 5) suspect.Flags.Add(CriminalFlags.CHEAT_EMP);
                        if (Util.RandomInt(1, 10) < 4) suspect.Flags.Add(CriminalFlags.CHEAT_CAN_EASILY_DISSAPEAR_WHEN_HIDDEN);
                        break;
                    }
                case CriminalType.ProffessionalsHeisters:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.HeistVehs);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_HURT,
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.SURRENDERS_IF_SURROUNDED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                        CriminalFlags.USES_PRO_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                                                    CriminalFlags.CAN_RAM,
                                                                                CriminalFlags.IMPORTANT_VEHICLE_CASH,

                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.PREFERS_HEIST_VEHICLES,
                        CriminalFlags.CAN_BRAKECHECK,
                        CriminalFlags.HAS_3_PARTNERS,
                            CriminalFlags.ONGOING_CHASE_AIRUNIT,
                            CriminalFlags.ONGOING_CHASE_AVERAGE,
                            CriminalFlags.CHEAT_CAN_EASILY_DISSAPEAR_WHEN_HIDDEN,

                    };

                        if (Util.RandomInt(0, 10) < 8) suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_CAUTIOUS);
                        if (Util.RandomInt(0, 10) < 8) suspect.Flags.Add(CriminalFlags.CAN_DRIVEBY);

                        break;
                    }
                case CriminalType.MotorbikeHeisters:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.HeistBikes);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                            CriminalFlags.SURRENDERS_IF_HURT,
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.SURRENDERS_IF_SURROUNDED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                        CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE,
                        CriminalFlags.USES_PRO_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.PREFERS_HEIST_VEHICLES,
                        CriminalFlags.CAN_BRAKECHECK,
                        CriminalFlags.HAS_3_PARTNERS,
                            CriminalFlags.ONGOING_CHASE_AIRUNIT,
                            CriminalFlags.ONGOING_CHASE_AVERAGE,
                            CriminalFlags.CHEAT_CAN_EASILY_DISSAPEAR_WHEN_HIDDEN,
                                                        CriminalFlags.IMPORTANT_VEHICLE_CASH,


                    };
                        if (Util.RandomInt(1, 10) < 8) suspect.Flags.Add(CriminalFlags.CAN_STANDOFF_CAUTIOUS);
                        if (Util.RandomInt(0, 10) < 6) suspect.Flags.Add(CriminalFlags.CAN_DRIVEBY);

                        break;
                    }
                case CriminalType.NormalHeisters:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.SUVs);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_IF_HURT,
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                                                CriminalFlags.CAN_STEAL_POLICE_VEHICLES,
                        CriminalFlags.PREFERS_HEAVY_VEHICLES, //
						CriminalFlags.CAN_DRIVEBY,
                        CriminalFlags.CAN_BRAKECHECK,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.CAN_STANDOFF_CAUTIOUS,
                        CriminalFlags.USES_PRO_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,

                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.SURRENDERS_IF_AIMED_AT,
                        CriminalFlags.HAS_1_PARTNER,
                            CriminalFlags.ONGOING_CHASE_AIRUNIT,
                            CriminalFlags.ONGOING_CHASE_AVERAGE,
                                                        CriminalFlags.IMPORTANT_VEHICLE_CASH,

                    };
                        if (Util.RandomInt(1, 10) > 5) suspect.Flags.Add(CriminalFlags.USES_HEAVY_GUNS);
                        if (Util.RandomInt(1, 10) > 8) suspect.Flags.Add(CriminalFlags.CHEAT_DRIVEBY_STICKY_MINES);
                        break;
                    }
                case CriminalType.MilitaryStealers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.MilitaryGradeVehs);
                        suspect.Auth_Ramming = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                            CriminalFlags.SURRENDERS_IF_HURT,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_DRIVEBY,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.CAN_STANDOFF_AGGRESIVE,
                        CriminalFlags.USES_HEAVY_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,

                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.SPAWNS_SMALL_GANG,
                            CriminalFlags.ONGOING_CHASE_AIRUNIT,
                            CriminalFlags.ONGOING_CHASE_AVERAGE,
                            CriminalFlags.ONGOING_CHASE_SWAT,
                                                    CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,


                        };
                        if (Util.RandomInt(1, 10) > 5) suspect.Flags.Add(CriminalFlags.USES_HEAVY_GUNS);
                        if (Util.RandomInt(1, 10) > 8) suspect.Flags.Add(CriminalFlags.CHEAT_DRIVEBY_STICKY_MINES);
                        break;
                    }


                case CriminalType.Terrorists:
                    {
                        //VehModel = Util.GetRandomVehicleFromList(Info.MilitaryGradeVehs);
                        suspect.Auth_DeadlyForce = true;

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_DRIVEBY,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.CAN_STANDOFF_AGGRESIVE,
                        CriminalFlags.USES_PRO_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,

                        CriminalFlags.HAS_BODY_ARMOR,
                        CriminalFlags.HATES_EVERYONE,
                        CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF,
                        CriminalFlags.ONGOING_CHASE_PATROL,
                                                CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,

                        };
                        if (Util.RandomInt(1, 10) < 6)
                        {
                            suspect.Flags.Add(CriminalFlags.USES_HEAVY_GUNS);
                            suspect.Flags.Add(CriminalFlags.HAS_2_PARTNERS);
                        }
                        else
                        {
                            suspect.Flags.Add(CriminalFlags.SPAWNS_SMALL_GANG);
                        }
                        break;
                    }
                case CriminalType.CargoStealers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.Truck);

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.CAN_STANDOFF_CAUTIOUS,
                        CriminalFlags.USES_AVERAGE_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.HAS_1_PARTNER,
                        CriminalFlags.ONGOING_CHASE_PATROL,
                                                    CriminalFlags.IMPORTANT_VEHICLE_CASH,

                        };

                        break;
                    }
                case CriminalType.CommertialVanStealers:
                    {
                        VehModel = Util.GetRandomVehicleFromList(Info.CommercialVans);

                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_STANDOFF_CAUTIOUS,
                        CriminalFlags.USES_AVERAGE_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,
                        CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,
                        CriminalFlags.HAS_1_PARTNER,
                        CriminalFlags.ONGOING_CHASE_PATROL,
                                                    CriminalFlags.IMPORTANT_VEHICLE_CASH,

                        };

                        break;
                    }
                case CriminalType.EscapedPrisoner:
                    {
                        VehModel = "pbus";
                        PedModels = PrisonerModels;
                        suspect.Flags = new List<CriminalFlags>
                    {
                        CriminalFlags.SURRENDERS_WHEN_STUNNED,
                        CriminalFlags.CAN_STEAL_PARKED_VEHICLES,
                        CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES,
                        CriminalFlags.CAN_RAM,
                        CriminalFlags.CAN_STANDOFF_CAUTIOUS,
                        CriminalFlags.USES_AVERAGE_GUNS,
                        CriminalFlags.GREAT_DRIVING_ABILITY,

                        CriminalFlags.HAS_1_PARTNER,
                                                CriminalFlags.DYNAMIC_EVASION_BEHAVIOR,

                                                CriminalFlags.CAN_DRIVEBY
                        };
                        if (Util.RandomInt(0, 10) < 5) suspect.Flags.Add(CriminalFlags.HAS_1_PARTNER);
                        if (Util.RandomInt(0, 10) < 5) suspect.Flags.Add(CriminalFlags.USES_AVERAGE_GUNS);

                        break;
                    }
            }
            //PHASE 2: DEFINE CRIMINAL PED BASED ON FLAGS

            if (!transferredCriminal)
            {
                if (PedModels.Count > 0)
                {
                    suspect.Criminal = World.CreatePed(PedModels[Util.RandomInt(0, PedModels.Count - 1)], spawnpos);
                }
                else
                {
                    suspect.Criminal = World.CreateRandomPed(spawnpos);
                }
            }

            //WEAPONS, DRIVING ABILITY, ARMOR
            int accuracy = 50;
            if (suspect.Flags.Contains(CriminalFlags.USES_AVERAGE_GUNS)) accuracy = 70;
            if (suspect.Flags.Contains(CriminalFlags.USES_PRO_GUNS)) accuracy = 90;

            int Armor = 0;
            if (suspect.Flags.Contains(CriminalFlags.HAS_BODY_ARMOR)) Armor = 100;

            float DrvAbility = 0.2f;
            if (suspect.Flags.Contains(CriminalFlags.AVERAGE_DRIVING_ABILITY)) DrvAbility = 0.6f;
            if (suspect.Flags.Contains(CriminalFlags.GREAT_DRIVING_ABILITY)) DrvAbility = 1f;

            List<WeaponHash> Guns = new List<WeaponHash>();
            if (suspect.Flags.Contains(CriminalFlags.USES_THUG_GUNS)) { foreach (WeaponHash gun in Util.ThugGuns) Guns.Add(gun); }
            if (suspect.Flags.Contains(CriminalFlags.USES_AVERAGE_GUNS)) { foreach (WeaponHash gun in Util.AverageGuns) Guns.Add(gun); }
            if (suspect.Flags.Contains(CriminalFlags.USES_PRO_GUNS)) { foreach (WeaponHash gun in Util.ProGuns) Guns.Add(gun); }
            if (suspect.Flags.Contains(CriminalFlags.USES_HEAVY_GUNS)) { foreach (WeaponHash gun in Util.HeavyGuns) Guns.Add(gun); }
            if (Guns.Count == 0) Guns.Add(WeaponHash.Unarmed);


            //Make some suspects try to go Offroad, for variety
            if (new[] { CriminalType.NormalHeisters, CriminalType.ProffessionalsHeisters, CriminalType.MainCharacters, CriminalType.ExperiencedRobbers, CriminalType.SmartThug, CriminalType.MilitaryStealers }.Contains(suspect.KindOfCriminal) && Util.RandomInt(0, 10) <= 2)
            {
                suspect.Flags.Add(CriminalFlags.PREFERS_OFFROAD);
            }

            if (!transferredCriminal) Util.PreparePed(suspect.Criminal, 100, accuracy, Armor, DrvAbility, Guns[Util.RandomInt(0, Guns.Count - 1)], Guns[Util.RandomInt(0, Guns.Count - 1)], Guns[Util.RandomInt(0, Guns.Count - 1)]);

            int CriminalGroup = Function.Call<int>(Hash.CREATE_GROUP);
            Function.Call(Hash.SET_PED_AS_GROUP_LEADER, suspect.Criminal.Handle, CriminalGroup);

            if (transferredCriminal)
            {
                //Auths
                if (!Guns.Contains(WeaponHash.Unarmed)) { suspect.Auth_DeadlyForce = true; suspect.Auth_Ramming = true; }
                if (suspect.Flags.Contains(CriminalFlags.CAN_DRIVEBY)) { suspect.Auth_DeadlyForce = true; suspect.Auth_Ramming = true; }
            }
            else
            {
                if (suspect.Criminal.Weapons.Current.Hash != WeaponHash.Unarmed) suspect.Auth_DeadlyForce = true;
            }

            //DRIVING STYLE
            suspect.DrivingStyle = 4 + 8 + 16;
            if (suspect.Flags.Contains(CriminalFlags.CAREFUL_DRIVING)) suspect.DrivingStyle += 1 + 2 + 32;

            //STANDOFF
            if (suspect.Flags.Contains(CriminalFlags.CAN_STANDOFF_CAUTIOUS))
            {
                suspect.CopsToFightAgainst = 2;
                suspect.RadiusToFight = 20;
            }
            if (suspect.Flags.Contains(CriminalFlags.CAN_STANDOFF_AGGRESIVE))
            {
                suspect.CopsToFightAgainst = 10;
                suspect.RadiusToFight = 30;
            }

            //VEHICLE CHOSEN & PARTNER SPAWNING
            suspect.Criminal.RelationshipGroup = DangerousIndividuals.CriminalsRLGroup;

            if (!transferredCriminal)
            {
                if (suspect.Flags.Contains(CriminalFlags.HATES_EVERYONE))
                {
                    suspect.Criminal.RelationshipGroup = DangerousIndividuals.PublicEnemyRLGroup;
                }
                else
                {
                    if (type == CriminalType.ViolentGangBallas)
                    {
                        suspect.Criminal.RelationshipGroup = DangerousIndividuals.EnemyGangRLGroup;
                    }
                }
                if (VehModel != null) suspect.VehicleChosen = World.CreateVehicle(VehModel, suspect.Criminal.Position.Around(5));
                Script.Wait(200);
                if (Util.CanWeUse(suspect.VehicleChosen))
                {
                    //Util.WarnPlayer(DangerousIndividuals.ScriptName, "Vehicle failed to spawn", "~r~The criminal vehicle (" + VehModel + ") has failed to spawn.");
                    if (type == CriminalType.ProRacers || type == CriminalType.AmateurRacers)
                    {
                        Util.RandomTuning(suspect.VehicleChosen); //DangerousIndividuals.LayoutFile
                    }
                    if (type == CriminalType.CargoStealers && Info.Truck.Contains(suspect.VehicleChosen.Model))
                    {
                        suspect.Cargo = World.CreateVehicle(Util.GetRandomVehicleFromList(Info.Cargo), suspect.VehicleChosen.Position + (suspect.VehicleChosen.ForwardVector * -5), suspect.VehicleChosen.Heading);
                        Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, suspect.VehicleChosen, suspect.Cargo, 10);
                    }
                }


                int nOfPartners = 0;
                if (suspect.Flags.Contains(CriminalFlags.HAS_3_PARTNERS)) nOfPartners = 3;
                if (suspect.Flags.Contains(CriminalFlags.HAS_2_PARTNERS)) nOfPartners = 2;
                if (suspect.Flags.Contains(CriminalFlags.HAS_1_PARTNER)) nOfPartners = 1;
                if (suspect.Flags.Contains(CriminalFlags.SPAWNS_SMALL_GANG)) nOfPartners = Util.RandomInt(3, 6);
                if (suspect.Flags.Contains(CriminalFlags.SPAWNS_BIG_GANG)) nOfPartners = Util.RandomInt(6, 15);

                if (Util.CanWeUse(suspect.VehicleChosen) && suspect.VehicleChosen.PassengerSeats < nOfPartners) nOfPartners = suspect.VehicleChosen.PassengerSeats;

                Ped Partner;
                for (int i = 0; i < nOfPartners; i++)
                {
                    if (PedModels.Count > 0)
                    {
                        Partner = World.CreatePed(PedModels[Util.RandomInt(0, PedModels.Count - 1)], spawnpos);
                        suspect.Partners.Add(Partner);
                    }
                    else
                    {
                        Partner = World.CreateRandomPed(suspect.Criminal.Position.Around(3));
                        suspect.Partners.Add(Partner);
                    }


                    if (suspect.Flags.Contains(CriminalFlags.HATES_EVERYONE))
                    {
                        Partner.RelationshipGroup = DangerousIndividuals.PublicEnemyRLGroup;
                    }
                    else
                    {
                        if (type == CriminalType.ViolentGangBallas)
                        {
                            Partner.RelationshipGroup = DangerousIndividuals.EnemyGangRLGroup;
                        }
                        else
                        {
                            Partner.RelationshipGroup = DangerousIndividuals.CriminalsRLGroup;
                        }
                    }
                }
            }


            if (!suspect.Flags.Contains(CriminalFlags.CAN_DRIVEBY)) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, suspect.Criminal, 2, false);
            else
            {
                if (suspect.Flags.Contains(CriminalFlags.USES_THUG_GUNS)) suspect.Criminal.Weapons.Give(WeaponHash.Pistol, -1, true, true);
                if (suspect.Flags.Contains(CriminalFlags.USES_AVERAGE_GUNS)) suspect.Criminal.Weapons.Give(WeaponHash.MicroSMG, -1, true, true);
                if (suspect.Flags.Contains(CriminalFlags.USES_PRO_GUNS)) suspect.Criminal.Weapons.Give(WeaponHash.SMG, -1, true, true);
                if (suspect.Flags.Contains(CriminalFlags.USES_HEAVY_GUNS)) suspect.Criminal.Weapons.Give(WeaponHash.AssaultSMG, -1, true, true);
            }

            //PARTNER STUFF 
            foreach (Ped partner in suspect.Partners)
            {
                if (!suspect.Flags.Contains(CriminalFlags.CAN_DRIVEBY)) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, partner, 2, false);
                else
                {
                    if (suspect.Flags.Contains(CriminalFlags.USES_THUG_GUNS)) partner.Weapons.Give(WeaponHash.Pistol, -1, true, true);
                    if (suspect.Flags.Contains(CriminalFlags.USES_AVERAGE_GUNS)) partner.Weapons.Give(WeaponHash.MicroSMG, -1, true, true);
                    if (suspect.Flags.Contains(CriminalFlags.USES_PRO_GUNS)) partner.Weapons.Give(WeaponHash.SMG, -1, true, true);
                    if (suspect.Flags.Contains(CriminalFlags.USES_HEAVY_GUNS)) partner.Weapons.Give(WeaponHash.AssaultSMG, -1, true, true);
                }
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, partner.Handle, CriminalGroup);
                Function.Call(Hash.SET_PED_NEVER_LEAVES_GROUP, partner.Handle, 1);
                Util.PreparePed(partner, 100, accuracy, Armor, DrvAbility, Guns[Util.RandomInt(0, Guns.Count - 1)], Guns[Util.RandomInt(0, Guns.Count - 1)], Guns[Util.RandomInt(0, Guns.Count - 1)]);

            }

            //NO RAGDOLL FLAG
            if (suspect.Flags.Contains(CriminalFlags.CHEAT_NO_RAGDOLL_WHEN_SHOT))
            {
                Function.Call(Hash._0x26695EC767728D84, suspect.Criminal, 1);
                foreach (Ped partner in suspect.Partners) Function.Call(Hash._0x26695EC767728D84, partner, 1);
            }
            if (!transferredCriminal && DangerousIndividuals.AllowOngoingChase.Checked)
            {
                if (suspect.Flags.Contains(CriminalFlags.ONGOING_CHASE_PATROL) && DangerousIndividuals.NumberOfUnitsDispatched(CopUnitType.Patrol) < 7)
                {
                    Vector3 pos = suspect.Criminal.Position + (suspect.Criminal.ForwardVector * -10);
                    DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.Patrol, suspect, pos, false));
                }
                if (suspect.Flags.Contains(CriminalFlags.ONGOING_CHASE_AVERAGE) && DangerousIndividuals.NumberOfUnitsDispatched(CopUnitType.Patrol) < 7)
                {
                    Vector3 pos = World.GetNextPositionOnStreet(suspect.Criminal.Position + (suspect.Criminal.ForwardVector * -20));
                    DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.AveragePolice, suspect, pos, false));
                }
                if (suspect.Flags.Contains(CriminalFlags.ONGOING_CHASE_AIRUNIT) && DangerousIndividuals.NumberOfUnitsDispatched(CopUnitType.AirUnit) < 2)
                {
                    Vector3 pos = World.GetNextPositionOnStreet(suspect.Criminal.Position + (suspect.Criminal.ForwardVector * -500));
                    DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.AirUnit, suspect, pos + new Vector3(0, 0, 50f), false));
                }
                if (suspect.Flags.Contains(CriminalFlags.ONGOING_CHASE_SWAT) && DangerousIndividuals.NumberOfUnitsDispatched(CopUnitType.NOoSE) < 2)
                {
                    Vector3 pos = World.GetNextPositionOnStreet(suspect.Criminal.Position + (suspect.Criminal.ForwardVector * -100));
                    DangerousIndividuals.CopsChasing.Add(new CopUnitHandler(CopUnitType.NOoSE, suspect, pos.Around(5f), false));
                }
            }

        }
        public static void DefineCopUnit(CopUnitHandler unit, CopUnitType type, Vector3 pos)
        {
            int nOfPartners = 0;
            int accuracy = 50;
            unit.LeaderModel = "s_m_y_cop_01";
            unit.PartnerModels = "s_m_y_cop_01";
            WeaponHash firstweapon = WeaponHash.Unarmed;
            WeaponHash secondweapon = WeaponHash.Unarmed;

            Util.SetUpCopUnitLoadout(unit, pos);

          //  Info.GetCorrectUnitForArea(unit, pos);
            switch (type)
            {
                case CopUnitType.Bike:
                    {
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_VEH_DAMAGED);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.ATTEMPTS_TASING);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);

                     if(firstweapon== WeaponHash.Unarmed)   firstweapon = WeaponHash.Pistol;
                        break;
                    }
                case CopUnitType.Patrol:
                    {

                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_VEH_DAMAGED);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.ATTEMPTS_TASING);


                        /*

                        unit.LeaderModel = Info.GetCorrectPedForArea(type, pos);
                        unit.PartnerModels = Info.GetCorrectPedForArea(type, pos);
                        unit.VehicleModel = Info.GetCorrectVehicleForArea(type, pos);
                        if (Util.GetMapAreaAtCoords(pos) == "city")
						{
							unit.Leadermodel = Info.LSPDModels[Util.RandomInt(0, Info.LSPDModels.Count - 1)];
							unit.PartnerModels = Info.LSPDModels[Util.RandomInt(0, Info.LSPDModels.Count - 1)];
							unit.VehicleModel = Info.LSPDCars[Util.RandomInt(0, Info.LSPDCars.Count - 1)];
						}
						else
						{
							unit.Leadermodel = Info.LSSDModels[Util.RandomInt(0, Info.LSSDModels.Count - 1)];
							unit.PartnerModels = Info.LSSDModels[Util.RandomInt(0, Info.LSSDModels.Count - 1)];
							unit.VehicleModel = Info.LSSDCars[Util.RandomInt(0, Info.LSSDCars.Count - 1)];
						}
                        */
                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.Pistol;
                        break;
                    }
                case CopUnitType.PrisonerTransporter:
                    {

                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.ATTEMPTS_TASING);

                        nOfPartners = 1;
                        accuracy = 70;
                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.Pistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.PumpShotgun;

                        /*
                        if (Util.GetMapAreaAtCoords(pos) == "city")
                        {
                            unit.LeaderModel = Info.LSPDModels[Util.RandomInt(0, Info.LSPDModels.Count - 1)];
                            unit.PartnerModels = Info.LSPDModels[Util.RandomInt(0, Info.LSPDModels.Count - 1)];

                        }
                        else
                        {
                            unit.LeaderModel = Info.LSSDModels[Util.RandomInt(0, Info.LSSDModels.Count - 1)];
                            unit.PartnerModels = Info.LSSDModels[Util.RandomInt(0, Info.LSSDModels.Count - 1)];
                        }*/

                        break;
                    }
                case CopUnitType.AveragePolice:
                    {
                        unit.Flags.Add(CopUnitFlags.PARTNERS_BECOME_LEADER_IF_LEADER_DIES);

                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.ATTEMPTS_TASING);

                        nOfPartners = 1;
                        accuracy = 70;
                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.Pistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.CarbineRifle;

                        break;
                    }
                case CopUnitType.AirUnit:
                    {
                        unit.Flags.Add(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_VEH_DAMAGED);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);

                        nOfPartners = 1;
                        break;
                    }
                case CopUnitType.LocalNoose:
                    {

                        unit.Flags.Add(CopUnitFlags.CAN_RAM);
                        unit.Flags.Add(CopUnitFlags.AGGRESIVE_IN_STANDOFF);
                        unit.Flags.Add(CopUnitFlags.PARTNERS_BECOME_LEADER_IF_LEADER_DIES);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.HAS_AVERAGE_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);

                        nOfPartners = 3;
                        accuracy = 85;

                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.HeavyPistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.SpecialCarbine;


                        break;
                    }
                case CopUnitType.NOoSE:
                    {

                        unit.Flags.Add(CopUnitFlags.CAN_RAM);
                        unit.Flags.Add(CopUnitFlags.AGGRESIVE_IN_STANDOFF);
                        unit.Flags.Add(CopUnitFlags.PARTNERS_BECOME_LEADER_IF_LEADER_DIES);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.HAS_AVERAGE_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);

                        
                        nOfPartners = 7;
                        accuracy = 85;

                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.HeavyPistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.SpecialCarbine;
                        break;
                    }
                case CopUnitType.InsurgentNoose:
                    {

                        unit.Flags.Add(CopUnitFlags.CAN_RAM);
                        unit.Flags.Add(CopUnitFlags.AGGRESIVE_IN_STANDOFF);
                        unit.Flags.Add(CopUnitFlags.PARTNERS_BECOME_LEADER_IF_LEADER_DIES);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.HAS_AVERAGE_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);


                        nOfPartners = 5;
                        accuracy = 85;

                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.HeavyPistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.SpecialCarbine;
                        break;
                    }

                case CopUnitType.NOoSEAirUnit:
                    {

                        unit.Flags.Add(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_VEH_DAMAGED);
                        unit.Flags.Add(CopUnitFlags.LEAVES_IF_HURT);
                        unit.Flags.Add(CopUnitFlags.PARTNERS_CAN_DRIVEBY);
                        unit.Flags.Add(CopUnitFlags.HAS_AVERAGE_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);

                        nOfPartners =  5;
                        accuracy = 100;


                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.HeavyPistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.SniperRifle;
                        break;
                    }
                case CopUnitType.Army:
                    {
                        /*
                        unit.LeaderModel = Info.ArmyModels[Util.RandomInt(0, Info.ArmyModels.Count - 1)];
                        unit.PartnerModels = Info.ArmyModels[Util.RandomInt(0, Info.ArmyModels.Count - 1)];

                        Info.
                        unit.VehicleModel = Info.ArmyCars[Util.RandomInt(0, Info.ArmyCars.Count - 1)];
                        */

                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);
                        unit.Flags.Add(CopUnitFlags.PARTNERS_CAN_DRIVEBY);
                        unit.Flags.Add(CopUnitFlags.LEADER_CAN_DRIVEBY);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.HAS_HIGH_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.LEADER_CAN_USE_VEHICLE_WEAPONS);

                        nOfPartners = 5;
                        accuracy = 90;


                        if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.HeavyPistol;
                        if (secondweapon == WeaponHash.Unarmed) secondweapon = WeaponHash.SpecialCarbine;
                        break;
                    }
                case CopUnitType.ArmyAirUnit:
                    {
                        /*
                        unit.LeaderModel = Info.ArmyModels[Util.RandomInt(0, Info.ArmyModels.Count - 1)];
                        unit.PartnerModels = Info.ArmyModels[Util.RandomInt(0, Info.ArmyModels.Count - 1)];

                        unit.VehicleModel = Info.ArmyHelis[Util.RandomInt(0, Info.ArmyHelis.Count - 1)];
                        */
                        unit.Flags.Add(CopUnitFlags.PARTNERS_CAN_DRIVEBY);
                        unit.Flags.Add(CopUnitFlags.LEADER_CAN_DRIVEBY);
                        unit.Flags.Add(CopUnitFlags.CAN_COMANDEER_VEHICLES);
                        unit.Flags.Add(CopUnitFlags.HAS_HIGH_BODY_ARMOR);
                        unit.Flags.Add(CopUnitFlags.LEADER_CAN_USE_VEHICLE_WEAPONS);
                        unit.Flags.Add(CopUnitFlags.CANT_ARREST);
                        unit.Flags.Add(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR);

                        nOfPartners = 3;
                        accuracy = 90;

                            if (firstweapon == WeaponHash.Unarmed) firstweapon = WeaponHash.CombatPistol;
                        if (secondweapon == WeaponHash.Unarmed)                            secondweapon = WeaponHash.MG;
                        break;
                    }
            }

            //PHASE 2: DEFINE CRIMINAL PED BASED ON FLAGS

            //WEAPONS, DRIVING ABILITY, ARMOR
            //accuracy = 70;
            //accuracy = 90;

            int Armor = 0;
            if (unit.Flags.Contains(CopUnitFlags.HAS_AVERAGE_BODY_ARMOR)) Armor = 100;
            if (unit.Flags.Contains(CopUnitFlags.HAS_HIGH_BODY_ARMOR)) Armor = 300;

            /*
			WeaponHash Gun_0 = WeaponHash.StunGun;
			WeaponHash Gun_1 = WeaponHash.Unarmed;
			WeaponHash Gun_2 = WeaponHash.Unarmed;
			if (type == CopUnitType.Patrol || type == CopUnitType.AveragePolice) { Gun_1 = WeaponHash.Pistol; Gun_2 = WeaponHash.CarbineRifle; }
			if (type == CopUnitType.NOoSE || type == CopUnitType.NOoSEAirUnit) { Gun_1 = WeaponHash.SpecialCarbine; Gun_2 = WeaponHash.PumpShotgun; }
			if (type == CopUnitType.Army || type == CopUnitType.ArmyAirUnit) { Gun_1 = WeaponHash.MG; Gun_2 = WeaponHash.Revolver; }
			*/

            //LEADER SPAWNING
            unit.Leader = World.CreatePed(unit.LeaderModel, pos);
            unit.Leader.BlockPermanentEvents = true;

            Util.PreparePed(unit.Leader, 100, accuracy, Armor, 100f, WeaponHash.Unarmed, firstweapon, secondweapon);

            int UnitGroup = Function.Call<int>(Hash.CREATE_GROUP);
            Function.Call(Hash.SET_PED_AS_GROUP_LEADER, unit.Leader.Handle, UnitGroup);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, unit.Leader, 0f);

            if (!unit.Flags.Contains(CopUnitFlags.LEADER_CAN_DRIVEBY)) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, unit.Leader, 2, false);

            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, unit.Leader,52, false);

            if (unit.Flags.Contains(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR)) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, unit.Leader, 3, false);

            //PARTNER SPAWNING
            unit.Leader.RelationshipGroup = DangerousIndividuals.CopsRLGroup;

            int maxPassengers = Function.Call<int>(Hash._GET_VEHICLE_MODEL_MAX_NUMBER_OF_PASSENGERS, (Model)unit.VehicleModel);
            if (unit.VehicleModel == "riot2") maxPassengers--;
            if (nOfPartners > maxPassengers) nOfPartners=maxPassengers;

            //Fix RDE Insurgent max passengers
            if (unit.VehicleModel == "nooseinsurgent" || unit.VehicleModel == "sheriffinsurgent") maxPassengers -= 2;
            if (maxPassengers <= 2 || maxPassengers- nOfPartners<2) unit.Flags.Add(CopUnitFlags.CANT_ARREST);
            Ped Partner;
            for (int i = 0; i < nOfPartners; i++)
            {
                Partner = World.CreatePed(unit.PartnerModels, unit.Leader.Position.Around(3));
                unit.Partners.Add(Partner);

                if (!unit.Flags.Contains(CopUnitFlags.PARTNERS_CAN_DRIVEBY)) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Partner, 2, false);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, unit.Leader, 52, false);
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, Partner.Handle, UnitGroup);

                Function.Call(Hash.SET_PED_NEVER_LEAVES_GROUP, Partner.Handle, 1);
                Util.PreparePed(Partner, 100, accuracy, Armor, 1f, WeaponHash.Unarmed, firstweapon, secondweapon);
                Partner.RelationshipGroup = DangerousIndividuals.CopsRLGroup;
            }

            /*
			//NO RAGDOLL FLAG
			if (unit.Flags.Contains(CriminalFlags.CHEAT_NO_RAGDOLL_WHEN_SHOT))
			{
				Function.Call(Hash._0x26695EC767728D84, unit.Leader, 1);
				foreach (Ped partner in unit.Partners) Function.Call(Hash._0x26695EC767728D84, partner, 1);
			}
			*/
        }
    }
}
