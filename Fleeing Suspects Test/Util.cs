using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Globalization;
using LSPDispatch;
using System.Linq;

public class Util : Script
{

    public enum Subtask
    {
        AIMED_SHOOTING_ON_FOOT = 4,
        GETTING_UP = 16,
        MOVING_ON_FOOT_NO_COMBAT = 35,
        MOVING_ON_FOOT_COMBAT = 38,
        USING_STAIRS = 47,
        CLIMBING = 50,
        GETTING_OFF_SOMETHING = 51,
        SWAPPING_WEAPON = 56,
        REMOVING_HELMET = 92,
        DEAD = 97,
        MELEE_COMBAT = 130,
        HITTING_MELEE = 130,
        SITTING_IN_VEHICLE = 150,
        DRIVING_WANDERING = 151,
        EXITING_VEHICLE = 152,

        ENTERING_VEHICLE_GENERAL = 160,
        ENTERING_VEHICLE_BREAKING_WINDOW = 161,
        ENTERING_VEHICLE_OPENING_DOOR = 162,
        ENTERING_VEHICLE_ENTERING = 163,
        ENTERING_VEHICLE_CLOSING_DOOR = 164,

        EXIING_VEHICLE_OPENING_DOOR_EXITING = 167,
        EXITING_VEHICLE_CLOSING_DOOR = 168,
        DRIVING_GOING_TO_DESTINATION_OR_ESCORTING = 169,
        USING_MOUNTED_WEAPON = 199,
        AIMING_THROWABLE = 289,
        AIMING_GUN = 290,
        AIMING_PREVENTED_BY_OBSTACLE = 299,
        IN_COVER_GENERAL = 287,
        IN_COVER_FULLY_IN_COVER = 288,

        RELOADING = 298,

        RUNNING_TO_COVER = 300,
        IN_COVER_TRANSITION_TO_AIMING_FROM_COVER = 302,
        IN_COVER_TRANSITION_FROM_AIMING_FROM_COVER = 303,
        IN_COVER_BLIND_FIRE = 304,

        PARACHUTING = 334,
        PUTTING_OFF_PARACHUTE = 336,

        JUMPING_OR_CLIMBING_GENERAL = 420,
        JUMPING_AIR = 421,
        JUMPING_FINISHING_JUMP = 422,
    }
    public enum VehicleWeapons
    {
        NO_WEAPON = 0,
        TANK_CANNON = 1945616459,
        TECNICAL_GUN = 2144528907,
        INSURGENT_GUN = 1155224728,
        EXPLOSIVE_GUN = 1097917585,
        DOUBLE_MINIGUN_HELI = 1186503822,
        SINGLE_MINIGUN_HELI = 1638077257,
        SINGLE_MINIGUN_PLANE = -494786007,
        MISSILE_PLANE = -821520672,
        MISSILE = -123497569,

    }

    public static VehicleWeapons GetCurrentVehicleWeapon(Ped ped)
    {
        var weapon = new OutputArgument();
        Function.Call(Hash.GET_CURRENT_PED_VEHICLE_WEAPON, ped, weapon);
        return weapon.GetResult<VehicleWeapons>();
    }
    public enum DrivingFlags
    {
        STOP_AT_VEHS = 1,
        STOP_AT_PEDS = 2,
        AVOID_VEHS = 4,
        AVOID_EMPTY_VEHS = 8,
        AVOID_PEDS = 16,
        AVOID_OBJS = 32,
        STOP_AT_TRAFFIC_LIGHTS = 128,
        USE_BLINKERS = 256,
        GO_IN_REVERSE = 1024,
        TAKE_SHORTEST_PATH = 262144,
        USE_LOCAL_PATHING = 4194304,
        IGNORE_PATHING = 16777216,
    }
    public enum WeaponDamageType
    {
        UNKNOWN = 0, NO_DAMAGE = 1, MELEE = 2, BULLET = 3, EXPLOSIVE = 5, FIRE = 6, ELECTRIC = 10, BARBED_WIRE = 11, EXTINGUISHER = 12, GAS = 13, WATER_CANNON = 14,
    }


    public static bool IsSubttaskActive(Ped ped, Subtask task)
    {
        return Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, ped, (int)task);
    }

    public static VehicleSeat GetEmptyBackseat(Vehicle veh)
    {
        Ped ped = veh.GetPedOnSeat(VehicleSeat.RightRear);
        if (!Util.CanWeUse(ped)) return VehicleSeat.RightRear;
        ped = veh.GetPedOnSeat(VehicleSeat.LeftRear);
        if (!Util.CanWeUse(ped)) return VehicleSeat.LeftRear;

        return VehicleSeat.Passenger;
    }
    public static bool AnyBackseatEmpty(Vehicle veh)
    {
        Ped ped = veh.GetPedOnSeat(VehicleSeat.RightRear);
        if (!Util.CanWeUse(ped) || Util.IsCop(ped)) return true;
        ped = veh.GetPedOnSeat(VehicleSeat.LeftRear);
        if (!Util.CanWeUse(ped) || Util.IsCop(ped)) return true;

        return false;
    }



    /*
    string TranslateAreaHash(int hash)
    {
        if (hash == Game.GenerateHash("city")) return "city";
        if (hash == Game.GenerateHash("countryside")) return "countryside";
        if (hash == Game.GenerateHash("ocean")) return "ocean";
        return "unknown";
    }*/
    public static string GetMapAreaAtCoords(Vector3 pos)
    {
        int MapArea;
        MapArea = Function.Call<int>(Hash.GET_HASH_OF_MAP_AREA_AT_COORDS, pos.X, pos.Y, pos.Z);
        if (MapArea == Game.GenerateHash("city")) return "city";
        if (MapArea == Game.GenerateHash("countryside")) return "countryside";
        return MapArea.ToString();
    }



    //Police models
    public static List<string> LSSPDModels = new List<string>
    {
"s_m_y_sheriff_01",
    };
    public static List<string> SWATModels = new List<string>
    {
"s_m_y_swat_01",
    };
    public static List<string> LSPDModels = new List<string>
    {
"s_m_y_cop_01",
    };
    //POLICE CARS
    public static List<string> LSPDCars = new List<string>
    {
"POLICE",
"POLICE2",
"POLICE3",
    };
    public static List<string> LSSPDCars = new List<string>
    {
"POLICE",
"POLICE2",
"POLICE3",
    };
    public static List<string> BCSOCars = new List<string>
    {
"SHERIFF",
    };
    public static List<string> SWATCars = new List<string>
    {
"RIOT",
    };
    public static List<string> LSPDHelis = new List<string>
    {
"polmav",
    };
    public static List<string> NOoSEHelis = new List<string>
    {
"annihilator",
    };



    public static Vector3 GetClosestLocation(Vector3 pos, List<Vector3> list)
    {
        Vector3 finalpos = list[0];
        foreach (Vector3 position in list)
        {
            if (position.DistanceTo(pos) < finalpos.DistanceTo(pos)) finalpos = position;
        }
        return finalpos;
    }
    //WEAPON TYPES FOR NPCS
    public static List<WeaponHash> ThugGuns = new List<WeaponHash>
    {
        WeaponHash.Pistol,WeaponHash.CombatPistol,WeaponHash.SNSPistol,
        WeaponHash.MicroSMG,
    };
    public static List<WeaponHash> AverageGuns = new List<WeaponHash>
    {
        WeaponHash.Pistol50,WeaponHash.HeavyPistol,WeaponHash.VintagePistol,WeaponHash.APPistol,WeaponHash.Revolver,
        WeaponHash.SMG,WeaponHash.PumpShotgun, WeaponHash.SawnOffShotgun,WeaponHash.BullpupShotgun,
        WeaponHash.AssaultRifle,WeaponHash.CarbineRifle,WeaponHash.Gusenberg
    };
    public static List<WeaponHash> ProGuns = new List<WeaponHash>
    {
        WeaponHash.AssaultSMG,WeaponHash.AssaultShotgun,WeaponHash.HeavyShotgun,
        WeaponHash.AdvancedRifle,WeaponHash.SpecialCarbine,WeaponHash.BullpupRifle,
           };
    public static List<WeaponHash> HeavyGuns = new List<WeaponHash>
    {
        WeaponHash.MG,WeaponHash.CombatMG,WeaponHash.RPG,WeaponHash.GrenadeLauncher,WeaponHash.Minigun,
        WeaponHash.Railgun,WeaponHash.HomingLauncher
    };

    public static List<Model> OffroadCapableVehicles = new List<Model>
    {
        VehicleHash.Sanchez,VehicleHash.Sanchez2,VehicleHash.Sandking,VehicleHash.Sandking2,VehicleHash.Enduro,VehicleHash.BfInjection,
        VehicleHash.Bifta,VehicleHash.BJXL,VehicleHash.BobcatXL,VehicleHash.Bodhi2,VehicleHash.Brawler,VehicleHash.Dubsta3,VehicleHash.Guardian,
        VehicleHash.Insurgent,VehicleHash.Insurgent2,VehicleHash.Kalahari,VehicleHash.Monster,VehicleHash.Patriot,VehicleHash.RancherXL,VehicleHash.RancherXL2,
        VehicleHash.Technical,VehicleHash.Rebel,VehicleHash.Rebel2,VehicleHash.Dune,VehicleHash.Dune2,VehicleHash.Blazer,VehicleHash.Blazer2,VehicleHash.Blazer3
    };

    public static void DrawLine(Vector3 from, Vector3 to)
    {
        Function.Call(Hash.DRAW_LINE, from.X, from.Y, from.Z, to.X, to.Y, to.Z, 255, 255, 0, 255);
    }

    public static string GetImmersiveCriminalStatus(SuspectHandler suspect)
    {
        switch (suspect.State)
        {
            case CriminalState.Fleeing_Vehicle:
                {
                    return "Fleeing ("+suspect.VehicleChosen.FriendlyName+",  "+(suspect.Partners.Count+1)+" occupants)";
                }
            case CriminalState.Fleeing_Foot:
                {
                    return "Fleeing on foot";
                }
            case CriminalState.FightingCops:
                {
                    return "Shootout";
                }
        }

        return "";
    }
    public static void HandleVehicleCarefulness(CopUnitHandler unit)
    {
        if (!Game.IsPaused)
        {
            Vehicle veh = unit.CopVehicle;
            if (CanWeUse(veh) && CanWeUse(veh.GetPedOnSeat(VehicleSeat.Driver)) && veh.GetPedOnSeat(VehicleSeat.Driver).IsAlive && veh.Speed > 3f)
            {
                float speed = 0;
                RaycastResult ray = World.RaycastCapsule(veh.Position, veh.ForwardVector, 20f, 5f, IntersectOptions.Everything, veh);
                if (ray.DitHitEntity && ray.HitEntity.Velocity.Length() > 0f)
                {

                    //Disable allow suspect raming
                    if (unit.Suspect != null && unit.Suspect.Auth_Ramming && CanWeUse(unit.Suspect.VehicleChosen) && unit.Suspect.VehicleChosen == ray.HitEntity) return;

                    //DrawLine(veh.Position, ray.HitCoords);
                    speed = ray.HitEntity.Velocity.Length();
                    float distance = ray.HitCoords.DistanceTo(veh.Position);
                    float relativespeed = 3f;
                    if (distance > 10f)
                    {
                        relativespeed = 3f;
                    }
                    else
                    {
                        relativespeed = -3f;
                    }
                    if (veh.Speed > speed + relativespeed && AnyVehicleNear(veh.Position + (veh.ForwardVector * 10), 9))
                    {
                        /*
                        veh.BrakeLightsOn = true;
                        Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
                        //veh.EngineTorqueMultiplier = -100;ç
                        */
                        unit.BrakeTime = Game.GameTime + 500;
                    }
                }
                if (CanWeUse(Game.Player.Character.CurrentVehicle) && IsVehicleBehindVehicle(Game.Player.Character.CurrentVehicle, veh, true, 0f, 30f))
                {
                    //DrawLine(veh.Position, Game.Player.Character.Position);

                    if ((ForwardSpeed(veh) > ForwardSpeed(Game.Player.Character.CurrentVehicle) - 3f) || veh.IsTouching(Game.Player.Character.CurrentVehicle))
                    {
                        unit.BrakeTime = Game.GameTime + 500;

                        //Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
                    }
                }
            }
        }
    }
    public static bool IsOffroadCapable(Vehicle veh)
    {
        if (OffroadCapableVehicles.Contains(veh.Model)) return true;
        return false;
    }

    public static void VehicleAttackEntity(Ped attacker, Entity attacked)
    {
        if (CanWeUse(attacker) && CanWeUse(attacker.CurrentVehicle))
        {
            //if(attacker.we)

        }
    }
    public static void HandleVehicleCarefulnessArea(CopUnitHandler unit, float maxSpeed)
    {
        if (!Game.IsPaused)
        {
            Vehicle veh = unit.CopVehicle;
            if (CanWeUse(veh) && CanWeUse(veh.GetPedOnSeat(VehicleSeat.Driver)) && veh.GetPedOnSeat(VehicleSeat.Driver).IsAlive && veh.IsOnAllWheels)
            {
                Vector3 area = veh.Position + (veh.ForwardVector * (veh.Model.GetDimensions().Y * 4));
                GTA.Vehicle[] vehs = World.GetNearbyVehicles(area, veh.Model.GetDimensions().Y);
                if (vehs.Length > 0)
                {
                    Vehicle TargetVeh = vehs[0];
                    if (CanWeUse(TargetVeh))
                    {
                        //DrawLine(veh.Position, TargetVeh.Position);
                        if (TargetVeh.Speed > 4f)
                        {
                            if (veh.Speed > TargetVeh.Speed + maxSpeed)
                            {
                                /*veh.BrakeLightsOn = true;
                                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
                                */
                                unit.BrakeTime = Game.GameTime + 500;

                            }
                        }
                        else
                        {
                            //DrawLine(veh.Position, TargetVeh.Position);

                            if (veh.Speed > 10f)
                            {
                                unit.BrakeTime = Game.GameTime + 500;
                                /*
                                veh.BrakeLightsOn = true;
                                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
                                */
                            }
                        }
                    }
                }
            }
        }
    }
    
    public static void HandleAntiRamSystem(Vehicle offender, Vehicle offended)
    {
        if (!Game.IsPaused)
        {
            if (offender.IsInRangeOf(offended.Position, 20f) && ForwardSpeed(offender) > ForwardSpeed(offended) - 2f)
            {
                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, offender, 3, 0f, -0.5f, 0f, 0f, 0f, -0.2f, 0, true, true, true, true, true);
                offender.BrakeLightsOn = true;
            }
        }
    }

    public static bool AnyVehicleNear(Vector3 point, float radius)
    {
        return Function.Call<bool>(Hash.IS_ANY_VEHICLE_NEAR_POINT, point.X, point.Y, point.Z, radius);
    }
    public static bool IsInVehicle(Ped ped, VehicleSeat seat)
    {
        if (CanWeUse(ped.CurrentVehicle))
        {
            if (seat == VehicleSeat.Any) return true;
            else if (ped == ped.CurrentVehicle.GetPedOnSeat(seat)) return true;
        }
        return false;
    }
    public static bool IsInVehicle(Ped ped, Vehicle veh, VehicleSeat seat)
    {
        if (CanWeUse(ped.CurrentVehicle) && CanWeUse(veh))
        {
            if (ped.CurrentVehicle.Handle == veh.Handle)
            {
                if (seat == VehicleSeat.Any) return true;
                else if (ped == ped.CurrentVehicle.GetPedOnSeat(seat)) return true;
            }
        }
        return false;
    }

    public static int GetPedVehicleWeapon(Ped ped)
    {
        OutputArgument weapon = new OutputArgument();
        Function.Call(Hash.GET_CURRENT_PED_VEHICLE_WEAPON, ped, weapon);
        return weapon.GetResult<int>();
    }

    public static bool IsIdle(Ped ped)
    {
        List<uint> tasks = new List<uint> { 0x49bef36e, 0x6134071b, 0xa573b67c, 0xf09b15b3, 0xb41f1a34, 0x2288a57c, 0x370bcf53, 0x21d33957, 0x93a5526e, 0xf09b15b3 };
        //foreach (uint task in tasks) if (Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, ped,task) != 7) return false;
        if (Function.Call<int>(Hash.GET_SCRIPT_TASK_STATUS, ped.Handle, 0xf09b15b3) == 7) return true;
        return false;
    }

    /*
    2 = getting out of vehicle
    4 = aiming
    */
    public static bool IsInVehicle(Ped ped, Vehicle veh)
    {
        if (CanWeUse(veh) && CanWeUse(ped.CurrentVehicle) && ped.CurrentVehicle.Handle == veh.Handle) return true;
        return false;
    }

    public static List<int> GetRandomWeaponsFromList(int number, List<WeaponHash> list)
    {
        List<int> numberlist = new List<int>();
        while (numberlist.Count < number)
        {
            int random = RandomInt(0, list.Count - 1);
            if (!numberlist.Contains(random)) numberlist.Add(random);
        }
        return numberlist;
    }
    public static float ForwardSpeed(Entity ent)
    {
        return Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, ent, true).Y;
    }
    public static Model GetRandomVehicleFromList(List<Model> list)
    {
        Model modelo= list[RandomInt(0, list.Count - 1)];
        if (modelo.IsValid)
        {
            return modelo;
        }
        else
        {
            WarnPlayer(DangerousIndividuals.ScriptName, "Vehicle not found", modelo.ToString() + " not found.~n~Cars in list: " + list.Count);
        }
        return null;
    }
    public static Vehicle LookForGetawayVehicles(Vector3 pos, float radius, int minPassengerSeats, bool FastVehs, bool HeavyVehs, bool PoliceVehs, bool Empty, bool Occupied)
    {
        List<Vehicle> Candidates = new List<Vehicle>();
        bool VehiclesNearby = false;

        //Is Any Vehicle nearby
        foreach (Vehicle veh in World.GetNearbyVehicles(pos, radius))
        {
            if (veh.Speed < 1f) VehiclesNearby = true;
            break;
        }

        //If any vehicle nearby, look for vehicles farther (so the preferences thing works)
        if (VehiclesNearby)
        {
            foreach (Vehicle veh in World.GetNearbyVehicles(pos, radius * 2))
            {
                if ((veh.Model.IsCar || veh.Model.IsBike) && veh.PassengerSeats >= minPassengerSeats && veh.Speed < 2f && veh.IsAlive && !veh.IsOnFire && veh.IsDriveable && veh.Health > 700 && veh.IsOnAllWheels && !IsPlayerOrCopNearby(veh.Position, 30f))
                {
                    Ped driver = veh.GetPedOnSeat(VehicleSeat.Driver);
                    if (((Occupied && CanWeUse(driver)) || (Empty && !CanWeUse(driver)))) if (!IsPoliceVehicle(veh) || PoliceVehs) Candidates.Add(veh);
                }
            }
        }

        //Filter by preferences
        foreach (Vehicle veh in Candidates)
        {
            if (FastVehs)
            {
                if (veh.ClassType == VehicleClass.Super) return veh;
                if (veh.ClassType == VehicleClass.Sports) return veh;
                if (veh.ClassType == VehicleClass.SportsClassics) return veh;
            }
            if (HeavyVehs)
            {
                if (IsBig(veh) || veh.ClassType == VehicleClass.SUVs || veh.ClassType == VehicleClass.Vans) return veh;
            }
            if (PoliceVehs && IsPoliceVehicle(veh)) return veh;
        }
        if (Candidates.Count > 0) return Candidates[RandomInt(0, Candidates.Count - 1)]; else return null;
    }


    public static bool IsBig(Vehicle veh)
    {
        return Function.Call<bool>(Hash.IS_BIG_VEHICLE, veh);
    }

    public static List<String> MessageQueue = new List<String>();
    public static int MessageQueueInterval = 8000;
    public static int MessageQueueReferenceTime = 0;
    public static void HandleMessages()
    {
        if (MessageQueue.Count > 0)
        {
            DisplayHelpTextThisFrame(MessageQueue[0]);
        }
        else
        {
            MessageQueueReferenceTime = Game.GameTime;
        }
        if (Game.GameTime > MessageQueueReferenceTime + MessageQueueInterval)
        {
            if (MessageQueue.Count > 0)
            {
                MessageQueue.RemoveAt(0);
            }
            MessageQueueReferenceTime = Game.GameTime;
        }
    }
    public static void AddQueuedHelpText(string text)
    {
        if (!MessageQueue.Contains(text)) MessageQueue.Add(text);
    }

    public static void ClearAllHelpText(string text)
    {
        MessageQueue.Clear();
    }


    public static List<String> NotificationQueueText = new List<String>();
    public static List<String> NotificationQueueAvatar = new List<String>();
    public static List<String> NotificationQueueAuthor = new List<String>();
    public static List<String> NotificationQueueTitle = new List<String>();

    public static int NotificationQueueInterval = 8000;
    public static int NotificationQueueReferenceTime = 0;
    public static void HandleNotifications()
    {
        if (Game.GameTime > NotificationQueueReferenceTime)
        {

            if (NotificationQueueAvatar.Count > 0 && NotificationQueueText.Count > 0 && NotificationQueueAuthor.Count > 0 && NotificationQueueTitle.Count > 0)
            {
                NotificationQueueReferenceTime = Game.GameTime + ((NotificationQueueText[0].Length / 10) * 1000);
                Notify(NotificationQueueAvatar[0], NotificationQueueAuthor[0], NotificationQueueTitle[0], NotificationQueueText[0]);
                NotificationQueueText.RemoveAt(0);
                NotificationQueueAvatar.RemoveAt(0);
                NotificationQueueAuthor.RemoveAt(0);
                NotificationQueueTitle.RemoveAt(0);
            }
        }
    }

    public static void AddNotification(string avatar, string author, string title, string text)
    {
        NotificationQueueText.Add(text);
        NotificationQueueAvatar.Add(avatar);
        NotificationQueueAuthor.Add(author);
        NotificationQueueTitle.Add(title);
    }
    public static void CleanNotifications()
    {
        NotificationQueueText.Clear();
        NotificationQueueAvatar.Clear();
        NotificationQueueAuthor.Clear();
        NotificationQueueTitle.Clear();
        NotificationQueueReferenceTime = Game.GameTime;
        Function.Call(Hash._REMOVE_NOTIFICATION, CurrentNotification);
    }

    public static int CurrentNotification;
    public static void Notify(string avatar, string author, string title, string message)
    {
        if (avatar != "" && author != "" && title != "")
        {
            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
            CurrentNotification = Function.Call<int>(Hash._SET_NOTIFICATION_MESSAGE, avatar, avatar, true, 0, title, author);
        }
        else
        {
            UI.Notify(message);
        }
    }

    public static bool CanPedSeePed(Ped watcher, Ped target, bool WatcherIsPlayer)
    {
        RaycastResult ray;
        if (WatcherIsPlayer)
        {
            if (watcher.IsInRangeOf(target.Position, 10f)) return true;
            ray = World.Raycast(GameplayCamera.Position, target.Position, Game.Player.Character.Position.DistanceTo(target.Position), IntersectOptions.Map, Game.Player.Character);
        }
        else
        {
            ray = World.Raycast(watcher.Position, target.Position, IntersectOptions.Map);
        }
        if (!ray.DitHitAnything || ray.HitCoords.DistanceTo(target.Position) < 10f) return true;
        return false;
    }

    public static bool CanPlayerdSeePed(Ped target, bool CheckCamera)
    {
        Ped watcher = Game.Player.Character;
        if (CheckCamera && !target.IsOnScreen) return false;
        else
        {
            RaycastResult ray = World.Raycast(GameplayCamera.Position, target.Position, GameplayCamera.Position.DistanceTo(target.Position), IntersectOptions.Map, Game.Player.Character);
            if (!ray.DitHitAnything || ray.HitCoords.DistanceTo(target.Position) < 10f) return true;
        }
        return false;
    }

    public static float RoadTravelDistance(Vector3 pos, Vector3 destination)
    {
        return Function.Call<float>(Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS, pos.X, pos.Y, pos.Z, destination.X, destination.Y, destination.Z);
    }

    public static Vector3 GetRandomPosAroundPlayer(float radius, bool road)
    {
        Vector3 pos;
        if (road) pos = World.GetNextPositionOnStreet(Game.Player.Character.Position.Around(radius), true); else pos = World.GetNextPositionOnSidewalk(Game.Player.Character.Position.Around(radius));
        if (pos.DistanceTo(Game.Player.Character.Position) > radius || pos == Vector3.Zero)
        {
            if (road) pos = World.GetNextPositionOnStreet(pos, true); else pos = World.GetNextPositionOnSidewalk(pos);
        }
        return pos;
    }
    public enum Nodetype { AnyRoad, Road, Offroad,LocalPathing, Water }

    public static Vector3 GenerateSpawnPos(Vector3 desiredPos, Nodetype roadtype, bool sidewalk)
    {

        Vector3 finalpos = Vector3.Zero;
        bool ForceOffroad = false;


        OutputArgument outArgA = new OutputArgument();
        int NodeNumber = 1;
        int type = 0;

        if (roadtype == Nodetype.AnyRoad) type = 1;
        if (roadtype == Nodetype.Road) type = 0;
        if (roadtype == Nodetype.Offroad) { type = 1; ForceOffroad = true; }
        if (roadtype == Nodetype.Water) type = 3;

        if (roadtype == Nodetype.LocalPathing)
        {
            int patience = 0;
            while (patience < 30 && finalpos == Vector3.Zero)
            {
                patience++;
                finalpos = World.GetSafeCoordForPed(desiredPos.Around(patience * 2));
            }
        }
        else
        {
            int patience = 0;
            while (patience < 30 && finalpos == Vector3.Zero)
            {
                patience++;
                int NodeID = Function.Call<int>(Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID, desiredPos.X, desiredPos.Y, desiredPos.Z, NodeNumber, type, 300f, 300f);
                if (ForceOffroad)
                {
                    while (!Function.Call<bool>(Hash._GET_IS_SLOW_ROAD_FLAG, NodeID) && NodeNumber < 500)
                    {
                        NodeNumber++;
                        NodeID = Function.Call<int>(Hash.GET_NTH_CLOSEST_VEHICLE_NODE_ID, desiredPos.X, desiredPos.Y, desiredPos.Z, NodeNumber + 5, type, 300f, 300f);
                    }
                }
                Function.Call(Hash.GET_VEHICLE_NODE_POSITION, NodeID, outArgA);
                finalpos = outArgA.GetResult<Vector3>();
            }
        }
        

        //UI.Notify("Tries: " + patience.ToString() + "~n~Desired: " + desiredPos.ToString() + "~n~Final: " + finalpos.ToString());

        if (sidewalk) finalpos = World.GetNextPositionOnSidewalk(finalpos);
        return finalpos;
    }
    public static bool IsOnRoad(Vector3 pos, Vehicle v)
    {
        return Function.Call<bool>(Hash.IS_POINT_ON_ROAD, pos.X, pos.Y, pos.Z, v);
    }
    public static bool IsDriving(Ped ped)
    {
        return (Util.IsSubttaskActive(ped, Util.Subtask.DRIVING_WANDERING) || Util.IsSubttaskActive(ped, Util.Subtask.DRIVING_GOING_TO_DESTINATION_OR_ESCORTING));
    }
    public static bool IsPlayingAnim(Ped ped, string animDict, string AnimName)
    {
        return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, animDict, AnimName, 3);
    }
    public static bool IsInNorthYankton(Ped ped)
    {
        return ped.IsInRangeOf(new Vector3(5000, -5000, 100), 1000);
    }
    public static Vector3 GetClosestRoad(Vector3 pos)
    {

        Vector3 finalpos = Vector3.Zero;

        OutputArgument outArgA = new OutputArgument();
        OutputArgument outArgB = new OutputArgument();
        OutputArgument outArgC = new OutputArgument();

        // UI.Notify(Function.Call<bool>(Hash.GET_RANDOM_VEHICLE_NODE, pos.X, pos.Y, pos.Z, 200f, true, true, true, outArgA, outArgB).ToString());

        finalpos = outArgA.GetResult<Vector3>();
        return finalpos;
    }


    public static void MoveEntitytoNearestRoad(Entity E)
    {
        if (CanWeUse(E))
        {
            OutputArgument outArgA = new OutputArgument();
            OutputArgument outArgB = new OutputArgument();


            if (Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, E.Position.X, E.Position.Y, E.Position.Z, outArgA, outArgB, 0, 1077936128, 0))
            {
                Vector3 pos = outArgA.GetResult<Vector3>();
                if (CanWeUse(World.GetClosestVehicle(pos, 5f))) pos = pos.Around(5f);
                if (E.Model.IsHelicopter && E.HeightAboveGround < 40) pos = pos + new Vector3(0, 0, 50);
                E.Position = pos;
                E.Heading = outArgB.GetResult<float>();
            }
        }
    }

    public static void WarnPlayer(string script_name, string title, string message)
    {
        Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
        Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
        Function.Call(Hash._SET_NOTIFICATION_MESSAGE, "CHAR_SOCIAL_CLUB", "CHAR_SOCIAL_CLUB", true, 0, title, "~b~" + script_name);
    }

    public static bool CanWeUse(Entity entity)
    {
        return entity != null && entity.Exists();
    }
    public static void SetRelationshipBetweenGroups(int Group1, int Group2, Relationship relationship, bool ApplyToBoth)
    {
        World.SetRelationshipBetweenGroups(relationship, Group1, Group2);
        if (ApplyToBoth) World.SetRelationshipBetweenGroups(relationship, Group2, Group1);
    }
    public static void DisplayHelpTextThisFrame(string text)
    {
        Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
        Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, text);
        Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, false, false, -1);
    }
    public static void PreparePed(Ped ped, int health, int accuracy, int armor, float DrvAbility, WeaponHash weapon, WeaponHash weapon2, WeaponHash weapon3)
    {
        Function.Call(GTA.Native.Hash.SET_PED_TO_INFORM_RESPECTED_FRIENDS, ped, 20f, 20);

        Function.Call(GTA.Native.Hash.SET_PED_COMBAT_MOVEMENT, ped, 2);

        Function.Call(GTA.Native.Hash.SET_DRIVER_ABILITY, ped, DrvAbility);
        Function.Call(GTA.Native.Hash.SET_PED_SEEING_RANGE, ped, 100f);
        Function.Call(GTA.Native.Hash.SET_PED_HEARING_RANGE, ped, 30f);

        ped.MaxHealth = health;
        ped.Health = health;
        ped.AlwaysDiesOnLowHealth = false;
        ped.Accuracy = accuracy;
        ped.Armor = armor;
        ped.AlwaysKeepTask = true;
        ped.CanSwitchWeapons = true;
        ped.Weapons.Give(weapon, -1, false, true);
        ped.Weapons.Give(weapon2, -1, false, true);
        ped.Weapons.Give(weapon3, -1, false, true);

    }
    public static void GetSquadIntoVehicle(List<Ped> Group, Vehicle Vehicle)
    {
        if (Group.Count == 0) return;

        //int max_seats = GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, Vehicle);
        foreach (Ped ped in Group)
        {
            if (!ped.IsSittingInVehicle(Vehicle))
            {
                for (int i = -1; i < Group.Count; i++)
                {
                    if (Function.Call<bool>(Hash.IS_VEHICLE_SEAT_FREE, Vehicle, -2))
                    {
                        ped.SetIntoVehicle(Vehicle, (VehicleSeat)(-2));
                    }
                }
            }
        }
    }

    public static bool IsAreaPopulated(Vector3 pos, float area, int maxpeds, bool CheckForCops)
    {
        int population = 0;
        foreach (Ped ped in World.GetNearbyPeds(pos, area))
        {
            if (!ped.IsPlayer && (CheckForCops && IsCop(ped) || !CheckForCops)) population++;
        }
        if (population > maxpeds) return true;

        return false;
    }

    public static bool AnyTireBlown(Vehicle veh)
    {
        for (int i = 0; i < 20; i++) if (veh.IsTireBurst(i)) return true;
        return false;
    }

    public static bool IsCop(Ped ped)
    {
        Vector3 pedpos = ped.Position;
        float radius = 0.5f;
        if (ped.IsSittingInVehicle() && CanWeUse(ped.CurrentVehicle)) radius = (ped.CurrentVehicle.Model.GetDimensions().X) / 2;
        return Function.Call<bool>(Hash.IS_COP_PED_IN_AREA_3D, pedpos.X + radius, pedpos.Y + radius, pedpos.Z + radius, pedpos.X - radius, pedpos.Y - radius, ped.Position.Z - radius);
    }
    public static bool IsPoliceVehicle(Vehicle veh)
    {
        Vector3 vehpos = veh.Position;
        float radius = veh.Model.GetDimensions().Y;
        return Function.Call<bool>(Hash.IS_COP_VEHICLE_IN_AREA_3D, vehpos.X + radius, vehpos.Y + radius, vehpos.Z + radius, vehpos.X - radius, vehpos.Y - radius, vehpos.Z - radius);
    }

    public static bool AnyCopNear(Vector3 pos, float radius)
    {
        foreach (Ped cop in World.GetAllPeds())
        {
            if (cop.IsAlive && (Function.Call<int>(Hash.GET_PED_TYPE, cop) == 6 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 27 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 29))
            {
                if (CanWeUse(cop.CurrentVehicle)) if (cop.CurrentVehicle.Model.IsHelicopter && cop.IsInRangeOf(pos, radius * 4)) return true;
                if (cop.IsInRangeOf(pos, radius)) return true;
            }
        }
        return false;
    }

    public static int NumberOfCopsNearby(Vector3 pos, float radius)
    {
        int Cops = 0;
        foreach (Ped cop in World.GetAllPeds())
        {
            if (cop.IsAlive && cop.IsInRangeOf(pos, radius) && (Function.Call<int>(Hash.GET_PED_TYPE, cop) == 6 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 27 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 29))
            {
                Cops++;
                if (Cops == 1) radius += radius;
            }
        }
        return Cops;
    }
    public static bool IsCopNearby(Vector3 pos, float radius)
    {
        foreach (Ped cop in World.GetNearbyPeds(pos, radius))
        {
            if (cop.IsAlive && (Function.Call<int>(Hash.GET_PED_TYPE, cop) == 6 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 27 || Function.Call<int>(Hash.GET_PED_TYPE, cop) == 29)) return true;
        }
        return false;
    }

    public enum StandoffStatus { NotNeccesary, Yes, No }
    public static StandoffStatus CanStandoffAgainstSurroundingCops(Ped ped, float radius, int max)
    {
        if (max == 0) return StandoffStatus.NotNeccesary;
        int cops = 0;
        foreach (Ped cop in World.GetNearbyPeds(ped.Position, radius))
        {
            if (cop.IsPlayer) cops++;
            if (cop.IsAlive && IsCop(cop) && !cop.IsPlayer) cops++;
            if (cops > max) return StandoffStatus.No;
        }
        if (cops > 0 && cops <= max) return StandoffStatus.Yes;
        return StandoffStatus.NotNeccesary;
    }

    public static bool IsPlayerOrCopNearby(Vector3 pos, float radius)
    {
        if (Game.Player.Character.IsInRangeOf(pos, radius)) return true;
        foreach (Ped cop in World.GetNearbyPeds(pos, radius))
        {
            if (cop.IsAlive && IsCop(cop)) return true;
        }
        return false;
    }

    public static int RandomInt(int min, int max)
    {
        max++;
        return Function.Call<int>(Hash.GET_RANDOM_INT_IN_RANGE, min, max);
    }
    public static Model GetRandomVehicleHash()
    {
        foreach (Vehicle veh in World.GetAllVehicles())
        {
            if ((veh.Model.IsCar || veh.Model.IsBike) && !IsPoliceVehicle(veh)) return veh.Model;
        }
        return "blista";
    }
    public static bool IsMasked(Ped ped)
    {
        List<int> MasksFranklin = new List<int> { 8, 9, 10, 11, 12, 13, 14 };
        List<int> MasksMichael = new List<int> { 14, 15, 16, 17, 18, 19, 20 };
        List<int> MasksTrevor = new List<int> { 14, 15, 16, 17, 18, 19, 20 };

        Model pedmodel = ped.Model;
        if ((pedmodel == "mp_f_freemode_01" || pedmodel == "mp_m_freemode_01") && GTA.Native.Function.Call<int>(GTA.Native.Hash.GET_PED_DRAWABLE_VARIATION, Game.Player.Character, 1) != 0) return true;
        if (pedmodel == PedHash.Franklin && MasksFranklin.Contains(Function.Call<int>(Hash.GET_PED_PROP_INDEX, ped, 0))) return true;
        if (pedmodel == PedHash.Michael && MasksMichael.Contains(Function.Call<int>(Hash.GET_PED_PROP_INDEX, ped, 0))) return true;
        if (pedmodel == PedHash.Trevor && MasksTrevor.Contains(Function.Call<int>(Hash.GET_PED_PROP_INDEX, ped, 0))) return true;
        return false;
    }

    public static bool AreStarsGreyedOut()
    {
        return Function.Call<bool>(Hash.ARE_PLAYER_STARS_GREYED_OUT, Game.Player);
    }
    public static bool HasPlayerBeenRecentlyArrested(int MS_Threshold)
    {
        if (Function.Call<int>(Hash.GET_TIME_SINCE_LAST_ARREST) == -1) return false;
        return Function.Call<int>(Hash.GET_TIME_SINCE_LAST_ARREST) < MS_Threshold;
    }
    public static bool HasPlayeRecentlyDied(int MS_Threshold)
    {
        if (Function.Call<int>(Hash.GET_TIME_SINCE_LAST_DEATH) == -1) return false;
        return Function.Call<int>(Hash.GET_TIME_SINCE_LAST_DEATH) < MS_Threshold;
    }
    enum Cardinal { North, East, West, South }

    public static string GetWhereIsHeaded(Ped ped, bool Shortened)
    {

            float heading = ped.Heading;
            if (heading > 337.5f && heading < 360)
            {
                if (Shortened) return "N"; else return "North";
            }
            if (heading > 0 && heading < 22.5f)
            {
                if (Shortened) return "N"; else return "North";
            }

            if (heading > 22.5f && heading < 67.5f)
            {
                if (Shortened) return "NW"; else return "Nortwest";
            }
            if (heading > 67.5f && heading < 112.5f)
            {
                if (Shortened) return "W"; else return "West";
            }
            if (heading > 112.5f && heading < 157.5f)
            {
                if (Shortened) return "SW"; else return "Southwest";
            }
            if (heading > 157.5f && heading < 202.5f)
            {
                if (Shortened) return "S"; else return "South";
            }
            if (heading > 202.5f && heading < 247.5f)
            {
                if (Shortened) return "SE"; else return "Southeast";
            }
            if (heading > 247.5f && heading < 292.5f)
            {
                if (Shortened) return "E"; else return "East";
            }
            if (heading > 292.5f && heading < 337.5f)
            {
                if (Shortened) return "N"; else return "Northeast";
            }

        return "";

    }

    //WOULD CRASH IF NOT PROTECTED VEH SET
    public static void GenerateEMP(Vector3 pos, float radius, Vehicle protectedveh, bool forwardonly)
    {

        foreach (Vehicle vehicle in World.GetNearbyVehicles(pos, radius))
        {
            if (!CanWeUse(protectedveh) || protectedveh.Handle != vehicle.Handle)
            {
                if (!forwardonly || IsThisEntityAheadThatEntity(vehicle, protectedveh, 0f))
                {
                    Vector3 sparkpos = vehicle.Position + vehicle.ForwardVector * (vehicle.Model.GetDimensions().Y / 3);

                    Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "scr_mp_house");
                    Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, "scr_mp_house");
                    Function.Call<int>(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "scr_sh_lighter_sparks", sparkpos.X, sparkpos.Y, sparkpos.Z, 0f, 0f, 0f, 15.0f, false, false, false);

                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, vehicle, 3, 0f, 0.5f, 0f, 0f, 0f, vehicle.Model.GetDimensions().Y * 0.5f, 0, true, true, true, true);

                    //DrawLine(pos, vehicle.Position);
                    vehicle.EngineRunning = false;
                    vehicle.FuelLevel = 0;
                    vehicle.EngineHealth = 140;
                    Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, vehicle, true);
                }
            }
        }
    }
    public static bool IsBiggerThan(Entity ent1, Entity ent2)
    {
        return ent1.Model.GetDimensions().Length() > ent2.Model.GetDimensions().Length();
    }

    public static bool IsBiggerThan(Entity ent1, Entity ent2, float targetModifier)
    {
        return ent1.Model.GetDimensions().Length() > (ent2.Model.GetDimensions().Length()+ targetModifier);
    }


    public static bool IsThisEntityAheadThatEntity(Entity ent1, Entity ent2, float MinAheadDistance)
    {
        Vector3 pos = ent1.Position;
        return Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, ent2, pos.X, pos.Y, pos.Z).Y > MinAheadDistance;
    }
    public static bool IsVehicleBehindVehicle(Vehicle ent1, Vehicle ent2, bool precise, float minDistance, float maxDistance)
    {
        Vector3 pos = ent1.Position;
        if (precise)
        {
            Vector3 back = ent2.Position;
            Vector3 raycastend = ent2.ForwardVector * (maxDistance * -1);
            RaycastResult raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, ent2);

            if (raycast.DitHitEntity && raycast.HitEntity == ent1) return true;
        }
        else
        {
            float BehindDistance = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, ent2, pos.X, pos.Y, pos.Z).Y;
            if (BehindDistance > minDistance && BehindDistance < maxDistance) return true;
        }
        return false;
    }

    public enum Side { Left=-1,Right=1}

    public static Vehicle GetVehicleAtSide(Vehicle veh, Side sideRef)
    {
        Vector3 back = veh.Position;
        Vector3 raycastend=veh.RightVector * (int)sideRef;
        RaycastResult raycast = World.Raycast(back, raycastend, 10f, IntersectOptions.Everything, veh);
        if (raycast.DitHitEntity && raycast.HitEntity.Model.IsVehicle) return raycast.HitEntity as Vehicle;
        return null;
    }
    public static bool IsThisVehicleTotheSideOfThatVehicle(Vehicle ent1, Vehicle ent2, bool precise, float xOffset, float minDistance, float maxDistance)
    {
        Vector3 pos = ent1.Position;
        if (precise)
        {
            Vector3 back = ent2.Position;
            Vector3 raycastend = ent2.RightVector * xOffset;
            RaycastResult raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, ent2);
            if (raycast.DitHitEntity && raycast.HitEntity == ent1) return true;
        }
        else
        {
            float SideDistance = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, ent2, pos.X, pos.Y, pos.Z).X;
            if (xOffset > 0)
            {
                if (SideDistance > minDistance && SideDistance < maxDistance) return true;
            }
            else
            {
                if (SideDistance > -minDistance && SideDistance < -maxDistance) return true;
            }
        }
        return false;
    }


    //NEEDS TWEAKS
    public static Vehicle GetVehicleTotheSideOfThatVehicle(Vehicle veh, float xOffset, float maxDistance)
    {
        Vector3 back = veh.Position;
        Vector3 raycastend = veh.RightVector * xOffset;
        RaycastResult raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, veh);
        if (raycast.DitHitEntity && raycast.HitEntity.Model.IsVehicle) return raycast.HitEntity as Vehicle;

        return null;
    }


    public static Vehicle GetVehicleTotheSideOfThatVehicle(Vehicle veh, float maxDistance)
    {
        Vector3 back = veh.Position;
        Vector3 raycastend = veh.RightVector;
        RaycastResult raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, veh);
        if (raycast.DitHitEntity && raycast.HitEntity.Model.IsVehicle) return raycast.HitEntity as Vehicle;
        else
        {
            raycastend = veh.RightVector * -1;
            raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, veh);
            if (raycast.DitHitEntity && raycast.HitEntity.Model.IsVehicle) return raycast.HitEntity as Vehicle;
        }
        return null;
    }
    public static bool AreVehsGoingAtSimilarSpeeds(Vehicle veh1, Vehicle veh2, float speedtreshold)
    {
        return (veh1.Speed - veh2.Speed) < speedtreshold;
    }
    public static Vehicle GetVehicleBehindThatVehicle(Vehicle veh, float maxDistance)
    {
        Vector3 back = veh.Position;
        Vector3 raycastend = veh.ForwardVector * -1;
        RaycastResult raycast = World.Raycast(back, raycastend, maxDistance, IntersectOptions.Everything, veh);
        if (raycast.DitHitEntity && raycast.HitEntity.Model.IsVehicle) return raycast.HitEntity as Vehicle;
        return null;
    }

    public static bool IsThisEntityLeftToThatEntity(Entity ent1, Entity ent2)
    {
        Vector3 pos = ent1.Position;
        return Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, ent2, pos.X, pos.Y, pos.Z).X < 0;
    }
    public static Vehicle GetLastVehicle(Ped RecieveOrder)
    {
        Vehicle vehicle = Function.Call<Vehicle>(Hash.GET_VEHICLE_PED_IS_IN, RecieveOrder, false);
        if (CanWeUse(vehicle)) return vehicle;

        vehicle = Function.Call<Vehicle>(Hash.GET_VEHICLE_PED_IS_IN, RecieveOrder, true);
        if (CanWeUse(vehicle)) return vehicle;
        return null;
    }
    public static bool IsSliding(Vehicle veh, float threshold)
    {
        return Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_SPEED_VECTOR, veh, true).X) > threshold;
    }
    public static bool IsRaining()
    {
        int weather = Function.Call<int>(GTA.Native.Hash._0x564B884A05EC45A3); //get current weather hash
        switch (weather)
        {
            case (int)Weather.Blizzard:
                {
                    return true;
                }
            case (int)Weather.Clearing:
                {
                    return true;
                }
            case (int)Weather.Foggy:
                {
                    return true;
                }
            case (int)Weather.Raining:
                {
                    return true;
                }
            case (int)Weather.Neutral:
                {
                    return true;
                }
            case (int)Weather.ThunderStorm:
                {
                    return true;
                }
            case (int)Weather.Snowlight:
                {
                    return true;
                }
            case (int)Weather.Snowing:
                {
                    return true;
                }
            case (int)Weather.Christmas:
                {
                    return true;
                }
        }
        return false;
    }
    public static bool IsNightTime()
    {
        int hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        return (hour > 20 || hour < 7);
    }
    public static Vector3 FindSpawnpointInDirection(Vector3 pos, float distance, float radius)
    {
        int tries = 0;
        OutputArgument spawnpoint = new OutputArgument();
        while (spawnpoint.GetResult<Vector3>() == Vector3.Zero && tries < 5)
        {
            tries++;
            Function.Call<Vector3>(Hash.FIND_SPAWN_POINT_IN_DIRECTION, pos.X + (radius * tries), pos.Y + (radius * tries), pos.Z, pos.X - (radius * tries), pos.Y - (radius * tries), pos.Z, distance, spawnpoint);
        }
        Vector3 final_spawnpoint = spawnpoint.GetResult<Vector3>();
        return final_spawnpoint;
    }

    public static bool IsCarDrivable(Vehicle veh)
    {
        if (!CanWeUse(veh)) return false;
        if (veh.IsDead || !veh.IsDriveable) return false;
        if (veh.EngineHealth < 300)
        {
            return false;
        }
        if (veh.PetrolTankHealth < 650)
        {
            return false;
        }
        return true;
    }
    public static bool IsStuck(Vehicle veh)
    {
        if (veh.IsStopped && !veh.IsOnAllWheels) return true;
        return false;
    }

    public static bool IsEmpty(Vehicle veh)
    {
        if (!CanWeUse(veh.GetPedOnSeat(VehicleSeat.Driver)) || veh.GetPedOnSeat(VehicleSeat.Driver).IsDead) return true;
        return false;
    }
    public static bool IsBulletInArea(Vector3 pos, float radius)
    {
        return Function.Call<bool>(Hash.IS_BULLET_IN_AREA, pos.X, pos.Y, pos.Z, radius, false);
    }
    public static bool HasBulletImpactedInArea(Vector3 pos, float radius)
    {
        return Function.Call<bool>(Hash.HAS_BULLET_IMPACTED_IN_AREA, pos.X, pos.Y, pos.Z, radius, true, true);
    }
    public static bool IsAnyPedShootingInArea(Vector3 pos, float radius)
    {
        return Function.Call<bool>(Hash.IS_ANY_PED_SHOOTING_IN_AREA, pos.X + radius, pos.Y + radius, pos.Z + radius, pos.X - radius, pos.Y - radius, pos.Z - radius, true, true);
    }
    public static string SaveVehicleLayoutToFile(Vehicle veh, string filePath)
    {
        string error = "";
        if (!File.Exists(@"" + filePath))
        {
            File.WriteAllText(@"" + filePath, "<Data></Data>");
        }


        XmlDocument originalXml = new XmlDocument();
        originalXml.Load(@"" + filePath);

        XmlNode changes = originalXml.SelectSingleNode("//Data");


        XmlNode Data = originalXml.CreateNode(XmlNodeType.Element, "Vehicle", null);


        XmlElement Model = originalXml.CreateElement("Model");
        Model.InnerText = veh.Model.Hash.ToString();
        Data.AppendChild(Model);

        XmlElement Wheeltype = originalXml.CreateElement("WheelType");
        Wheeltype.InnerText = ((int)veh.WheelType).ToString();
        Data.AppendChild(Wheeltype);


        XmlElement TrimColor = originalXml.CreateElement("TrimColor");
        TrimColor.InnerText = ((int)veh.TrimColor).ToString();
        Data.AppendChild(TrimColor);

        XmlElement DashColor = originalXml.CreateElement("DashColor");
        DashColor.InnerText = ((int)veh.DashboardColor).ToString();
        Data.AppendChild(DashColor);

        XmlElement PrimaryColor = originalXml.CreateElement("PrimaryColor");
        PrimaryColor.InnerText = ((int)veh.PrimaryColor).ToString();
        Data.AppendChild(PrimaryColor);

        XmlElement SecondaryColor = originalXml.CreateElement("SecondaryColor");
        SecondaryColor.InnerText = ((int)veh.SecondaryColor).ToString();
        Data.AppendChild(SecondaryColor);

        XmlElement PearlescentColor = originalXml.CreateElement("PearlescentColor");
        PearlescentColor.InnerText = ((int)veh.PearlescentColor).ToString();
        Data.AppendChild(PearlescentColor);


        XmlElement RimColor = originalXml.CreateElement("RimColor");
        RimColor.InnerText = ((int)veh.RimColor).ToString();
        Data.AppendChild(RimColor);


        XmlElement LicensePlate = originalXml.CreateElement("LicensePlate");
        LicensePlate.InnerText = ((int)veh.NumberPlateType).ToString();
        Data.AppendChild(LicensePlate);

        XmlElement LicensePlateText = originalXml.CreateElement("LicensePlateText");
        LicensePlateText.InnerText = veh.NumberPlate;
        Data.AppendChild(LicensePlateText);




        XmlElement WindowsTint = originalXml.CreateElement("WindowsTint");
        WindowsTint.InnerText = ((int)veh.WindowTint).ToString();
        Data.AppendChild(WindowsTint);

        XmlElement Livery = originalXml.CreateElement("Livery");
        Livery.InnerText = ((int)veh.Livery).ToString();
        Data.AppendChild(Livery);


        XmlElement Components = originalXml.CreateElement("Components");

        for (int i = 0; i <= 25; i++)
        {
            XmlElement Component = originalXml.CreateElement("Component");

            XmlAttribute Attribute = originalXml.CreateAttribute("ComponentIndex");
            Attribute.InnerText = i.ToString();
            Component.Attributes.Append(Attribute);

            if (Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, veh, i))
            {
                Component.InnerText = "1";
            }
            else
            {
                Component.InnerText = "0";
            }
            Components.AppendChild(Component);
        }
        Data.AppendChild(Components);

        //SMOKE COLOR GOES HERE




        XmlElement ModToggles = originalXml.CreateElement("ModToggles");

        for (int i = 0; i <= 25; i++)
        {
            XmlElement Component = originalXml.CreateElement("Toggle");

            XmlAttribute Attribute = originalXml.CreateAttribute("ToggleIndex");
            Attribute.InnerText = i.ToString();
            Component.Attributes.Append(Attribute);

            if (Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, veh, i))
            {
                Component.InnerText = "true";
                ModToggles.AppendChild(Component);
            }

        }
        Data.AppendChild(ModToggles);




        XmlElement Mods = originalXml.CreateElement("Mods");
        for (int i = 0; i <= 500; i++)
        {
            XmlElement Component = originalXml.CreateElement("Mod");

            XmlAttribute Attribute = originalXml.CreateAttribute("ModIndex");
            Attribute.InnerText = i.ToString();
            Component.Attributes.Append(Attribute);

            if (Function.Call<int>(Hash.GET_VEHICLE_MOD, veh, i) != -1)
            {
                Component.InnerText = Function.Call<int>(Hash.GET_VEHICLE_MOD, veh, i).ToString();
                Mods.AppendChild(Component);
            }
        }
        Data.AppendChild(Mods);


        //CUSTOM TYRES GO HERE
        XmlElement CustomTires = originalXml.CreateElement("CustomTires");
        CustomTires.InnerText = "false";
        Data.AppendChild(CustomTires);


        //NEON COLORS GO HERE
        XmlElement Neons = originalXml.CreateElement("Neons");

        XmlElement NeonInfo = originalXml.CreateElement("Left");
        NeonInfo.InnerText = ((bool)veh.IsNeonLightsOn(VehicleNeonLight.Left)).ToString();
        Neons.AppendChild(NeonInfo);

        NeonInfo = originalXml.CreateElement("Right");
        NeonInfo.InnerText = ((bool)veh.IsNeonLightsOn(VehicleNeonLight.Right)).ToString();
        Neons.AppendChild(NeonInfo);

        NeonInfo = originalXml.CreateElement("Front");
        NeonInfo.InnerText = ((bool)veh.IsNeonLightsOn(VehicleNeonLight.Front)).ToString();
        Neons.AppendChild(NeonInfo);

        NeonInfo = originalXml.CreateElement("Back");
        NeonInfo.InnerText = ((bool)veh.IsNeonLightsOn(VehicleNeonLight.Back)).ToString();
        Neons.AppendChild(NeonInfo);

        Data.AppendChild(Neons);


        XmlElement NeonColor = originalXml.CreateElement("NeonColor");
        XmlElement Color = originalXml.CreateElement("R");
        Color.InnerText = veh.NeonLightsColor.R.ToString();
        NeonColor.AppendChild(Color);

        Color = originalXml.CreateElement("G");
        Color.InnerText = veh.NeonLightsColor.G.ToString();
        NeonColor.AppendChild(Color);

        Color = originalXml.CreateElement("B");
        Color.InnerText = veh.NeonLightsColor.B.ToString();
        NeonColor.AppendChild(Color);

        Data.AppendChild(NeonColor);


        changes.AppendChild(Data);

        originalXml.Save(@"" + filePath);

        return error;
    }

    public static string ApplyRandomVehicleLayoutFromFile(Vehicle veh, string filePath)
    {
        XmlDocument driversdoc = new XmlDocument();
        driversdoc.Load(@"" + filePath);
        if (driversdoc == null)
        {
            RandomTuning(veh);
            return filePath + "not found.";

        }

        XmlNodeList nodelist = driversdoc.SelectNodes("//Data/*");

        List<XmlElement> Layouts = new List<XmlElement>();

        XmlElement driver;
        foreach (XmlElement Layout in nodelist)
        {
            if (Layout.SelectSingleNode("Model").InnerText == veh.DisplayName || Layout.SelectSingleNode("Model").InnerText == veh.FriendlyName || Layout.SelectSingleNode("Model").InnerText == veh.Model.Hash.ToString()) Layouts.Add(Layout);
        }
        if (Layouts.Count > 0)
        {
            driver = Layouts[RandomInt(0, Layouts.Count - 1)];

            Function.Call(Hash.SET_VEHICLE_MOD_KIT, veh, 0);

            if (driver.SelectSingleNode("WheelType") != null)
            {
                veh.WheelType = (VehicleWheelType)int.Parse(driver.SelectSingleNode("WheelType").InnerText, CultureInfo.InvariantCulture);
            }

            if (driver.SelectSingleNode("TrimColor") != null)
            {
                Function.Call((Hash)0xF40DD601A65F7F19, veh, int.Parse(driver.SelectSingleNode("TrimColor").InnerText, CultureInfo.InvariantCulture));
            }
            if (driver.SelectSingleNode("DashColor") != null)
            {
                Function.Call((Hash)0x6089CDF6A57F326C, veh, int.Parse(driver.SelectSingleNode("DashColor").InnerText, CultureInfo.InvariantCulture));

            }
            if (driver.SelectSingleNode("PearlescentColor") != null)
            {
                veh.PearlescentColor = (VehicleColor)int.Parse(driver.SelectSingleNode("PearlescentColor").InnerText, CultureInfo.InvariantCulture);
            }
            if (driver.SelectSingleNode("SecondaryColor") != null)
            {
                veh.SecondaryColor = (VehicleColor)int.Parse(driver.SelectSingleNode("SecondaryColor").InnerText, CultureInfo.InvariantCulture);
            }
            if (driver.SelectSingleNode("PrimaryColor") != null)
            {
                veh.PrimaryColor = (VehicleColor)int.Parse(driver.SelectSingleNode("PrimaryColor").InnerText, CultureInfo.InvariantCulture);
            }

            if (driver.SelectSingleNode("RimColor") != null)
            {
                veh.RimColor = (VehicleColor)int.Parse(driver.SelectSingleNode("RimColor").InnerText, CultureInfo.InvariantCulture);
            }
            if (driver.SelectSingleNode("LicensePlate") != null)
            {
                Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, veh, int.Parse(driver.SelectSingleNode("LicensePlate").InnerText, CultureInfo.InvariantCulture));
            }
            if (driver.SelectSingleNode("LicensePlateText") != null)
            {
                veh.NumberPlate = driver.SelectSingleNode("LicensePlateText").InnerText;
            }
            if (driver.SelectSingleNode("WindowsTint") != null)
            {
                veh.WindowTint = (VehicleWindowTint)int.Parse(driver.SelectSingleNode("WindowsTint").InnerText, CultureInfo.InvariantCulture);
            }
            if (driver.SelectSingleNode("Livery") != null)
            {
                veh.Livery = int.Parse(driver.SelectSingleNode("Livery").InnerText, CultureInfo.InvariantCulture);
            }

            if (driver.SelectSingleNode("SmokeColor") != null)
            {
                Color color = Color.FromArgb(255, int.Parse(driver.SelectSingleNode("SmokeColor/Color/R").InnerText), int.Parse(driver.SelectSingleNode("SmokeColor/Color/G").InnerText), int.Parse(driver.SelectSingleNode("SmokeColor/Color/B").InnerText));
                veh.TireSmokeColor = color;
            }

            if (driver.SelectSingleNode("NeonColor") != null)
            {
                Color color = Color.FromArgb(255, int.Parse(driver.SelectSingleNode("NeonColor/R").InnerText), int.Parse(driver.SelectSingleNode("NeonColor/G").InnerText), int.Parse(driver.SelectSingleNode("NeonColor/B").InnerText));
                veh.NeonLightsColor = color;

                veh.SetNeonLightsOn(VehicleNeonLight.Back, bool.Parse(driver.SelectSingleNode("Neons/Back").InnerText));
                veh.SetNeonLightsOn(VehicleNeonLight.Front, bool.Parse(driver.SelectSingleNode("Neons/Front").InnerText));
                veh.SetNeonLightsOn(VehicleNeonLight.Left, bool.Parse(driver.SelectSingleNode("Neons/Left").InnerText));
                veh.SetNeonLightsOn(VehicleNeonLight.Right, bool.Parse(driver.SelectSingleNode("Neons/Right").InnerText));
            }

            foreach (XmlElement component in driver.SelectNodes("Components/*"))
            {
                Function.Call(Hash.SET_VEHICLE_EXTRA, veh, int.Parse(component.GetAttribute("ComponentIndex")), int.Parse(component.InnerText, CultureInfo.InvariantCulture));
            }
            foreach (XmlElement component in driver.SelectNodes("ModToggles/*"))
            {
                Function.Call(Hash.TOGGLE_VEHICLE_MOD, veh, int.Parse(component.GetAttribute("ToggleIndex")), bool.Parse(component.InnerText));
            }
            if (driver.SelectSingleNode("CustomTires") != null)
            {
                foreach (XmlElement component in driver.SelectNodes("Mods/*"))
                {
                    veh.SetMod((VehicleMod)int.Parse(component.GetAttribute("ModIndex")), int.Parse(component.InnerText, CultureInfo.InvariantCulture), bool.Parse(driver.SelectSingleNode("CustomTires").InnerText));
                }
            }
        }
        else
        {
            if (veh.LiveryCount > 0) veh.Livery = RandomInt(0, veh.LiveryCount - 1);
            RandomTuning(veh);
            return "No layouts found for this " + veh.FriendlyName + ".";
        }
        return "";
    }

    
    public static void RandomTuning(Vehicle veh)
    {
        Function.Call(Hash.SET_VEHICLE_MOD_KIT, veh, 0);

        //Change color
        var color = Enum.GetValues(typeof(VehicleColor));
        Random random = new Random();
        veh.PrimaryColor = (VehicleColor)color.GetValue(random.Next(color.Length));

        Random random2 = new Random();
        veh.SecondaryColor = (VehicleColor)color.GetValue(random2.Next(color.Length));

        if (veh.LiveryCount > 0) veh.Livery = RandomInt(0, veh.LiveryCount);

        //Change tuning parts
        foreach (int mod in Enum.GetValues(typeof(VehicleMod)).Cast<VehicleMod>())
        {
            veh.SetMod((VehicleMod)mod,  RandomInt(0, veh.GetModCount((VehicleMod)mod)), false);
        }
        //Change neons if at night
        if (World.CurrentDayTime.Hours > 20 || World.CurrentDayTime.Hours < 7)
        {

            //Color neoncolor = Color.FromArgb(0, Util.GetRandomInt(0, 255), Util.GetRandomInt(0, 255), Util.GetRandomInt(0, 255));

            Color neoncolor = Color.FromKnownColor((KnownColor)RandomInt(0, Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>().Count()));
            veh.NeonLightsColor = neoncolor;

            veh.SetNeonLightsOn(VehicleNeonLight.Front, true);
            veh.SetNeonLightsOn(VehicleNeonLight.Back, true);
            veh.SetNeonLightsOn(VehicleNeonLight.Left, true);
            veh.SetNeonLightsOn(VehicleNeonLight.Right, true);

        }
    }




}

