using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSPDispatch
{
    public class CopUnitHandler
    {

        public CopUnitHandler(CopUnitType type, SuspectHandler suspect, Vector3 pos, bool notify)
        {
            UnitType = type;
            Suspect = suspect;

            DefineUnits.DefineCopUnit(this, type, pos);
            // Util.SetRelationshipBetweenGroups(DangerousIndividuals.CopsRLGroup, DangerousIndividuals.CriminalsRLGroup, Relationship.Neutral, false);
            // Util.SetRelationshipBetweenGroups( DangerousIndividuals.CriminalsRLGroup, DangerousIndividuals.CopsRLGroup, Relationship.Hate, false);


            Leader.AddBlip();
            Leader.CurrentBlip.Sprite = BlipSprite.PoliceOfficer;
            Leader.CurrentBlip.Scale = 0.5f;


            foreach (Ped partner in Partners)
            {
                partner.AddBlip();
                partner.CurrentBlip.Sprite = BlipSprite.PoliceOfficer;
                partner.CurrentBlip.Scale = 0.5f;

            }


            CopVehicle = World.CreateVehicle(VehicleModel, pos, Leader.Heading);
            if (Util.CanWeUse(CopVehicle))
            {
                CopVehicle.EngineRunning = true;
                CopVehicle.AddBlip();
                CopVehicle.CurrentBlip.Scale = 0.5f;



                Leader.SetIntoVehicle(CopVehicle, VehicleSeat.Driver);
                Util.GetSquadIntoVehicle(Partners, CopVehicle);
                //Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, CopVehicle, 10f);

                if (CopVehicle.Model.IsHelicopter)
                {
                    CopVehicle.CurrentBlip.Sprite = BlipSprite.PoliceHelicopterAnimated;
                    CopVehicle.CurrentBlip.Scale = 1f;
                    if (CopVehicle.Model == "polmav") CopVehicle.Livery = 0;
                    CopVehicle.Position = CopVehicle.Position + (CopVehicle.UpVector * 50);

                    Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, CopVehicle);


                }
                else
                {
                    CopVehicle.SirenActive = true;
                    CopVehicle.CurrentBlip.Sprite = BlipSprite.PoliceCarDot;
                }

                if (DangerousIndividuals.AllowChaseNotifications.Checked && notify) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "UNIT JOINS PURSUIT", "Dispatch, ~b~" + CopVehicle.FriendlyName + "~w~ here, joining the pursuit from ~y~" + World.GetZoneName(Leader.Position) + "~w~.");
            }
            else
            {
                UI.Notify("~r~Failed to spawn this cop unit (" + VehicleModel + "). Removing...");
                ShouldRemoveCopUnit = true;
                return;
            }
            Leader.Weapons.Select(Leader.Weapons.BestWeapon.Hash, true);

            if (Suspect != null && Suspect.Surrendered()) SetUnitState(CopState.Arrest);
        }
        //UNIT INFO
        public Model VehicleModel = "police";
        public Model LeaderModel = "s_m_y_cop_01";
        public Model PartnerModels = "s_m_y_cop_01";

        public WeaponHash FirstWeaponHash = (WeaponHash) Game.GenerateHash("WEAPON_PISTOL");
        public WeaponHash SecondtWeaponHash = (WeaponHash)Game.GenerateHash("WEAPON_PUMPSHOTGUN");

        public CopState State = CopState.ChaseVehicle;
        public CopUnitType UnitType;
        public List<CopUnitFlags> Flags = new List<CopUnitFlags>();
        public Ped Leader;
        public List<Ped> Partners = new List<Ped>();
        public SuspectHandler Suspect;
        public int DrivingStyle = 4 + 8 + 16 + 32 + 262144;
        public int ExtraDrivingStyle = 0;

        public Vehicle CopVehicle;
        public int GameTimeWhenSpawned = Game.GameTime;
        public int BrakeTime = Game.GameTime;
        public bool Braking = false;
        public bool FuelWarning = false;
        public int NextStateCheck = Game.GameTime;

        //SCRIPT TOOLS
        float VehicleStuck = 0;

        public int RefTime = Game.GameTime;
        public bool ShouldRemoveCopUnit = false;


        
        public void HandleSpeed()
        {

            if (BrakeTime > Game.GameTime)
            {
                if (BrakeTime > Game.GameTime + 3000) BrakeTime = Game.GameTime + 3000;
                /*
                if (!Util.IntHasFlag(ExtraDrivingStyle, 1))
                {
                    ExtraDrivingStyle += 1;
                    if (Util.IntHasFlag(DrivingStyle, 4)) DrivingStyle -= 4;
                }
                
*/
                float d = (CopVehicle.Velocity.Length() * 0.9f);
                if (d > 5f) Leader.DrivingSpeed = d;

                if (!Braking)
                {

                    //  UI.Notify(CopVehicle.FriendlyName + " brakes");
                    Braking = true;
                    //Leader.DrivingSpeed = (CopVehicle.Velocity.Length() * 0.6f);
                }
            }
            else 
            {
                //  UI.Notify(CopVehicle.FriendlyName + " stops braking");
                if (Braking)
                {
                    Leader.DrivingSpeed = 200f;
                    Braking = false;
                }
                else
                {
                    if(State != CopState.ChaseVehicle)
                    {
                        if(CopVehicle.IsInRangeOf(Suspect.Criminal.Position, 30f))
                        {
                            Leader.DrivingSpeed = 20f;
                         //   UI.ShowSubtitle("50f");
                        }
                        else
                        {
                            Leader.DrivingSpeed = 60f;
                            //UI.ShowSubtitle("200f");
                        }
                    }
                }
            }
        }
        public void UpdateOnTick()
        {

           // if (Braking) CopVehicle.EngineTorqueMultiplier = 0f;
            if (Suspect != null && UnitType == CopUnitType.AirUnit && Util.IsNightTime())
            {

                /*
                void _DRAW_SPOT_LIGHT_WITH_SHADOW(float posX, float posY, float posZ,
  float dirX, float dirY, float dirZ, int colorR, int colorG,
  int colorB, float distance, float brightness, float roundness,
  float radius, float falloff, float shadow)
    */

                Vector3 pos = Vector3.Normalize(Suspect.Criminal.Position - CopVehicle.Position);
                World.DrawSpotLightWithShadow(CopVehicle.Position + new Vector3(0, 0, -2), pos, System.Drawing.Color.White, 100f, 1f, 1f, 5f, 90f);
            }


            if (DangerousIndividuals.CriminalsActive.Count == 0) ShouldRemoveCopUnit = true;

            //Cop driving skillz
            if (Suspect != null)
            {
                if (!Suspect.Auth_DeadlyForce || Suspect.Surrendered())
                {
                 if(CopVehicle.IsInRangeOf(Game.Player.Character.Position, 150f))
                    {
                        Util.HandleVehicleCarefulnessArea(this, 15f);
                        Util.HandleVehicleCarefulness(this);
                    }
                }
                if (Util.CanWeUse(Game.Player.Character.CurrentVehicle)) // && IsVehicleBehindVehicle(Game.Player.Character.CurrentVehicle, veh, true, 0f, 30f))
                {

                    Vector3 offset = Util.GetOffset(CopVehicle, Game.Player.Character.CurrentVehicle );
                    //DrawLine(veh.Position, Game.Player.Character.Position);

                    if (Util.ForwardSpeed(CopVehicle) > Util.ForwardSpeed(Game.Player.Character.CurrentVehicle) - 3f && (((Math.Abs(offset.X) < 5 && offset.Y > 0 && offset.Y < 20) || CopVehicle.IsTouching(Game.Player.Character.CurrentVehicle))))
                    {
                        BrakeTime =Game.GameTime+ 1000;
                        //UI.Notify(CopVehicle.FriendlyName + " gives way to player");
                        //Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
                    }
                }
            }



            //Suspect ramming
            if (Util.CanWeUse(Suspect.VehicleChosen) && Util.ForwardSpeed(Suspect.VehicleChosen) > 1f && (!Suspect.Auth_Ramming)) Util.HandleAntiRamSystem(CopVehicle, Suspect.VehicleChosen); //Auth_Ramming

            //Make driver stop if partners aren't in the car
            if (Util.IsInVehicle(Leader, CopVehicle) && !AreAllPartnersInTheCar())
            {
                if (Util.ForwardSpeed(CopVehicle) > 0.5f) Function.Call(Hash.APPLY_FORCE_TO_ENTITY, CopVehicle, 3, 0f, -0.6f, 0f, 0f, 0f, -0.3f, 0, true, true, true, true, true);
            }


            //Fix Leader if allowed to, else remove entire unit
            if (Leader.IsDead)
            {
                Suspect.CopsKilled++;
                if (Flags.Contains(CopUnitFlags.PARTNERS_BECOME_LEADER_IF_LEADER_DIES) && Partners.Count > 0)
                {
                    if (Leader.CurrentBlip.Exists()) Leader.CurrentBlip.Remove();
                    Leader.MarkAsNoLongerNeeded();
                    Leader = Partners[0];
                    Partners.RemoveAt(0);
                }
                else
                {
                    if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~DISPATCH", "OFFICER DOWN", "~r~" + CopVehicle.FriendlyName + " has been shot down.");
                    ShouldRemoveCopUnit = true;
                }
            }


            //    Leader.DrivingStyle =(DrivingStyle) DrivingStyle + ExtraDrivingStyle;
        }

        public bool IsSurrendered(SuspectHandler suspect)
        {
            return (Suspect.State == CriminalState.Surrendering || Suspect.State == CriminalState.Arrested || Suspect.State == CriminalState.DealtWith);
        }

        public void HandleDrivingStyle()
        {
            if (!CopVehicle.IsOnAllWheels) return;
            
            
            if (Suspect != null)
            {
                if(!CopVehicle.IsStopped)
                {
                    if (!Util.IntHasFlag(ExtraDrivingStyle, 4194304))
                    {
                        if (State != CopState.Arrest && Util.RoadTravelDistance(Leader.Position, Suspect.Criminal.Position) > Leader.Position.DistanceTo(Suspect.Criminal.Position) * 3 && Leader.IsInRangeOf(Suspect.Criminal.Position, 150f))
                        {
                            ExtraDrivingStyle += 4194304;
                            //UI.ShowSubtitle("Offroad added for " + CopVehicle.FriendlyName);
                        }
                    }
                    else
                    {
                        if (Util.RoadTravelDistance(Leader.Position, Suspect.Criminal.Position) < Leader.Position.DistanceTo(Suspect.Criminal.Position) * 1.5f || !Leader.IsInRangeOf(Suspect.Criminal.Position, 200f))
                        {
                            ExtraDrivingStyle -= 4194304;
                         //   UI.ShowSubtitle("Offroad removed for " + CopVehicle.FriendlyName);
                        }
                    }
                }                
            }
                                


            if (Util.CanWeUse(Leader))
            {
                Leader.DrivingStyle = (DrivingStyle)(DrivingStyle + ExtraDrivingStyle);
            //    Leader.DrivingSpeed = 25f;
            }
        }
        public bool FindNewTarget(bool aloneOnly, bool surrenderedOnly)
        {
            if (DangerousIndividuals.CriminalsActive.Count > 0)
            {
                SuspectHandler target = DangerousIndividuals.CriminalsActive[Util.RandomInt(0, DangerousIndividuals.CriminalsActive.Count - 1)];
                if (aloneOnly)
                {
                    if (DangerousIndividuals.CriminalsAlone.Count > 0) target = DangerousIndividuals.CriminalsAlone[0];
                }

                if (surrenderedOnly)
                {
                    if (DangerousIndividuals.CriminalsSurrendered.Count > 0) target = DangerousIndividuals.CriminalsSurrendered[0];
                }

                if (target != null && target != Suspect)
                {
                    Suspect = target;
                    SetUnitState(CopState.ChaseVehicle);
                    return true;
                }
            }
            return false;
        }

        /*
        public bool FindNewTarget()
        {
            SuspectHandler test = DangerousIndividuals.GetRandomActiveCriminal();
            if (test != null)
            {
                if (Suspect == null || Suspect != test)
                {
                    Suspect = test;
                    SetUnitState(CopState.ChaseVehicle);
                    return true;
                }
            }
            return false;
        }


        public bool FindNewSuspectToArrest(bool AllowBeingArrested)
        {
            SuspectHandler test = DangerousIndividuals.GetRandomSurrenderedCriminal(AllowBeingArrested);
            if (test != null)
            {
                if (Suspect == null || Suspect != test)
                {
                    Suspect = test;
                    if (Suspect.CopArrestingMe == null) //ADD CANT_ARREST SUPPORT
                    {
                        if (CanArrest())
                        {
                            Suspect.CopArrestingMe = this;
                            SetUnitState(CopState.Arrest);
                        }
                        else
                        {
                            Leader.Task.ClearAll();
                        }
                    }
                    else
                    {
                        if (Suspect.CopArrestingMe == this)
                        {
                            if (CanArrest())
                            {
                                SetUnitState(CopState.Arrest);
                            }
                            else
                            {
                                Leader.Task.ClearAll();
                            }
                        }
                        else
                        {
                            if (CanArrest())
                            {
                                SetUnitState(CopState.SecureArrest);
                            }
                            else
                            {
                                Leader.Task.ClearAll();
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

    */
        public void Handlecombat()
        {

            //Tasing functions
            if (Flags.Contains(CopUnitFlags.ATTEMPTS_TASING) && !IsSurrendered(Suspect))
            {
                if (!Suspect.Auth_DeadlyForce)
                {
                    if (Leader.Weapons.Current.Hash != WeaponHash.StunGun)
                    {
                        Leader.Weapons.Give(WeaponHash.StunGun, -1, true, true);
                    }
                    Leader.CanSwitchWeapons = false;
                    foreach (Ped partner in Partners)
                    {
                        if (partner.Weapons.Current.Hash != WeaponHash.StunGun)
                        {
                            partner.Weapons.Give(WeaponHash.StunGun, -1, true, true);
                            partner.CanSwitchWeapons = false;
                        }
                    }
                }
                else
                {
                    Leader.CanSwitchWeapons = true;
                    if (Leader.Weapons.Current.Hash == WeaponHash.StunGun) Leader.Weapons.Remove(WeaponHash.StunGun);
                    foreach (Ped partner in Partners)
                    {
                        partner.CanSwitchWeapons = true;
                        if (partner.Weapons.Current.Hash == WeaponHash.StunGun)
                        {
                            partner.Weapons.Remove(WeaponHash.StunGun);
                        }
                    }
                }
            }

            //Partner removal
            Ped ToRemove = null;
            foreach (Ped partner in Partners) if (partner.IsDead) { ToRemove = partner; break; }
            if (Util.CanWeUse(ToRemove))
            {
                Suspect.CopsKilled++;
                if (ToRemove.CurrentBlip.Exists()) ToRemove.CurrentBlip.Remove();
                Partners.Remove(ToRemove);
                ToRemove.MarkAsNoLongerNeeded();
            }



            //Vehicular combat (Army Heli)
            if (UnitType == CopUnitType.ArmyAirUnit && Util.CanWeUse(Suspect.Criminal.CurrentVehicle) && Util.RandomInt(0, 10) < 2)
            {
                if (Util.GetPedVehicleWeapon(Leader) != (int)Util.VehicleWeapons.NO_WEAPON)
                {
                    Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, Leader, 0, Suspect.Criminal.Position.X, Suspect.Criminal.Position.Y, Suspect.Criminal.Position.Z);
                }

                foreach (Ped ped in Partners)
                {
                    if (Util.GetPedVehicleWeapon(ped) != (int)Util.VehicleWeapons.NO_WEAPON)
                    {
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, ped, 0, Suspect.Criminal.Position.X, Suspect.Criminal.Position.Y, Suspect.Criminal.Position.Z);
                    }
                }


                World.ShootBullet(Leader.Position + (Leader.UpVector * -2), Suspect.Criminal.Position, Leader, "blista2", 20, 2f);
            }
        }

        public void HandleUnitStates()
        {
            //Prevent thinking if this unit only pursues
            if (Flags.Contains(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR))
            {
                if (Suspect.Surrendered())
                {
                    FindNewTarget(false, false);
                }
                return;
            }

            //If suspect not valid, find new one
            if (!DangerousIndividuals.IsSuspectValid(Suspect) || Suspect.State == CriminalState.DealtWith)
            {
                FindNewTarget(false, false);
            }



            if (DangerousIndividuals.IsSuspectValid(Suspect))
            {
                if (Suspect.Surrendered())
                {
                    //Arresting / Securing arrest
                    if (CanArrest())
                    {
                        if (State == CopState.Arrest)
                        {
                            if (!Util.AnyBackseatEmpty(CopVehicle))
                            {
                                if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "SUSPECT IN CUSTODY", "Suspect in custody, I'm out with them.");
                                ShouldRemoveCopUnit = true;
                            }


                            if (Suspect.InPlayerCustody)
                            {
                                SetUnitState(CopState.SecureArrest);
                            }
                            else
                            {
                                //If there's someone arresting the criminal
                                if (DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                                {
                                    // If i'm not the one arresting the criminal
                                    //Keeps bugging out                        
                                    if (Suspect.CopArrestingMe != this)
                                    {
                                        if (!FindNewTarget(true, false)) SetUnitState(CopState.SecureArrest);
                                    }
                                }
                                else
                                {
                                    Suspect.CopArrestingMe = this;
                                    SetUnitState(CopState.Arrest);
                                    //FindNewTarget(false, false);
                                    //if (!FindNewTarget() && FindNewSuspectToArrest(false)) FindNewSuspectToArrest(true);
                                }
                            }

                        }
                        else if (State == CopState.SecureArrest)
                        {
                            if (Suspect.InPlayerCustody)
                            {
                                FindNewTarget(false,false);
                            }
                            else
                            {
                                if (DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                                {
                                    if (Suspect.CopsChasingMe.Count > 2)
                                    {
                                        FindNewTarget(false, false);

                                        if(!Suspect.Criminal.IsInRangeOf(Leader.Position, 200f)) ShouldRemoveCopUnit = true;
                                    }
                                    else if (Suspect.CopArrestingMe == this || Suspect.CopArrestingMe.State == CopState.SecureArrest)
                                    {
                                        Suspect.CopArrestingMe = this;
                                        SetUnitState(CopState.Arrest);
                                    }
                                }
                                else
                                {
                                    if (DangerousIndividuals.IsSuspectValid(Suspect))
                                    {
                                        if (Suspect.Surrendered())
                                        {
                                            if (!DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                                            {
                                                Suspect.CopArrestingMe = this;
                                                SetUnitState(CopState.Arrest);
                                            }
                                            else
                                            {
                                                SetUnitState(CopState.SecureArrest);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else //If i'm not arresting nor securing arrest
                        {
                            if (Suspect.CopArrestingMe == null) SetUnitState(CopState.Arrest); else SetUnitState(CopState.SecureArrest);
                        }
                    }
                    else if (!FindNewTarget(false, false))
                    {
                        SetUnitState(CopState.SecureArrest);
                    }
                }
                else
                {
                    //Fleeing Suspects        
                    if (State == CopState.FightCriminals) Leader.BlockPermanentEvents = false; else Leader.BlockPermanentEvents = true;

                    if (Suspect.Criminal.IsInVehicle())
                    {
                        if (Suspect.Criminal.CurrentVehicle.Speed > 5f)
                        {
                            SetUnitState(CopState.ChaseVehicle);
                        }
                    }
                    else
                    {
                        if (State != CopState.TaseAttempt && !Suspect.InCombat)
                        {
                            SetUnitState(CopState.ChaseFoot);
                        }
                        if (Suspect.InCombat || Suspect.Flags.Contains(CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF))
                        {
                            SetUnitState(CopState.FightCriminals);
                        }
                    }
                }
            }
            else
            {
                FindNewTarget(false, false);
            }


            // Arrest behavior
            /*
            if (CanArrest())
            {
                if (State == CopState.Arrest)
                {
                    if (DangerousIndividuals.IsSuspectValid(Suspect))
                    {
                        if (!Util.AnyBackseatEmpty(CopVehicle))
                        {
                            if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "SUSPECT IN CUSTODY", "Suspect in custody, I'm out with them.");
                            ShouldRemoveCopUnit = true;
                        }

                        if (DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                        {
                            //Keeps bugging out                        
                            if (Suspect.CopArrestingMe != this)
                            {
                                if (!FindNewTarget(true, false)) SetUnitState(CopState.SecureArrest);
                            }
                        }
                        else
                        {
                            FindNewTarget(false, false);
                        }
                    }
                    else
                    {
                        FindNewTarget(false, false);
                    }
                }
                else if (State == CopState.SecureArrest)
                {
                    if (DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                    {

                        if (Suspect.CopsChasingMe.Count > 2) FindNewTarget(false, false);
                        else if (Suspect.CopArrestingMe == this) SetUnitState(CopState.Arrest);
                        else if (Suspect.CopArrestingMe.State == CopState.SecureArrest) SetUnitState(CopState.Arrest);
                    }
                    else
                    { //Not arresting nor securing arrest, but the suspect is surrendered and has no cops arresting it
                        if (DangerousIndividuals.IsSuspectValid(Suspect))
                        {
                            if (Suspect.Surrendered())
                            {
                                if (!DangerousIndividuals.IsCopUnitValid(Suspect.CopArrestingMe))
                                {
                                    Suspect.CopArrestingMe = this;
                                    SetUnitState(CopState.Arrest);
                                }
                                else
                                {
                                    SetUnitState(CopState.SecureArrest);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (Suspect.Surrendered()) FindNewTarget(false, false);
            }

            //Surrendered suspects
            if (DangerousIndividuals.IsSuspectValid(Suspect))
            {
                if (Suspect.Surrendered())
                {
                    if (!CanArrest())
                    {
                        if (!FindNewTarget(false, false))
                        {
                            SetUnitState(CopState.SecureArrest);
                        }
                    }
                    else
                    {
                        if (State != CopState.Arrest && State != CopState.SecureArrest)
                        {
                            if (Suspect.CopArrestingMe == null)
                            {
                                if (!CopVehicle.IsUpsideDown && Leader.IsInRangeOf(Suspect.Criminal.Position, 50f))
                                {
                                    Suspect.CopArrestingMe = this;
                                    SetUnitState(CopState.Arrest);
                                }
                            }
                            else
                            {
                                if (Suspect.CopArrestingMe == this)
                                {
                                    SetUnitState(CopState.Arrest);
                                    //if (Util.IsCarDrivable(CopVehicle) && CopVehicle.IsUpsideDown) Suspect.CopArrestingMe = null;
                                }
                                else
                                {
                                    if (!FindNewTarget(false, false))
                                    {
                                        SetUnitState(CopState.SecureArrest);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //Fleeing Suspects        
                    if (State == CopState.FightCriminals) Leader.BlockPermanentEvents = false; else Leader.BlockPermanentEvents = true;

                    if (Suspect.Criminal.IsInVehicle())
                    {
                        if (Suspect.Criminal.CurrentVehicle.Speed > 5f)
                        {
                            SetUnitState(CopState.ChaseVehicle);
                        }
                    }
                    else
                    {
                        if (State != CopState.TaseAttempt && !Suspect.InCombat)
                        {
                            SetUnitState(CopState.ChaseFoot);
                        }
                        if (Suspect.InCombat || Suspect.Flags.Contains(CriminalFlags.WILL_NOT_FLEE_ALWAYS_STANDOFF))
                        {
                            SetUnitState(CopState.FightCriminals);
                        }
                    }
                }
            }
                        */

        }

        public bool WorthDrivingToFootCriminal()
        {
            float VehDist = Leader.Position.DistanceTo(CopVehicle.Position);
            float CriminalDist = Leader.Position.DistanceTo(Suspect.Criminal.Position);

            if (CriminalDist > 40 && VehDist < CriminalDist) return true;
            return false;
        }
        public bool CanArrest()
        {
            return !Flags.Contains(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR) && !Flags.Contains(CopUnitFlags.CANT_ARREST);
        }
        public void SetUnitState(CopState state)
        {
            if (State != state)
            {
                if (DangerousIndividuals.DebugNotifications.Checked) UI.ShowSubtitle(UnitType.ToString() + "~n~Old State :" + State.ToString() + "~n~~r~New State: " + state.ToString());
                State = state;
                Leader.Task.ClearAll();
                NextStateCheck = Game.GameTime + 1000;
            }
        }
        public void Update()
        {
            //if (Suspect == null && !FindNewTarget() && !FindNewSuspectToArrest(true)) ShouldRemoveCopUnit = true;

            HandleSpeed();

            if (Util.IsDriving(Leader) || !CopVehicle.IsStopped)
            {
                HandleDrivingStyle();

            }
           // if (Util.CheckForObstaclesAhead(CopVehicle)) BrakeTime = Game.GameTime + 1000;


            //Stuck handler
            if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 50f) && Leader.IsInVehicle(CopVehicle) && AreAllPartnersInTheCar())
            {
                if (CopVehicle.Model.IsCar)
                {
                    if (CopVehicle.Speed < 1f) VehicleStuck++; else if (VehicleStuck > 0) VehicleStuck = 0f;
                }

                if (VehicleStuck > 20f)
                {
                    Util.MoveEntitytoNearestRoad(CopVehicle);
                    Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, CopVehicle, 10f);
                    VehicleStuck = 0;
                    if (DangerousIndividuals.DebugNotifications.Checked) UI.Notify("Vehicle stuck, relocating.");
                }
            }
            else if (VehicleStuck > 0) VehicleStuck = 0;


            if (Util.IsSubttaskActive(Leader, Util.Subtask.ENTERING_VEHICLE_ENTERING) && !Leader.IsInRangeOf(Game.Player.Character.Position, 200f))
            {
                Leader.SetIntoVehicle(CopVehicle, VehicleSeat.Driver);
                Util.GetSquadIntoVehicle(Partners, CopVehicle);
            }

            Handlecombat();
            if(NextStateCheck < Game.GameTime) HandleUnitStates();

            if (Flags.Contains(CopUnitFlags.PURSUIT_EXCLUSIVE_NO_DYNAMIC_BEHAVIOR))
            {
                //Makesure helis chase suspects (MAY CAUSE HELIS TO SHOOT AT SUSPECTS, NEEDS REWRITE)
                if (CopVehicle.Model.IsHelicopter)
                {
                    if (!Leader.IsInCombat && DangerousIndividuals.IsSuspectValid(Suspect))
                    {
                        if (UnitType == CopUnitType.AirUnit) Function.Call(Hash.TASK_HELI_MISSION, Leader, CopVehicle, 0, Suspect.Criminal, 0, 0, 0, 6, 60f, 40f, 270f, 0f, 30, 50f, 0);
                        else Function.Call(Hash.TASK_HELI_MISSION, Leader, CopVehicle, 0, Suspect.Criminal, 0, 0, 0, 23, 60f, 40f, 270f, 0f, 20, 50f, 0);
                    }
                    if (!DangerousIndividuals.IsSuspectValid(Suspect))
                    {
                        if (DangerousIndividuals.DebugNotifications.Checked) UI.ShowSubtitle("HELI SUSPECT NOT VALID");
                        FindNewTarget(false, false);
                    }

                    //Make helis retreat if damaged
                    if (!Util.CanWeUse(CopVehicle) || (CopVehicle.EngineHealth < 400 || CopVehicle.Health < 300))
                    {
                        if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "AIR UNIT OUT", "We heavily damaged, " + CopVehicle.FriendlyName + " is of the pursuit.");
                        ShouldRemoveCopUnit = true;
                        return;
                    }
                    if (Game.GameTime > GameTimeWhenSpawned + (60000 * 2) && !FuelWarning) // 3 Minutes of fuel for the helis
                    {
                        if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "AIR UNIT ON LOW FUEL", "We're almost out of fuel. We will have to return to base shortly.");
                        FuelWarning = true;
                    }
                    if (Game.GameTime > GameTimeWhenSpawned + (60000 * 3)) // 3 Minutes of fuel for the helis
                    {
                        if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "AIR UNIT OUT", "Out of fuel, we're out of the pursuit.");
                        ShouldRemoveCopUnit = true;
                        return;
                    }
                }

            }
            else
            {
                switch (State)
                {
                    case CopState.ChaseVehicle:
                        {
                            if (Util.CanWeUse(CopVehicle) && Util.IsCarDrivable(CopVehicle))
                            {
                                if (Leader.IsOnFoot)
                                {
                                    if (Game.Player.Character.IsInRangeOf(Leader.Position, 200f))
                                    {
                                        Leader.Task.EnterVehicle(CopVehicle, VehicleSeat.Driver, -1, 20f);
                                    }
                                    else
                                    {
                                        //Leader.Task.EnterVehicle(CopVehicle, VehicleSeat.Driver, -1, 20);
                                        Leader.SetIntoVehicle(CopVehicle, VehicleSeat.Driver);
                                    }
                                }
                                if (Util.IsInVehicle(Leader, CopVehicle))
                                {
                                    if (CopVehicle.IsStopped && !CopVehicle.IsInRangeOf(Suspect.Criminal.Position, 50f))
                                    {
                                        if (AreAllPartnersInTheCar() && !Util.IsDriving(Leader))
                                        {
                                            //Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 7, 100f, DrivingStyle, 0f, 5f, false);
                                            Leader.Task.VehicleChase(Suspect.Criminal);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (Suspect.Criminal.IsInRangeOf(Leader.Position, 40f))
                                {
                                    if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "UNIT LEAVES PURSUIT", "Vehicle heavily damaged, I'm out of the pursuit.");
                                    ShouldRemoveCopUnit = true;
                                }
                            }
                            break;
                        }
                    case CopState.ChaseFoot:
                        {
                            Leader.BlockPermanentEvents = true;
                            if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 5f))
                            {
                                if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 30f) && Leader.IsSittingInVehicle())
                                {
                                    if (!Util.IsDriving(Leader))
                                    {
                                        if (Leader.IsInRangeOf(Suspect.Criminal.Position, 100f)) Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 4, 20f, DrivingStyle + 4194304, 0f, 2f, true);
                                        else
                                        {
                                            Vector3 pedpos = Suspect.Criminal.Position;
                                            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE, Leader, CopVehicle, pedpos.X, pedpos.Y, pedpos.Z, 100f, DrivingStyle, 10f);
                                        }
                                    }
                                    //Leader.Task.DriveTo(CopVehicle, Suspect.Criminal.Position, 5f, 20f, DrivingStyle);
                                    //AI::TASK_VEHICLE_MISSION_PED_TARGET(ped, vehicle, ped, 8, FLOAT, 0x402c423d, 350.0, -1.0, 1);

                                    //8= flees;1=drives around 4=drives and stops near, 7=follows 10=follows to the left,11=follows right,12 = follows behind?,13=follows ahead,14=follows, stop when near
                                }
                                else
                                {
                                    if (WorthDrivingToFootCriminal())
                                    {
                                        if (!Util.IsDriving(Leader))
                                        {
                                            Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 4, 20f, DrivingStyle + 4194304, 0f, 2f, true);
                                        }
                                    }
                                    else
                                    {
                                        Leader.Task.RunTo(Suspect.Criminal.Position); //if (Suspect.Auth_DeadlyForce) Leader.BlockPermanentEvents=false; else 
                                    }
                                }
                            }
                            if (Leader.IsInCombat) Leader.Task.ClearAll();
                            if (Leader.IsInRangeOf(Suspect.Criminal.Position, 10f))
                            {

                                if (Util.CanWeUse(Suspect.Criminal.CurrentVehicle))
                                {
                                    if (!Util.IsSubttaskActive(Leader, Util.Subtask.ENTERING_VEHICLE_GENERAL)) Leader.Task.EnterVehicle(Suspect.Criminal.CurrentVehicle, VehicleSeat.Driver, -1, 5f);
                                }
                                else
                                {
                                    if(!Suspect.Auth_DeadlyForce && Flags.Contains(CopUnitFlags.ATTEMPTS_TASING)) State = CopState.TaseAttempt;
                                }
                            }

                            break;
                        }
                    case CopState.TaseAttempt:
                        {
                            int pattern = 0;
                            unchecked
                            {
                                pattern = (int)FiringPattern.FullAuto;
                            }

                            if (Leader.IsInRangeOf(Suspect.Criminal.Position, 4f))
                            {
                                if (!Util.IsSubttaskActive(Leader, Util.Subtask.AIMED_SHOOTING_ON_FOOT)) Leader.Task.ShootAt(Suspect.Criminal.Position, 2000, FiringPattern.FullAuto);
                            }
                            else
                            {
                                if (!Util.IsSubttaskActive(Leader, Util.Subtask.AIMED_SHOOTING_ON_FOOT)) Function.Call(Hash.TASK_GO_TO_ENTITY_WHILE_AIMING_AT_ENTITY, Leader, Suspect.Criminal, Suspect.Criminal, 5f, true, 2f, 30f, true, true, pattern);
                            }

                            if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 10f)) SetUnitState(CopState.ChaseFoot);
                            if (Suspect.State == CriminalState.Surrendering) SetUnitState(CopState.Arrest);
                            break;
                        }
                    case CopState.FightCriminals:
                        {
                            //if (!Leader.IsOnFoot) Leader.Task.LeaveVehicle();
                            if (!Leader.IsInCombat)
                            {
                                if (!Util.IsDriving(Leader))
                                {
                                    Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 7, 100f, DrivingStyle, 0f, 5f, true);
                                }
                            }
                            Leader.BlockPermanentEvents = false;
                            break;
                        }
                    case CopState.Arrest:
                        {

                    
                            if (!Util.IsPlayingAnim(Leader, "mp_arresting", "a_arrest_on_floor"))
                            {
                                if (Flags.Contains(CopUnitFlags.CANT_ARREST))
                                {
                                    //if (!FindNewTarget()) SetUnitState(CopState.SecureArrest); else SetUnitState(CopState.ChaseFoot);
                                }
                                else
                                {
                                    if (Suspect.CopArrestingMe != null)
                                    {
                                        if (Suspect.CopArrestingMe == this)
                                        {
                                            //is cop closer to criminal
                                            if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 30f))
                                            {
                                                if (!Util.IsSubttaskActive(Leader, Util.Subtask.ENTERING_VEHICLE_GENERAL) && !Util.IsDriving(Leader) && CopVehicle.IsStopped)
                                                {
                                                    if (Leader.IsInRangeOf(Suspect.Criminal.Position, 60f))
                                                    {
                                                       //Leader.Task.DriveTo(CopVehicle, Suspect.Criminal.Position, 10f, 15f, DrivingStyle);
                                                      Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 4, 30f, DrivingStyle, 10f, 1f, true);

                                                    }
                                                    else
                                                    {
                                                        Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 4, 30f, DrivingStyle, 30f, 1f, true);

                                                       // Leader.Task.DriveTo(CopVehicle, Suspect.Criminal.Position, 25f, 25f, DrivingStyle);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (Leader.IsStopped)
                                                {
                                                    CriminalState cstate = Suspect.State;

                                                    if(cstate == CriminalState.Surrendering)
                                                    {

                                                        if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 2f))
                                                        {
                                                            if (Suspect.State != CriminalState.Arrested)
                                                            {
                                                                if (Leader.Weapons.Current.Hash != WeaponHash.Unarmed)
                                                                {
                                                                    Function.Call(Hash.TASK_GO_TO_ENTITY_WHILE_AIMING_AT_ENTITY, Leader, Suspect.Criminal, Suspect.Criminal, 2f, false, 1.5f, 1f, true, true, 0);
                                                                }
                                                                else Function.Call(Hash.TASK_GO_TO_ENTITY, Leader, Suspect.Criminal, -1, 1.5f, 2f, 1f, 0);
                                                            }
                                                        }
                                                    }
                                                    if (cstate == CriminalState.Arrested || cstate == CriminalState.DealtWith)
                                                    {

                                                        float speed = 1f;
                                                        if (DangerousIndividuals.CriminalsActive.Count > 1) speed = 3f;
                                                        if (Suspect.Criminal.IsSittingInVehicle(CopVehicle))
                                                        {
                                                            if (Leader.IsOnFoot)
                                                            {
                                                                Leader.Task.EnterVehicle(CopVehicle, VehicleSeat.Driver, 10000, 1f);
                                                                /*
                                                                if (Util.GetDoorFromSeat(CopVehicle, Suspect.Criminal.SeatIndex))
                                                                {

                                                                    Function.Call(Hash.TASK_OPEN_VEHICLE_DOOR, Leader, CopVehicle, -1, (int)Util.GetDoorFromSeat(CopArrestingMe.CopVehicle, seat), 2f);

                                                                }*/
                                                            }
                                                            else if (!Util.IsDriving(Leader))
                                                            {
                                                                Leader.Task.DriveTo(CopVehicle, Util.GetClosestLocation(Leader.Position, Info.AllPoliceStations), 20f, 30f, 1 + 2 + 8 + 16 + 32 + 128);
                                                                CopVehicle.SirenActive = true;
                                                            }

                                                        }
                                                        else
                                                        {
                                                            if (Leader.IsInRangeOf(Suspect.Criminal.Position, 4f)) Function.Call(Hash.TASK_GO_TO_ENTITY, Leader, CopVehicle, -1, 4f, speed, 1f, 0);
                                                            //Function.Call(Hash.TASK_GO_TO_ENTITY, Leader, CopVehicle, -1, 4f, speed, 1f, 0);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }


                            if (Suspect.Criminal.IsInPoliceVehicle)
                            {
                                if (Suspect.Criminal.CurrentVehicle == CopVehicle)
                                {
                                    //Suspect.ShouldRemoveCriminal = true;

                                    if (Util.AnyBackseatEmpty(CopVehicle))
                                    {
                                        // if (!FindNewTarget() && !FindNewSuspectToArrest(false)) FindNewSuspectToArrest(true);
                                    }
                                    else
                                    {
                                    //    ShouldRemoveCopUnit = true;
                                        if (DangerousIndividuals.AllowChaseNotifications.Checked) Util.AddNotification("web_lossantospolicedept", "~b~" + CopVehicle.FriendlyName + " unit", "Suspect in custody", "Suspect in custody, I'm out with them.");
                                    }
                                    //string bool sex = Suspect.Criminal.Is
                                }
                                else
                                {
                                    //FindNewTarget();
                                }
                            }
                            break;
                        }
                    case CopState.SecureArrest:
                        {

                            if (Leader.IsInRangeOf(Suspect.Criminal.Position, 150f) && Suspect.CopArrestingMe==null)
                            {
                                SetUnitState(CopState.Arrest);
                            }
                            //if (Leader.IsInCombat) Leader.Task.ClearAll();
                            if (Suspect.Criminal.IsSittingInVehicle() && Util.IsPoliceVehicle(Suspect.Criminal.CurrentVehicle))
                            {

                                if (!Util.IsDriving(Leader))
                                {
                                    Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, Leader, CopVehicle, 20f, 1+2+4+8+16+32+128);
                                }

                                if(!Leader.IsInRangeOf(Game.Player.Character.Position, 200f)) ShouldRemoveCopUnit = true;

                                break;
                            }

                            if (!Leader.IsInRangeOf(Suspect.Criminal.Position, 30f))
                            {
                                if (!Util.IsSubttaskActive(Leader, Util.Subtask.ENTERING_VEHICLE_GENERAL) && !Util.IsDriving(Leader))
                                {

                                    Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET, Leader, CopVehicle, Suspect.Criminal, 4, 30f, DrivingStyle, 25f, 1f, true);

                                   // Leader.Task.DriveTo(CopVehicle, Suspect.Criminal.Position, 25f, 15f, DrivingStyle);
                                }
                            }
                            else
                            {
                                if (Leader.IsStopped && !Leader.IsInRangeOf(Suspect.Criminal.Position, 10f) && !Util.IsSubttaskActive(Leader, Util.Subtask.AIMING_GUN))
                                {

                                    Function.Call(Hash.TASK_GO_TO_ENTITY, Leader, Suspect.Criminal, -1, 4f, 2f, 5f, 0);
                                   // Function.Call(Hash.TASK_GO_TO_ENTITY_WHILE_AIMING_AT_ENTITY, Leader, Suspect.Criminal, Suspect.Criminal, 1f, false, 4f, 1f, true, true, 0);
                                }
                            }

                            break;
                        }
                    case CopState.AwaitingOrders:
                        {
                            if (Leader.IsStopped) Leader.Task.WanderAround(Leader.Position, 5f);
                            break;
                        }

                }
            }
        }
        public bool AreAllPartnersInTheCar()
        {
            int partners = 0;
            foreach (Ped partner in Partners)
            {
                if (Util.CanWeUse(partner.CurrentVehicle)) partners++;
            }

            if (partners == Partners.Count) return true;
            else
            {
                if (!Game.Player.Character.IsInRangeOf(Leader.Position, 200f))
                {
                    Util.GetSquadIntoVehicle(Partners, CopVehicle);
                }
            }
            return false;
        }


        public void Clear()
        {
            if (Util.CanWeUse(CopVehicle))
            {
                if (CopVehicle.CurrentBlip != null) CopVehicle.CurrentBlip.Remove();
                CopVehicle.SirenActive = false;
                Leader.Task.EnterVehicle(CopVehicle, VehicleSeat.Driver, -1, 1f);

                CopVehicle.MarkAsNoLongerNeeded();
            }




            Leader.BlockPermanentEvents = true;
            //if(!CopVehicle.Model.IsHelicopter)
            Leader.Task.ClearAll();
            if (Leader.CurrentBlip != null) Leader.CurrentBlip.Remove();
            Leader.MarkAsNoLongerNeeded();

            foreach (Ped partner in Partners)
            {
                if (partner.CurrentBlip != null) partner.CurrentBlip.Remove();

                partner.MarkAsNoLongerNeeded();
            }
        }
    }
}
