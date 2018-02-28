using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeUI;
using GTA.Math;
using System.IO;

namespace LSPDispatch
{

    public class SuspectHandler
    {
        float VehicleStuck = 0;



        //STATIC
        public CriminalType KindOfCriminal;
        public Ped Criminal;
        public List<Ped> Partners = new List<Ped>();
        public int CopsToFightAgainst = 0;
        public float RadiusToFight = 0;
        public int DrivingStyle = 4 + 8 + 16 + 32;
        public List<CriminalFlags> Flags = new List<CriminalFlags>();
        public bool ShouldRemoveCriminal = false;
        public Blip LostLOSBlip;

        bool BigRewardOnSavedVehicle = false;


        //DYNAMICALLY CHANGED
        public CopUnitHandler CopArrestingMe;
        public Vehicle VehicleChosen;
        public CriminalState State = CriminalState.Fleeing_Foot;
        public int RefTime = Game.GameTime;
        public int LOSTreshold = 0;
        public int ActionInertiaRefTime = 0;
        public bool IsBrakeChecking = false;
        public bool IsRamming = false;
        public int CopsNearSuspect = 0;
        public float RamSpeed = 0;
        public int CopsKilled = 0;
        public List<CopUnitHandler> CopsChasingMe = new List<CopUnitHandler>();
        public bool InPlayerCustody = false;
        public int WanderTimeRef = 0;
        //Decision
        int DesperateBehavior = Game.GameTime;
        //Defines how much time the criminal will "act desperate". CriminalFlags.DYNAMIC_EVASION_BEHAVIOR is required.
        //Other functions will change this variable to give the criminal time to behave like this.

        

        public bool Auth_Ramming = false;
        public bool Auth_DeadlyForce = false;
        public bool InCombat = false;
        public Vector3 DesiredDestination;
        public Vehicle Cargo;

        //Abilities handlers
        public int FirstCarHandle=0;
        public int AbilityRefTime = Game.GameTime;



        public SuspectHandler(CriminalType type, Vector3 pos, Ped suspect)
        {
            KindOfCriminal = type;
            bool transferredCriminal = false;
            if (Util.CanWeUse(suspect))
            {
                if (!suspect.IsPersistent) suspect.IsPersistent = true;
                suspect.Task.ClearAll();
                transferredCriminal = true;
                Criminal = suspect;
            }


            //DEFINE CRIMINAL DETAILS
            DefineUnits.DefineCriminal(this,pos, type, transferredCriminal);
            

            Criminal.BlockPermanentEvents = true;
            Criminal.AlwaysKeepTask = true;
            //Criminal.CanSufferCriticalHits = false;

            Function.Call(Hash.SET_PED_DIES_WHEN_INJURED, Criminal, false);
            Function.Call(Hash.SET_PED_CONFIG_FLAG, Criminal, 281, true);
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, Criminal, true);
            Function.Call(Hash.SET_PED_SHOOT_RATE, Criminal, 1000);


            BigRewardOnSavedVehicle = (Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_IMPOUND) || Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_DRUGS)|| Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_CASH));
            if (!transferredCriminal)
            {
                if (Util.CanWeUse(VehicleChosen))
                {

                    Criminal.SetIntoVehicle(VehicleChosen, VehicleSeat.Driver);
                    State = CriminalState.Fleeing_Vehicle;
                    Util.MoveEntitytoNearestRoad(VehicleChosen);
                    VehicleChosen.IsPersistent = false;
                    Util.GetSquadIntoVehicle(Partners, VehicleChosen);
                    Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, VehicleChosen, 20f);
                    //VehicleChosen.Position=Util.GenerateSpawnPos(Criminal.Position, Util.Nodetype.Road, false);
                }
                else
                {
                    Criminal.Position = World.GetNextPositionOnSidewalk(Criminal.Position);
                }



                Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "DETAILS", DangerousIndividuals.GenerateContextDetailsForSuspect(this));

                if (Auth_DeadlyForce)
                {
                    Auth_Ramming = true;
                    Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "AUTHORIZATIONS", DangerousIndividuals.DeadlyForce + " and ~y~" + DangerousIndividuals.PIT + "~w~ have been authorized.");
                }
                else if (Auth_Ramming)
                {
                    if (!Auth_DeadlyForce) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "AUTHORIZATIONS", DangerousIndividuals.PIT + " authorized, no deadly force necessary for now.");
                }
                else
                {
                    Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "AUTHORIZATIONS", "Stand by.");
                }
                if (BigRewardOnSavedVehicle)
                {
                    Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "IMPORTANT VEHICLE", "The suspect's vehicle is a valuable asset. Try not to destroy it.");

                    // (Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_IMPOUND) || Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_DRUGS) || Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_CASH));
                }
            }





            LostLOSBlip = Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, Criminal.Position.X, Criminal.Position.Y, Criminal.Position.Z);
            LostLOSBlip.Sprite = BlipSprite.PoliceArea;
            LostLOSBlip.IsShortRange = true;

            if (Flags.Contains(CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF)) State = CriminalState.FightingCops;
            if (Util.CanWeUse(VehicleChosen)) FirstCarHandle = VehicleChosen.Handle;
        }

        public bool Surrendered()
        {
            return State == CriminalState.Arrested || State == CriminalState.DealtWith || State == CriminalState.Surrendering;
        }



        public void HandleVisiblity()
        {
            if (Util.CanPedSeePed(Game.Player.Character, Criminal, false) || Info.CanBeSeenByCops(this))
            {
                if (LOSTreshold != 0)
                {
                    if (LOSTreshold > DangerousIndividuals.LostLOSThreshold)
                    {
                        if (DangerousIndividuals.AllowChaseNotifications.Checked)
                        {
                            CopUnitHandler Unit = DangerousIndividuals.GetClosestCop(this, true, true);//Suspect.CopsChasingMe[Util.RandomInt(0, Suspect.CopsChasingMe.Count - 1)];
                            if (Unit != null)
                            {
                                string Context = "";
                                if (Util.CanWeUse(VehicleChosen)) Context += "" + DangerousIndividuals.GetVehicleClassName(VehicleChosen) + ", headed " + Util.GetWhereIsHeaded(Criminal, false);
                                else Context += "They're on foot, fleeing " + Util.GetWhereIsHeaded(Criminal, false);
                                Util.AddNotification("web_lossantospolicedept", "~b~" + Unit.CopVehicle.FriendlyName, "SUSPECT SIGHT REGAINED", "Suspect visual regained, "+Context+".");
                            }
                        }
                    }
                    LOSTreshold = 0;
                }

            }
            else if (!Criminal.IsInPoliceVehicle && State != CriminalState.Surrendering && State != CriminalState.Arrested)
            {
                LOSTreshold++;
            }

            if (LOSTreshold < 10)
            {
                if (Criminal.CurrentBlip.Alpha != 255) Criminal.CurrentBlip.Alpha = 255;
                foreach (Ped partner in Partners) if (partner.CurrentBlip.Alpha != 255) partner.CurrentBlip.Alpha = 255;
            }
            else
            {
                if (Criminal.CurrentBlip.Alpha != 0) Criminal.CurrentBlip.Alpha = 0;
                foreach (Ped partner in Partners) if (partner.CurrentBlip.Alpha != 0) partner.CurrentBlip.Alpha = 0;
            }
        }

        //Things that need to be checked ontick
        public void UpdateFast()
        {
            if(State == CriminalState.Surrendering && Game.Player.Character.Position.DistanceTo(this.Criminal.Position) < 5f)
            {
                Util.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to arrest this criminal.");
                if(Game.IsControlJustPressed(2, GTA.Control.Context))
                {
                    Criminal.Heading =Game.Player.Character.Heading;
                    Criminal.Heading += 90;
                    Function.Call(Hash.TASK_PLAY_ANIM, Criminal, "mp_arresting", "b_arrest_on_floor", 1f, 1f, -1, 0, 0f, false, false, false);
                    Function.Call(Hash.TASK_PLAY_ANIM, Game.Player.Character, "mp_arresting", "a_arrest_on_floor", 1f, 1f, -1, 0, 0f, false, false, false);
                    State = CriminalState.Arrested;
                    InPlayerCustody = true;
                    Util.AddQueuedHelpText("Bring the ~g~criminal~w~ to a nearby police station to end this callout.");
                }
            }
            if (DangerousIndividuals.DebugNotifications.Checked && this.DesiredDestination != Vector3.Zero && Util.CanWeUse(this.Criminal)) DangerousIndividuals.DrawLine(this.Criminal.Position, DesiredDestination);
            if (Util.CanWeUse(Cargo) && !Cargo.IsInRangeOf(Criminal.Position,50f)) Cargo.IsPersistent = false;

            /* Incomplete Dynamic Offroad
            if(Util.CanWeUse(VehicleChosen))
            {A new police car spawned inside my old Unmarkeed Stanier while I was leaving the place.
                if (Util.IsOffroadCapable(VehicleChosen) && !Flags.Contains(CriminalFlags.PREFERS_OFFROAD))
                {
                    if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Added Offroad preference");
                    Flags.Add(CriminalFlags.PREFERS_OFFROAD);
                }

                if (!Util.IsOffroadCapable(VehicleChosen) && Flags.Contains(CriminalFlags.PREFERS_OFFROAD) && Flags.Contains(CriminalFlags.PREFERS_ROAD))
                {
                    if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Removed Offroad preference");
                    Flags.Remove(CriminalFlags.PREFERS_OFFROAD);
                }
            }
            */
            if (LostLOSBlip.Exists())
            {
                 if(LOSTreshold < 10) LostLOSBlip.Position = Criminal.Position;
                if (Surrendered() && LostLOSBlip.Alpha != 0) LostLOSBlip.Alpha = 0;
            }
            
            //Nitro
            if (Flags.Contains(CriminalFlags.CHEAT_NITRO)) HandleNitro();

            //SURRENDER
            if (!Auth_DeadlyForce)
            {
                if (Criminal.IsShooting)
                {
                    if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", ""+DangerousIndividuals.DeadlyForce+" authorized", ""+DangerousIndividuals.DeadlyForce+" is authorized on the suspect.");
                    Auth_DeadlyForce = true;
                }
                foreach (Ped partner in Partners)
                {
                    if (partner.IsShooting)
                    {
                        Auth_DeadlyForce = true;
                        if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", ""+DangerousIndividuals.DeadlyForce+" authorized", ""+DangerousIndividuals.DeadlyForce+" is authorized on the suspect.");
                    }
                    break;
                }
            }

            if (Criminal.IsDead)
            {
                if (Criminal.RelationshipGroup != DangerousIndividuals.SurrenderedCriminalsRLGroup) Criminal.RelationshipGroup = DangerousIndividuals.SurrenderedCriminalsRLGroup;

                if (Criminal.CurrentBlip != null) Criminal.CurrentBlip.Remove();
                Criminal.MarkAsNoLongerNeeded();

                if (Partners.Count > 0)
                {
                    Criminal = Partners[0];
                    Partners.RemoveAt(0);
                }
                else
                {
                    ShouldRemoveCriminal = true;
                }
                return;
            }



            InCombat = false;

            if (Criminal.IsInCombat && !Criminal.IsFleeing) InCombat = true;
            List<Ped> DeadPartners = new List<Ped>();
            foreach (Ped partner in Partners)
            {
                //Combat
                if (partner.IsInCombat && !partner.IsFleeing) InCombat = true;

                //Split
                if (!partner.IsInRangeOf(Criminal.Position, 30f))
                {
                    DangerousIndividuals.SplitCriminals.Add(partner);
                    DangerousIndividuals.SplitCriminalKind = KindOfCriminal;
                    if (DangerousIndividuals.AllowChaseNotifications.Checked)
                    {
                        string copname = Info.ClosestCopUnitName(this);
                        if (copname.Length > 0) Util.AddNotification("web_lossantospolicedept", "~b~" + copname, "SUSPECTS SPLIT", "Suspects are splitting.");
                    }
                    DeadPartners.Add(partner);

                }

                //Dead
                if (partner.IsDead)
                {
                    DeadPartners.Add(partner);
                }
            }

            foreach (Ped deadpartrner in DeadPartners)
            {
                if (Partners.Contains(deadpartrner))
                {
                    if (deadpartrner.IsDead)
                    {
                        if (deadpartrner.CurrentBlip != null) deadpartrner.CurrentBlip.Remove();
                        deadpartrner.MarkAsNoLongerNeeded();
                    }                
                    Partners.Remove(deadpartrner);
                }
            }




            if (!Surrendered() && Util.HasBulletImpactedInArea(Criminal.Position,10f))
            {
                if (Flags.Contains(CriminalFlags.SURRENDERS_IF_SHOT_AT_EASY) && Util.RandomInt(0,10)<4)
                {
                    State = CriminalState.Surrendering;
                }
                if (Flags.Contains(CriminalFlags.SURRENDERS_IF_SHOT_AT_HARD) && Util.RandomInt(0, 10) < 2)
                {
                    State = CriminalState.Surrendering;
                }
            }
        }
        bool CanStandOffCops()
        {
            int CopsNearby = DangerousIndividuals.NumberOfCopsNearSuspect(this, 20f);
            //UI.Notify(CopsNearby.ToString());
            if (CopsNearby == 0) return false;

            if (Flags.Contains(CriminalFlags.CAN_STANDOFF_CAUTIOUS) && (Partners.Count + 1) >= CopsNearby)
            {
                return true;
            }
            if (Flags.Contains(CriminalFlags.CAN_STANDOFF_AGGRESIVE) && ((Partners.Count + 1) >= CopsNearby / 2) || CopsNearby < 5)
            {
                return true;
            }

            return false;
        }


        void HandleSurrenderConditions()
        {
            if(!Surrendered())
            {
                //SURRENDER CONDITIONS
                if (Util.IsPlayerOrCopNearby(Criminal.Position, 10f))
                {
                    if (Criminal.IsBeingStunned ||
                        (Flags.Contains(CriminalFlags.SURRENDERS_WHEN_RAGDOLL) && Criminal.IsRagdoll && Criminal.Velocity.Length() < 1f) ||
                        (Flags.Contains(CriminalFlags.SURRENDERS_IF_HURT) && ((Criminal.Health < 40 && Util.RandomInt(0, 40) > Criminal.Health) || (Criminal.IsBeingStunned && Util.RandomInt(0, 100) > 30))) ||
                        (Util.ForwardSpeed(Criminal) < 5f && Flags.Contains(CriminalFlags.SURRENDERS_IF_AIMED_AT) && Game.Player.IsAiming && Game.Player.Character.IsInRangeOf(Criminal.Position, 4f)) ||
                        (Flags.Contains(CriminalFlags.SURRENDERS_IF_FORCED_OUT_OF_VEHICLE) && Criminal.IsBeingJacked) ||                   
                        (Criminal.IsOnFoot && Flags.Contains(CriminalFlags.SURRENDERS_IF_SURROUNDED) && CopsChasingMe.Count > (Partners.Count + 1) * 2))
                    {
                        State = CriminalState.Surrendering;
                    }
                }
                if(Partners.Count < 50)
                {
                    List<Ped> Surrendered = new List<Ped>();

                    foreach (Ped partner in Partners)
                    {
                        //SURRENDER CONDITIONS
                        if ((Flags.Contains(CriminalFlags.SURRENDERS_WHEN_STUNNED) && partner.IsBeingStunned) ||
                            (Flags.Contains(CriminalFlags.SURRENDERS_WHEN_RAGDOLL) && partner.IsRagdoll && partner.Velocity.Length() < 1f) ||
                            (Flags.Contains(CriminalFlags.SURRENDERS_IF_HURT) && partner.Health < 40) ||
                            (Flags.Contains(CriminalFlags.SURRENDERS_IF_AIMED_AT) && Util.ForwardSpeed(partner) < 5f && Game.Player.IsAiming && Game.Player.Character.IsInRangeOf(partner.Position, 4f) && partner.Velocity.Length() < 3f) ||
                            (Flags.Contains(CriminalFlags.SURRENDERS_IF_SURROUNDED) && partner.IsOnFoot && CopsNearSuspect >= Partners.Count + 1))
                        {
                            DangerousIndividuals.SplitCriminals.Add(partner);
                            DangerousIndividuals.SplitCriminalKind = KindOfCriminal;
                            Surrendered.Add(partner);
                        }
                    }

                    foreach (Ped Surrender in Surrendered)
                    {
                        if (Partners.Contains(Surrender))
                        {
                            Partners.Remove(Surrender);
                        }
                    }
                }
            }
        }
        
        bool IsNitroing = false;
        void HandleNitro()
        {
            if (Util.CanWeUse(VehicleChosen) && VehicleChosen.Handle == FirstCarHandle)
            {
                if (IsNitroing)
                {
                  if(VehicleChosen.Acceleration>0 && Math.Abs(Function.Call<Vector3>(Hash.GET_ENTITY_ROTATION_VELOCITY, VehicleChosen, true).Z)<1f)  Info.ForceNitro(VehicleChosen);
                    if (Game.GameTime > AbilityRefTime)
                    {
                        AbilityRefTime = Game.GameTime+ 5000;
                        IsNitroing = false;
                    }
                }
                else
                {
                    if (CopsNearSuspect>0 && Game.GameTime > AbilityRefTime && VehicleChosen.Speed>5f && !Util.IsSliding(VehicleChosen, 1f) )
                    {
                        AbilityRefTime = Game.GameTime + 10000;
                        IsNitroing = true;
                    }
                }
            }
        }


        public void GiveVehicleReward()
        {
            if(Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_IMPOUND)) Util.AddNotification("", "", "", "You got a ~g~1000$~w~ bonus for that tuned car.");
            if (Flags.Contains(CriminalFlags.IMPORTANT_VEHICLE_CASH)) Util.AddNotification("", "", "", "You got a ~g~1000$~w~ bonus for saving that cash truck.");
        }
        //Things checked each half a second
        public void Update()
        {

            File.AppendAllText(@"" +DangerousIndividuals.debugpath, " - Blip");

            //Blip
            if (!Criminal.CurrentBlip.Exists()) Criminal.AddBlip();
            if (Util.CanWeUse(Criminal.CurrentVehicle)) Criminal.CurrentBlip.Scale = 1f; else Criminal.CurrentBlip.Scale = 0.7f;
            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - Partners");
            foreach (Ped partner in Partners)
            {
                if (!partner.CurrentBlip.Exists())
                {
                    partner.AddBlip();
                    partner.CurrentBlip.Scale = 0.7f;
                }
                else
                {
                    if (Util.CanWeUse(partner.CurrentVehicle)) partner.CurrentBlip.Scale = 1f; else partner.CurrentBlip.Scale = 0.7f;

                }
            }



            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - Copsnear");

            int num = 10;
            if (!this.Criminal.IsOnFoot) num = 50;
            CopsNearSuspect = DangerousIndividuals.NumberOfCopsNearSuspect(this, num);

            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - Visiblity");

            HandleVisiblity();

            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - SurrenderConds");

            HandleSurrenderConditions();
            //if(Surrendered()) HandleCopArrestingMeFixes();
            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - Handle VehicleChosen");

            if (Util.CanWeUse(VehicleChosen))
            {
                Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, VehicleChosen, true);
                //Ramming
                if (Function.Call<bool>(Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING, VehicleChosen))
                {
                    if (Flags.Contains(CriminalFlags.DYNAMIC_EVASION_BEHAVIOR) && DesperateBehavior < Game.GameTime && Util.RandomInt(0, 10) < 6)
                    {                        
                        DesperateBehavior = Game.GameTime + 30000;
                        UI.Notify(VehicleChosen.FriendlyName + " enables desperate behavior for 30 seconds");
                        Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, VehicleChosen);
                    }
                    if (!Auth_Ramming)
                    {
                        if (Util.RandomInt(0, 10) < 4)
                        {
                            Auth_Ramming = true;
                            if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "" + DangerousIndividuals.PIT + " authorized", "" + DangerousIndividuals.PIT + " have been authorized on the " + (DangerousIndividuals.SimplifyColorString(VehicleChosen.PrimaryColor.ToString())).ToLowerInvariant() + " ~b~" + DangerousIndividuals.GetVehicleClassName(VehicleChosen) + "~w~.");
                        }
                        else
                        {
                            Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, VehicleChosen);
                        }
                    }
                }
            }

            if (State == CriminalState.FightingCops) Criminal.BlockPermanentEvents = false; else Criminal.BlockPermanentEvents = true;

            if (State != CriminalState.Surrendering && State != CriminalState.Arrested && State != CriminalState.DealtWith)
            {
                // FIGHT COPS IF THEY'RE TOO CLOSE
                if (!Util.CanWeUse(Criminal.CurrentVehicle)) //Game.GameTime > ActionInertiaRefTime + 5000 && Criminal.IsOnFoot && !Criminal.IsGettingIntoAVehicle
                {
                    if (Criminal.Weapons.Current.Hash != WeaponHash.Unarmed && (CanStandOffCops() || Flags.Contains(CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF)))
                    {
                        ActionInertiaRefTime = Game.GameTime;
                        State = CriminalState.FightingCops;
                    }
                }

            }
            File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - Handle State");

            switch (State)
            {
                case CriminalState.Fleeing_Vehicle:
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - HS FleeCar");

                        if (Util.CanWeUse(VehicleChosen) && Util.IsCarDrivable(VehicleChosen))
                        {
                            if (Criminal.IsOnFoot)
                            {
                                if (!Util.IsSubttaskActive(Criminal, Util.Subtask.ENTERING_VEHICLE_GENERAL)) Criminal.Task.EnterVehicle(VehicleChosen, VehicleSeat.Driver, -1, 5f);
                            }
                            if (Util.IsInVehicle(Criminal, VehicleChosen, VehicleSeat.Driver))
                            {
                                if ((AreAllPartnersInTheCar() || CopsNearSuspect > 0))
                                {
                                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - HS Driving");

                                    HandleDriving();
                                }                                
                            }
                            // LOST/STUCK VEHICLE

                            if (Math.Round(VehicleStuck, 0) == 5 ) { Criminal.Task.ClearAll(); }
                            if (VehicleStuck > 10 || !VehicleChosen.IsInRangeOf(Criminal.Position, 100f) || (Criminal.IsOnFoot && VehicleChosen.Speed>2f))
                            {

                                VehicleStuck = 0;
                                State = CriminalState.Fleeing_Foot;
                                /*if (VehicleChosen.BodyHealth < 900)*/ VehicleChosen.IsDriveable = false;
                                VehicleChosen = null;
                            }
                        }
                        else
                        {
                            if (DangerousIndividuals.AllowChaseNotifications.Checked)
                            {
                                string copname = Info.ClosestCopUnitName(this);
                                if (copname.Length > 0) Util.AddNotification("web_lossantospolicedept", "~b~"+ copname, "SUSPECT DITCHING VEHICLE", "Suspect is ditching the "+DangerousIndividuals.GetVehicleClassName(VehicleChosen)+".");
                            }

                            if (Util.CanWeUse(VehicleChosen))
                            {
                                
                                VehicleChosen.IsDriveable = false;
                                VehicleChosen = null;
                            }
                            State = CriminalState.Fleeing_Foot;
                            //ActionInertiaRefTime = Game.GameTime;
                        }
                        break;
                    }
                case CriminalState.Fleeing_Foot:
                    {
                        //if (Criminal.IsInCombat && !Criminal.IsFleeing) Criminal.Task.ClearAll();
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - HS FleeFoot");

                        if (BigRewardOnSavedVehicle)
                        {
                            GiveVehicleReward();
                            BigRewardOnSavedVehicle = false;
                        }


                        // LOOK FOR VEHS
                        VehicleChosen = (Util.LookForGetawayVehicles(Criminal.Position, 40f, Partners.Count, Flags.Contains(CriminalFlags.PREFERS_FAST_VEHICLES), Flags.Contains(CriminalFlags.PREFERS_HEAVY_VEHICLES), Flags.Contains(CriminalFlags.CAN_STEAL_POLICE_VEHICLES), Flags.Contains(CriminalFlags.CAN_STEAL_PARKED_VEHICLES), Flags.Contains(CriminalFlags.CAN_STEAL_OCCUPIED_VEHICLES)));
                        if (Util.CanWeUse(VehicleChosen))
                        {
                            State = CriminalState.Fleeing_Vehicle;
                            ActionInertiaRefTime = Game.GameTime;
                        }

                        //FLEE
                        if (!Criminal.IsOnFoot)
                        {
                            Criminal.Task.LeaveVehicle();
                        }
                        else
                        {
                            if (Criminal.IsStopped || !Criminal.IsFleeing)
                            {
                                    if (Game.Player.Character.Position.DistanceTo(Criminal.Position) < 70f) Criminal.Task.FleeFrom(Game.Player.Character); else Criminal.Task.RunTo(Util.GenerateSpawnPos(Criminal.Position + (Criminal.ForwardVector * 50),Util.Nodetype.Offroad,false));
                            }
                            
                            //if (Criminal.IsStopped) Criminal.Task.FleeFrom(Game.Player.Character.Position, -1);
                        }

                        if (Criminal.Weapons.Current.Hash == WeaponHash.Unarmed) Auth_DeadlyForce=false;
                        break;
                    }
                case CriminalState.FightingCops:
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, " - HS FightCops");

                        if (!Flags.Contains(CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF))//Game.GameTime > ActionInertiaRefTime + 5000 && 
                        {
                            if (!CanStandOffCops() || ((Game.Player.Character.IsInCover()|| !Util.CanPlayerdSeePed(Criminal,true)) && Util.RandomInt(1, 10) > 7))
                            {
                                ActionInertiaRefTime = Game.GameTime;
                                State = CriminalState.Fleeing_Foot;
                            }
                        }
                        if (Criminal.IsFleeing) Criminal.Task.ClearAll();

                        if (!Criminal.IsOnFoot) Criminal.Task.LeaveVehicle(); else if (!Criminal.IsInCombat && Criminal.IsStopped)
                        {

                            Criminal.Task.WanderAround();

                            //if (Game.Player.Character.IsInRangeOf(Criminal.Position, 50f)) Criminal.Task.FightAgainst(Game.Player.Character);
                            if(Criminal.IsStopped) Criminal.Task.WanderAround(Criminal.Position, 5f);
                            Function.Call(Hash.REGISTER_HATED_TARGETS_AROUND_PED, Criminal, 100f);
                            Criminal.Task.FightAgainstHatedTargets(100f);
                        }
                        break;
                    }
                case CriminalState.Surrendering:
                    {

                        if(!Game.Player.Character.IsInRangeOf(Criminal.Position, 200f))
                        {
                            ShouldRemoveCriminal = true;
                        }
                        if (Criminal.IsInCombat || Criminal.IsFleeing) Criminal.Task.ClearAll();

                        if (Criminal.CurrentBlip != null && Criminal.CurrentBlip.Color != BlipColor.Yellow) Criminal.CurrentBlip.Color = BlipColor.Yellow;


                        if (Criminal.Weapons.Current.Hash != WeaponHash.Unarmed)
                        {
                            Criminal.Weapons.Drop();
                            Criminal.Weapons.RemoveAll();
                        }
                        if (Criminal.RelationshipGroup != DangerousIndividuals.SurrenderedCriminalsRLGroup) Criminal.RelationshipGroup = DangerousIndividuals.SurrenderedCriminalsRLGroup;

                        if (Partners.Count > 0)
                        {
                            foreach (Ped partner in Partners)
                            {
                                DangerousIndividuals.SplitCriminals.Add(partner);
                                DangerousIndividuals.SplitCriminalKind = KindOfCriminal;
                            }
                            Partners.Clear();
                        }

                        Criminal.RelationshipGroup = Game.Player.Character.RelationshipGroup;

                        if (Util.IsInVehicle(Criminal,VehicleSeat.Any)) { Criminal.Task.LeaveVehicle();}


                        if (Criminal.IsRagdoll && Util.IsPlayingAnim(Criminal, "mp_bank_heist_1", "prone_l_front_intro")) Function.Call(Hash.STOP_ENTITY_ANIM, Criminal, "mp_bank_heist_1", "prone_l_front_intro", 3);

                        if (Criminal.IsStopped && !Util.IsPlayingAnim(Criminal, "mp_bank_heist_1", "prone_l_front_intro"))
                        {
                            Function.Call(Hash.TASK_PLAY_ANIM, Criminal, "mp_bank_heist_1", "prone_l_front_intro", 1f, 1f, -1, 2, 0f, false, false, false);
                        }


                        
                        if (CopArrestingMe != null && !Util.IsPlayingAnim(CopArrestingMe.Leader, "mp_arresting", "a_arrest_on_floor") && 
                            CopArrestingMe.Leader.IsInRangeOf(Criminal.Position,2f) && CopArrestingMe.Leader.Velocity.Length()<1f && Criminal.Velocity.Length() < 1f && 
                            CopArrestingMe.Leader.IsOnFoot)
                        {
                            if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Surrendered criminal arrested");
                            CopArrestingMe.Leader.Weapons.Select(WeaponHash.Unarmed, true);
                            Vector3 pos = new Vector3(CopArrestingMe.Leader.Position.X,CopArrestingMe.Leader.Position.Y, Criminal.Position.Z-Criminal.HeightAboveGround)+(CopArrestingMe.Leader.ForwardVector*0.9f);
                            //Criminal.Position = pos;
                            Criminal.Heading = CopArrestingMe.Leader.Heading;
                            Criminal.Heading += 90;
                            Function.Call(Hash.TASK_PLAY_ANIM, Criminal, "mp_arresting", "b_arrest_on_floor", 1f, 1f, -1, 0, 0f, false, false, false);
                            Function.Call(Hash.TASK_PLAY_ANIM, CopArrestingMe.Leader, "mp_arresting", "a_arrest_on_floor", 1f, 1f, -1, 0, 0f, false, false, false);
                            State = CriminalState.Arrested;
                        }
                        break;
                    }
                case CriminalState.Arrested:
                    {
                        if (Criminal.CurrentBlip != null && Criminal.CurrentBlip.Color != BlipColor.Green) Criminal.CurrentBlip.Color = BlipColor.Green;

                        if (!Util.IsPlayingAnim(Criminal, "mp_arresting", "b_arrest_on_floor"))
                        {
                            //Animation
                            if (Criminal.IsRagdoll && Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Criminal, "MP_ARRESTING", "IDLE", 3)) Function.Call(Hash.STOP_ENTITY_ANIM, Criminal, "MP_ARRESTING", "IDLE", 3);
                            if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, Criminal, "MP_ARRESTING", "IDLE", 3)) Function.Call(Hash.TASK_PLAY_ANIM, Criminal, "MP_ARRESTING", "IDLE", -1, 1f, -1, 1 + 48, false, false, false);
                        }
                        else
                        {
                            //if(Criminal.Velocity.Length()>2f) Function.Call(Hash.STOP_ENTITY_ANIM, Criminal, "mp_arresting", "b_arrest_on_floor", 3);
                        }

                        if (InPlayerCustody)
                        {

                            foreach(Vector3 station in Info.AllPoliceStations)
                            {
                                if (Criminal.Position.DistanceTo(station) < 40f && Criminal.Velocity.Length() < 1f) State = CriminalState.DealtWith;
                            }


                            if (Criminal.IsOnFoot && Criminal.Velocity.Length() < 1f && !Util.IsPlayingAnim(Criminal, "mp_arresting", "b_arrest_on_floor"))
                            {
                                Vehicle playerveh = Util.GetLastVehicle(Game.Player.Character);

                                if (Util.CanWeUse(playerveh) && Criminal.Position.DistanceTo(playerveh.Position) < 7f)
                                {                                    
                                    Criminal.Task.EnterVehicle(playerveh, Util.GetEmptyBackseat(playerveh));
                                }
                                else
                                {
                                    if (Criminal.Position.DistanceTo(Game.Player.Character.Position) > 5f) Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY, Criminal, Game.Player.Character, 0, 0, 0, 1f, -1, 3f, true);

                                }
                            }                            
                        }
                        else
                        {
                            if (!Util.IsPlayingAnim(Criminal, "mp_arresting", "b_arrest_on_floor"))
                            {
                                if (CopArrestingMe != null)
                                {
                                    if (CopArrestingMe.State == CopState.SecureArrest) { CopArrestingMe = null; return;}                                
                                    if (Util.CanWeUse(CopArrestingMe.CopVehicle))
                                    {
                                        if (!Criminal.IsInPoliceVehicle)
                                        {
                                            if (!Game.Player.Character.IsInRangeOf(Criminal.Position, 300f))
                                            {
                                                Criminal.SetIntoVehicle(CopArrestingMe.CopVehicle, Util.GetEmptyBackseat(CopArrestingMe.CopVehicle));
                                            }
                                            else
                                            {
                                                if (!Criminal.IsInRangeOf(CopArrestingMe.CopVehicle.Position, 10f))
                                                {
                                                    if (Criminal.IsInRangeOf(CopArrestingMe.Leader.Position, 10f) && CopArrestingMe.Leader.IsOnFoot && Criminal.Velocity.Length() < 1f)
                                                    {
                                                        Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY, Criminal, CopArrestingMe.Leader, 0, 0, 0, 1f, -1, 3f, true);
                                                    }
                                                }
                                                else if (!Util.IsSubttaskActive(Criminal, Util.Subtask.ENTERING_VEHICLE_GENERAL)) Criminal.Task.EnterVehicle(CopArrestingMe.CopVehicle, Util.GetEmptyBackseat(CopArrestingMe.CopVehicle), 10000, 1f);
                                            }
                                        }
                                        else
                                        {
                                            State = CriminalState.DealtWith;
                                            CopArrestingMe = null;
                                        }
                                    }
                                    else { CopArrestingMe = null; }
                                }
                            }
                        }
                        break;
                    }
                case CriminalState.DealtWith:
                    {
                        //Function.Call(Hash.TASK_PLAY_ANIM, Criminal, "MP_ARRESTING", "IDLE", -1, 1f, -1, 1 + 48, false, false, false);
                        if (!ShouldRemoveCriminal) ShouldRemoveCriminal = true;
                        break;
                    }
            }

            //Handle simulated stealing of vehicles (when the criminal is off the radar)
            if (!Util.CanWeUse(VehicleChosen) && this.Criminal.IsOnFoot && (LOSTreshold == 30 || LOSTreshold == 60))
            {
                if ((this.Flags.Contains(CriminalFlags.CHEAT_CAN_EASILY_DISSAPEAR_WHEN_HIDDEN) && Util.RandomInt(0, 10) < 6) || (this.Flags.Contains(CriminalFlags.CAN_STEAL_PARKED_VEHICLES) && Util.RandomInt(0, 10) < 3))
                {
                    VehicleChosen = World.CreateVehicle(Util.GetRandomVehicleFromList(Info.AverageVehs), this.Criminal.Position.Around(5));

                    if (Util.CanWeUse(VehicleChosen))
                    {
                        this.Criminal.SetIntoVehicle(VehicleChosen, VehicleSeat.Driver);
                        this.State = CriminalState.Fleeing_Vehicle;
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("[CHEAT] Hidden suspect acquired a new car.");
                    }
                }

            }
        }

        void HandleDriving()
        {

            //If its not driving or its near its destination
            if ((!Util.IsDriving(Criminal) && !Util.IsSubttaskActive(Criminal, (Util.Subtask)155)) || Criminal.IsInRangeOf(DesiredDestination, 30f) || (WanderTimeRef<Game.GameTime && WanderTimeRef!=0))
            {
                File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 1 ");

                WanderTimeRef = 0;
                if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(VehicleChosen.FriendlyName + " arrived to destination");


                //By default, wander
                Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, Criminal, VehicleChosen, 200f, DrivingStyle);

                File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 2 ");

                if (Flags.Contains(CriminalFlags.DYNAMIC_EVASION_BEHAVIOR)) 
                {
                    int maneuver = 0;
                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 3 ");

                    //Road preference modifiers and stuff
                    DesiredDestination = Vector3.Zero;
                    if (CopsNearSuspect > 0) //Util.GenerateSpawnPos(Criminal.Position,Util.Nodetype.AnyRoad,false).DistanceTo(Criminal.Position)<20f &&
                    {
                        //Think about going offroad / through alleyways
                        if (DesperateBehavior > Game.GameTime)// if (Util.RandomInt(0, 100) > this.VehicleChosen.BodyHealth/10)                     //if (Util.RandomInt(0, 100) > 0)
                        {
                            if (LOSTreshold == 0)//If the car has high clearance or, at least, its in the city
                            {
                                if (Util.RandomInt(0, 10) <= 5)
                                {
                                    maneuver = 4194304;
                                    if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Suspect will go True Offroad [Desperate]");
                                }
                                else
                                {
                                    maneuver = 262144;
                                    if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Suspect will be on Dirtroad/Alleyway [Desperate]");
                                }
                            }
                        }
                        else
                        {
                            if ((this.VehicleChosen.HeightAboveGround > 0.5) && World.GetZoneName(this.Criminal.Position) != "city")
                            {
                                maneuver = 4194304;
                                if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Suspect will go True Offroad [normal]");
                            }
                            else 
                            {
                                maneuver = 262144;
                                if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Suspect will be on Dirtroad/Alleyway [normal]");
                            }
                        }
                    }
                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 4 ");

                    if (!Util.IsOnRoad(Criminal.Position, VehicleChosen))
                    {
                        maneuver = 4194304;
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("NOT ON ROAD, True Offroad until suspect finds a road");

                    }


                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 5 ");

                    //Generate destination
                    //If the true offroad maneuver is allowed, do it
                    if (maneuver == 4194304)
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 5.1 ");

                        Vector3 pos = Criminal.Position + ((Criminal.ForwardVector * Util.RandomInt(50, 200)) + (Criminal.RightVector * Util.RandomInt(-40, 40)));
                        DesiredDestination = Util.GenerateSpawnPos(pos, Util.Nodetype.LocalPathing, false);

                        if (DesiredDestination == Vector3.Zero) Util.GenerateSpawnPos(Criminal.Position + ((Criminal.ForwardVector * Util.RandomInt(50, 100))), Util.Nodetype.AnyRoad, false);
                    }
                    else if (maneuver == 262144) //if the dirtroad is allowed, do it
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 5.2 ");

                        Vector3 temp = Util.GenerateSpawnPos(Criminal.Position + (Criminal.ForwardVector * Util.RandomInt(50, 200)), Util.Nodetype.Offroad, false);

                        if (Util.RoadTravelDistance(Criminal.Position, temp) < 300f) DesiredDestination = temp;
                        
                    }
                    else if(1==0) //If nothing offroad allowed go on normal road;
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 5.3 ");

                        DesiredDestination = Util.GenerateSpawnPos(Criminal.Position + (Criminal.ForwardVector * Util.RandomInt(200, 400)), Util.Nodetype.Road, false);
                    }


                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 6 ");

                    //Allow going against traffic && ignore peds (only if local pathing is off), based on DesperateBehavior
                    if (maneuver == 0 && DesperateBehavior > Game.GameTime) maneuver = 512 - 16;



                    if(DesiredDestination== Vector3.Zero)
                    {
                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 6.1 ");

                        WanderTimeRef = Game.GameTime + 5000;
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, Criminal, VehicleChosen, 200f, DrivingStyle);
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Suspect will wander for some time");


                    }
                    else
                    {

                        if(maneuver==0) WanderTimeRef = Game.GameTime + 5000;

                        File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 6.2 ");

                        Criminal.Task.DriveTo(VehicleChosen, DesiredDestination, 10f, 200f, DrivingStyle + maneuver);

                    }
                    File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 7 ");

                }
            }
            else //If its driving and far from destination
            {
                File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - driving and far from destination");

                //Stuck check
                //if (Util.ForwardSpeed(VehicleChosen) < -1.5f) VehicleStuck--; 
                if (VehicleChosen.Speed < 1f) VehicleStuck++; else if (VehicleChosen.Speed < 5f) VehicleStuck += 0.5f; else if (VehicleStuck > 0) VehicleStuck = 0f;

                File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - 1");

                if (AbilityRefTime < Game.GameTime)
                {

                    //Try to unstuck
                    if (VehicleStuck > 6 && Util.ForwardSpeed(VehicleChosen)<0.5f)
                    {
                        if (DesperateBehavior < Game.GameTime && Util.RandomInt(0, 10) < 6)
                        {
                            DesperateBehavior = Game.GameTime + 30000;
                            if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(VehicleChosen.FriendlyName + " enables desperate behavior for 30 seconds");
                        }
                        //Can trigger Desperate behavior if stuck

                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(VehicleChosen.FriendlyName+" tries to unstuck");

                        int side = 3; //14 which is right or 13 which is left or 3 which is only reverse

                        AbilityRefTime = Game.GameTime + 10000;
                        TaskSequence RamSequence = new TaskSequence();
                        Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, 0, VehicleChosen, side, 2000);
                        if (DesiredDestination == Vector3.Zero) DesiredDestination = VehicleChosen.Position + (VehicleChosen.ForwardVector * 5);
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, 0, VehicleChosen, DesiredDestination.X, DesiredDestination.Y, DesiredDestination.Z, 200f, DrivingStyle, 10f);
                        RamSequence.Close();
                        Criminal.Task.PerformSequence(RamSequence);
                    }


                    //Ramming
                    if (Util.ForwardSpeed(VehicleChosen) > 15f) //Flags.Contains(CriminalFlags.CAN_RAM) && 
                    {
                        Vehicle Target = Util.GetVehicleAtSide(VehicleChosen, Util.Side.Left);
                        int side = 7;
                        if (!Util.CanWeUse(Target))
                        {
                            Target = Util.GetVehicleAtSide(VehicleChosen, Util.Side.Right);
                            side = 8;
                        }
                        if (Util.CanWeUse(Target) && (Target.IsPersistent || (Util.CanWeUse(Game.Player.Character.CurrentVehicle) && Target==Game.Player.Character.CurrentVehicle)))
                        {
                            //If vehicles are at similar speed and our vehicle is bigger than the target
                            //Math.Abs(Util.ForwardSpeed(VehicleChosen) - Util.ForwardSpeed(Target)
                            if (Util.AreVehsGoingAtSimilarSpeeds(Target,VehicleChosen, 2) && Util.IsBiggerThan(VehicleChosen,Target,-2f))
                            {
                                if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify(VehicleChosen.FriendlyName + " rams");

                                AbilityRefTime = Game.GameTime + 4000;
                                TaskSequence RamSequence = new TaskSequence();
                                Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, 0, VehicleChosen, side, 700);
                                if (DesiredDestination == Vector3.Zero) DesiredDestination = VehicleChosen.Position + (VehicleChosen.ForwardVector * 5);
                                Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, 0, VehicleChosen, DesiredDestination.X, DesiredDestination.Y, DesiredDestination.Z, 200f, DrivingStyle, 10f);
                                RamSequence.Close();
                                Criminal.Task.PerformSequence(RamSequence);
                            }
                        }
                    }
                }





                /*
                //AGGRESIDE DRIVING
                if (!IsBrakeChecking && !IsRamming && VehicleChosen.Speed > 20f)
                {
                    int Prob = Util.RandomInt(0, 10);
                    Vehicle Target = Util.GetVehicleTotheSideOfThatVehicle(VehicleChosen, 15f);
                    if (Prob < 10 && Flags.Contains(CriminalFlags.CAN_RAM) && Util.CanWeUse(Target) && Target.IsPersistent && Util.AreVehsGoingAtSimilarSpeeds(VehicleChosen, Target, 10f))
                    {
                        Util.DrawLine(VehicleChosen.Position, Util.GetVehicleTotheSideOfThatVehicle(VehicleChosen, 40f).Position);
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("IsRamming");
                        Function.Call(Hash.TASK_VEHICLE_ESCORT, Criminal, VehicleChosen, Target, 0, 90.0, 16777216, 1f, 1f, 30f);
                        IsRamming = true;
                        RamSpeed = VehicleChosen.Speed;
                        return;
                    }
                    Target = Util.GetVehicleBehindThatVehicle(VehicleChosen, 15f);
                    if (Prob < 2 && Flags.Contains(CriminalFlags.CAN_BRAKECHECK) && Util.CanWeUse(Target) && Target.IsPersistent && Util.AreVehsGoingAtSimilarSpeeds(VehicleChosen,Target,10f))
                    {
                        Util.DrawLine(VehicleChosen.Position, Util.GetVehicleBehindThatVehicle(VehicleChosen, 40f).Position);
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("IsBrakeChecking");
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, Criminal, VehicleChosen, 2f, DrivingStyle);
                        IsBrakeChecking = true;
                        RamSpeed = VehicleChosen.Speed;
                        return;
                    }
                }
                */
                File.AppendAllText(@"" + DangerousIndividuals.debugpath, "\n - finished?");

            }

            //if (IsRamming && (Util.IsSliding(VehicleChosen, 2f) || VehicleChosen.Speed < RamSpeed - 2f || VehicleChosen.Speed < 15f)) { IsBrakeChecking = false; IsRamming = false; Criminal.Task.ClearAll(); }
            //if (IsBrakeChecking && (VehicleChosen.Speed < RamSpeed - 10f || VehicleChosen.Speed < 15f)) { IsBrakeChecking = false; IsRamming = false; Criminal.Task.ClearAll(); }
        }
        public bool AreAllPartnersInTheCar()
        {
            int partners = 0;
            foreach (Ped partner in Partners)
            {
                if (Util.CanWeUse(partner.CurrentVehicle)) partners++;
            }
            if (partners == Partners.Count) return true;
            return false;
        }

        public void Clear()
        {
            if (Util.CanWeUse(Cargo)) Cargo.IsPersistent = false;
            Criminal.BlockPermanentEvents = true;
            Criminal.Task.ClearAll();
            if (Criminal.CurrentBlip.Exists()) Criminal.CurrentBlip.Remove();
            if (LostLOSBlip.Exists()) LostLOSBlip.Remove();

            foreach (Ped partner in Partners)
            {
               // if (partner.IsAlive) DangerousIndividuals.SplitCriminals.Add(partner);
               // else
                {
                    if (partner.CurrentBlip != null) partner.CurrentBlip.Remove();
                    partner.MarkAsNoLongerNeeded();
                }
            }
            if (InPlayerCustody) Criminal.Delete(); else Criminal.MarkAsNoLongerNeeded();
        }
    }
}
