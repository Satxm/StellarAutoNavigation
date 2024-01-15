using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using System;
using rail;

namespace AutoNavigate
{
    public class AutoStellarNavigation
    {
        private const int THRUSTER_LEVEL_FLY = 1;
        private const int THRUSTER_LEVEL_SAIL = 2;
        private const int THRUSTER_LEVEL_WARP = 3;

        public class Target
        {
            public bool isSet = false;
            public static double AU = 40000;
            public static double s_FocusParam = 0.01;
            private PlanetData m_PlanetData = null;
            private StarData m_StarData = null;
            private EnemyDFHiveSystem m_EnemyDFHiveSystem = null;
            private SpaceSector m_SpaceSector = null;
            private int enemyId = 0;

            private EnemyData m_Enemy
            {
                get { 
                    if(m_SpaceSector == null || !TargetSanity)
                    {
                        return new EnemyData
                        {
                            id = 0
                        };
                    }
                    return m_SpaceSector.enemyPool[enemyId];
                 }
            }

            public void Reset()
            {
                m_PlanetData = null;
                m_StarData = null;
                m_EnemyDFHiveSystem = null;
                m_SpaceSector = null;
                enemyId = 0;
                isSet = false;
            }

            private void FinishSetTarget()
            {
                isSet = true;
#if DEBUG
                ModDebug.Log(String.Format("Target selected! Position: {0}", Position));
#endif
            }

            public bool TargetSanity
            {
                get
                {
                    if (!isSet) return false;
                    if (m_PlanetData != null) return true;
                    if (m_StarData != null) return true;
                    if (m_EnemyDFHiveSystem != null) return true;
                    if (m_SpaceSector != null && enemyId > 0 && enemyId < m_SpaceSector.enemyPool.Length)
                    {
                        if (m_SpaceSector.enemyPool[enemyId].id == 0 || m_SpaceSector.enemyPool[enemyId].dfTinderId == 0)
                            return false;
                        return true;
                    }
                    return false;
                }
            }

            public void SetTarget(SpaceSector sector, int enemyId)
            {
                Reset();
                m_SpaceSector = sector;
                this.enemyId = enemyId;
                FinishSetTarget();
            }

            public void SetTarget(StarData star)
            {
                Reset();
                m_StarData = star;
                FinishSetTarget();
            }

            public void SetTarget(EnemyDFHiveSystem hive)
            {
                Reset();
                m_EnemyDFHiveSystem = hive;
                FinishSetTarget();
            }

            public void SetTarget(PlanetData planet)
            {
                Reset();
                m_PlanetData = planet;
                FinishSetTarget();
            }

            public string GetText()
            {
                if (m_PlanetData != null)
                {
                    return "Stellar";
                }
                else if (m_StarData != null)
                {
                    return "Galaxy";
                }
                else if (m_EnemyDFHiveSystem != null)
                {
                    return "Hive";
                }
                else if (isTinderValid)
                {
                    return "Tinder";
                }
                else
                {
                    ModDebug.Error("Get Text while no target!!!");
                    return "";
                }
            }

            public PlanetData TargetPlanet => m_PlanetData;

            public StarData TargetStar => m_StarData;

            public EnemyDFHiveSystem TargetHive => m_EnemyDFHiveSystem;


            public static bool IsFocusingNormalized(VectorLF3 dirL, VectorLF3 dirR)
            {
                return (dirL - dirR).magnitude < s_FocusParam;
            }

            public VectorLF3 Position
            {
                get
                {
                    if (TargetPlanet != null)
                    {
                        return TargetPlanet.uPosition;
                    }
                    else if (TargetStar != null)
                    {
                        return TargetStar.uPosition;
                    }
                    else if (m_EnemyDFHiveSystem != null)
                    {
                        int index = m_EnemyDFHiveSystem.hiveAstroId;
                        return m_EnemyDFHiveSystem.sector.astros[index - 1000000].uPos;
                    }
                    else if (isTinderValid && m_Enemy.positionIsValid)
                    {
                        return m_Enemy.pos;
                    }
                    else
                    {
                        ModDebug.Error("Get Target Position while no target!!!");
                        return new VectorLF3(0.0, 0.0, 0.0);
                    }
                }
            }

            public StarData TargetStarSystem
            {
                get
                {
                    if (TargetPlanet != null)
                    {
                        return TargetPlanet.star;
                    }
                    else if (TargetStar != null)
                    {
                        return TargetStar;
                    }
                    else if (m_EnemyDFHiveSystem != null)
                    {
                        return TargetHive.starData;
                    }
                    else
                    {
                        if (!isTinderValid)
                            ModDebug.Error("Get Star Data while no target!!!");
                        return null;
                    }
                }
            }

            public double GetDistance(Player __instance)
            {
                return (Position - __instance.uPosition).magnitude;
            }

            public VectorLF3 GetDirection(Player __instance)
            {
                return (Position - __instance.uPosition).normalized;
            }

            public bool isTinderValid
            {
                get
                {
                    return m_Enemy.id > 0 && m_Enemy.dfTinderId > 0;
                }
            }
            public double Radius
            {
                get
                {
                    if(m_PlanetData != null)
                    {
                        return m_PlanetData.realRadius;
                    }
                    else if(m_StarData != null)
                    {
                        return m_StarData.physicsRadius;
                    }
                    else if(m_EnemyDFHiveSystem != null)
                    {
                        return 0.5 * AU;
                    }
                    else if (isTinderValid)
                    {
                        return 0;
                    }
                    else 
                    {
                        ModDebug.Error("Get Radius while no target!!!");
                        return 0;
                    }
                }
            }


            /// <summary>
            /// If the distance between player and the target is less than lambda * (radius + eps), return true
            /// eps = 0.01 AU
            /// </summary>
            public bool IsCloseToTarget(Player player)
            {
                float lambda = player.warping && m_EnemyDFHiveSystem != null? 3 : 1;
                return GetDistance(player) <= (Radius + 0.01 * AU) * lambda;
            }
        }

        //Class Config
        public class NavigationConfig
        {
            public ConfigEntry<double> speedUpEnergylimit;
            public ConfigEntry<double> wrapEnergylimit;
            public double planetNearastDistance;
            public int sparseStarPlanetCount;
            public double sparseStarPlanetNearastDistance;
            public double focusParam;
            public double longNavUncoverRange;
            public double shortNavUncoverRange;
            public ConfigEntry<bool> enableLocalWrap;
            public ConfigEntry<double> localWrapMinDistance;
        }

        public Target target;
        public Text modeText;
        private Player player;

        // State
        public bool isHistoryNav = false;

        //private bool __enable;

        public bool enable
        {
            get;
            private set;
        }

        public bool Pause()
        {
            if (enable)
            {
                ModDebug.Log("SafePause");
                enable = false;
                isHistoryNav = true;
                return true;
            }
            return false;
        }

        public bool Resume()
        {
            if (isHistoryNav == true && !enable)
            {
                ModDebug.Log("SafeResume");
                enable = true;
                isHistoryNav = false;
                return true;
            }
            isHistoryNav = false;
            return false;
        }

        public bool sailSpeedUp;

        //Instance Config
        private NavigationConfig config;

        private bool useConfigFile = true;
        private double speedUpEnergyLimit;
        private double wrapEnergyLimit;
        private double planetNearestDistance;
        private int sparseStarPlanetCount;
        private double sparseStarPlanetNearestDistance;
        private double longNavUncoverRange;
        private double shortNavUncoverRange;
        private bool enableLocalWrap;
        private double localWrapMinDistance;

        public AutoStellarNavigation(NavigationConfig config)
        {
            this.config = config;

            target = new Target();
            Reset();
            target.Reset();
        }


        /// <summary>
        /// Start/stop navigation according to the current state.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public void ToggleNavigate(Player player)
        {
            // In navigation or wrong target or close to the target, stop navigation
            if (enable || !target.isSet || DetermineArrive(player))
            {
#if DEBUG
                ModDebug.Log(string.Format("isEnable: {0}", enable));
                ModDebug.Log(string.Format("isSet: {0}", target.isSet));
#endif
                Arrive();
                return;
            }
            // Otherwise start navigation
#if DEBUG
            ModDebug.Log("Start Navigation");
#endif
            enable = true;
        }

        public string GetText()
        {
            return target.GetText();
        }

        public void Reset()
        {
            target.Reset();
            isHistoryNav = false;
            player = null;
            enable = false;
            sailSpeedUp = false;

            if (useConfigFile)
            {
                speedUpEnergyLimit = config.speedUpEnergylimit.Value > 0 ? config.speedUpEnergylimit.Value : 0;
                wrapEnergyLimit = config.wrapEnergylimit.Value > 0 ? config.wrapEnergylimit.Value : 0;
                planetNearestDistance = 60000.0;
                sparseStarPlanetCount = 2;
                sparseStarPlanetNearestDistance = 200000.0;
                Target.s_FocusParam = 0.02;
                longNavUncoverRange = 1000.0;
                shortNavUncoverRange = 100.0;
                enableLocalWrap = config.enableLocalWrap.Value;
                localWrapMinDistance = config.localWrapMinDistance.Value > 0 ? config.localWrapMinDistance.Value : 0.0;
            }
            else
            {
                speedUpEnergyLimit = 50000000.0;
                wrapEnergyLimit = 1000000000;
                planetNearestDistance = 60000.0;
                sparseStarPlanetCount = 2;
                sparseStarPlanetNearestDistance = 200000.0;
                Target.s_FocusParam = 0.02;
                longNavUncoverRange = 1000.0;
                shortNavUncoverRange = 100.0;
                enableLocalWrap = false;
                localWrapMinDistance = 0.0;
            }
        }

        /// <summary>
        /// Stop navigation if user try to control ikaros.
        /// </summary>
        public void HandlePlayerInput()
        {
            if (!enable)
                return;

            if (VFInput._moveForward.onDown ||
                VFInput._moveBackward.onDown ||
                VFInput._moveLeft.onDown ||
                VFInput._moveRight.onDown)
            {
                Arrive();
            }
        }

        public void Arrive(string extraTip = null)
        {
            string tip = "Navigation Mode Ended".LocalText();

            if (extraTip != null)
                tip += ("-" + extraTip);

            Reset();
            UIRealtimeTip.Popup(tip);
            if (modeText != null && modeText.IsActive())
            {
                modeText.gameObject.SetActive(false);
                modeText.text = string.Empty;
            }

            return;
        }

        /// <summary>
        /// Check if the player arrives the destination 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool DetermineArrive(Player player)
        {

            if (target.TargetPlanet != null)
            {
                return GameMain.localPlanet != null && GameMain.localPlanet.id == target.TargetPlanet.id;
            }
            else if (target.TargetStar != null)
            {
                return GameMain.localStar != null && GameMain.localStar.id == target.TargetStar.id && IsCloseToNearStar(player);
            }
            else
            {
                return target.IsCloseToTarget(player);
            }
        }

        public bool AdvisePointIfOcclusion(PlayerMove_Sail __instance, ref VectorLF3 advisePoint, double radiusOffest)
        {
            StarData localStar = GameMain.localStar;
            if (localStar == null)
            {
                return false;
            }

            VectorLF3 srcPoint = __instance.player.uPosition;
            VectorLF3 dstPoint = target.Position;

            bool hit = false;

            // A vector point to the hit location.
            Math.Line3D hitPlaneVertical = new Math.Line3D();
            double uncoverRadius = 0;

            Math.Line3D line = new Math.Line3D(srcPoint, dstPoint);
            Math.Plane3D plane = new Math.Plane3D();
            plane.normal = line.dir;

            // For every planet/star in the local system, the following codes select the largest astroid (with largest minHitRange)
            // and try to avoid occlusion by sailing to the advise point of this astroid.
            // Though maybe problematic when multiple occlusions happen, it works fine in practice.

            //Planet occlusion
            for (int index = 0; index < localStar.planetCount; ++index)
            {
                PlanetData planet = localStar.planets[index];
                plane.point = planet.uPosition;

                //Target planet
                if (target.TargetPlanet != null && planet.id == target.TargetPlanet.id)
                    continue;

                VectorLF3 intersection = plane.GetIntersection(line);

                double minHitRange = planet.realRadius + radiusOffest;

                if (intersection.Distance(planet.uPosition) < minHitRange)
                {
                    // This condition will always be true?
                    // intersection.Distance(srcPoint) + intersection.Distance(dstPoint) <= (dstPoint.Distance(srcPoint) + 0.1)
                    hit = true;

                    //Maximum radius plane
                    if (minHitRange > uncoverRadius)
                    {
                        uncoverRadius = minHitRange;
                        hitPlaneVertical.src = planet.uPosition;

                        if (planet.uPosition != intersection)
                            hitPlaneVertical.dst = intersection;
                        //Rare case
                        else
                            hitPlaneVertical.dst = plane.GetAnyPoint();
                    }
                }
            }

            // Hive Occlusion
            // Hive will attack the player if the player is close enough to the HiveSystem.
            // To avoid this case, we treat the hive as a 0.5AU sphere.
            // TODO: may be too big, not implemented


            // Star Occlusion
            plane.point = localStar.uPosition;
            if (target.TargetStar == null || localStar.id == target.TargetStar.id)
            {
                VectorLF3 intersection = plane.GetIntersection(line);

                double minHitRange = localStar.physicsRadius + radiusOffest;
                if (intersection.Distance(localStar.uPosition) < minHitRange)
                {
                    hit = true;
                    //Maximum radius plane
                    if (minHitRange > uncoverRadius)
                    {
                        uncoverRadius = minHitRange;
                        hitPlaneVertical.src = localStar.uPosition;

                        if (localStar.uPosition != intersection)
                            hitPlaneVertical.dst = intersection;
                        //Rare case
                        else
                            hitPlaneVertical.dst = plane.GetAnyPoint();
                    }
                }
            }

            if (hit)
            {
#if DEBUG
                ModDebug.Log("AdvisePointIfOcclusion Hit");
#endif
                VectorLF3 uncoverOrbitPoint = hitPlaneVertical.src + (hitPlaneVertical.dir * (uncoverRadius + 10));
                Math.Line3D uncoverLine = new Math.Line3D(dstPoint, uncoverOrbitPoint);
                plane.normal = uncoverLine.dir;
                plane.point = srcPoint;

                advisePoint = plane.GetIntersection(uncoverLine);
            }

            return hit;
        }

        /// <summary>
        /// Get the distance to the nearest planet in the local star system, if there are no planet, return Double.MaxValue
        /// </summary>
        private double NearestPlanetDistance(Player player)
        {
            // ...
            StarData localStar = GameMain.localStar;
            double distance = Double.MaxValue;
            if (localStar != null)
            {
                for (int index = 0; index < localStar.planetCount; ++index)
                {
                    PlanetData planet = localStar.planets[index];
                    double magnitude = (planet.uPosition - player.uPosition).magnitude;

                    if (magnitude < distance)
                        distance = magnitude;
                }
            }
            return distance;
        }

        /// <summary>
        ///  Check if the ikaros is close to any planet/star in the star system.
        /// </summary>
        public bool IsCloseToNearStar(Player player)
        {
            if (GameMain.localStar == null)
                return false;

            double starDistance = (GameMain.localStar.uPosition - player.uPosition).magnitude;
            return System.Math.Min(NearestPlanetDistance(player), starDistance) < planetNearestDistance;
        }

        /// <summary>
        /// Check if the distance can use wrap
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        private bool CanWrapNavigate(PlayerMove_Sail __instance)
        {
            return target.GetDistance(__instance.player) > System.Math.Max(localWrapMinDistance, target.Radius + longNavUncoverRange);
        }

        public void Navigation(PlayerMove_Sail __instance)
        {
            player = __instance.player;

            // Check if the target exists
            if(!target.TargetSanity)
            {
                Arrive("Target Destroyed");
                return;
            }

            // Check if the player is close to the destination
            if (DetermineArrive(player))
            {
                Arrive();
                Warp.TryLeaveWarp(__instance);
                return;
            }

            // Try to leave the planet
            PlanetData localPlanet = GameMain.localPlanet;
            if (localPlanet != null)
            {
#if DEBUG
                ModDebug.Log("Leave Local Planet");
#endif
                VectorLF3 dir = (__instance.player.uPosition - localPlanet.uPosition).normalized;
                Sail.SetDir(__instance, dir);
                Sail.TrySpeedUp(this, __instance);
                return;
            }

            // Check occulusion
            StarData localStar = GameMain.localStar;
            bool isLocalNav = localStar != null && target.TargetStarSystem != null && localStar.id == target.TargetStarSystem.id;
            double radiusOffset = isLocalNav ? shortNavUncoverRange : longNavUncoverRange;

            VectorLF3 advisePoint = VectorLF3.zero;
            if (AdvisePointIfOcclusion(__instance, ref advisePoint, radiusOffset))
            {
                Sail.SetDir(__instance, (advisePoint - __instance.player.uPosition).normalized);
                Sail.TrySpeedUp(this, __instance);
                return;
            }

            // Start Navigation.
            if (isLocalNav)
                LocalNavigation(__instance);
            else
                WrapNavigation(__instance);
        }

        public void LocalNavigation(PlayerMove_Sail __instance)
        {
            if (enableLocalWrap && CanWrapNavigate(__instance))
                WrapNavigation(__instance);
            else
                SailNavigation(__instance);
        }

        public void SailNavigation(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDirection(__instance.player);
            Sail.SetDir(__instance, dir);
            if (Target.IsFocusingNormalized(dir, __instance.player.uVelocity.normalized))
            {
#if DEBUG
                ModDebug.Log("Sail Navigate - Speed Up");
#endif
                Sail.TrySpeedUp(this, __instance);
            }
        }

        public void WrapNavigation(PlayerMove_Sail __instance)
        {
            VectorLF3 dir = target.GetDirection(__instance.player);
            Sail.SetDir(__instance, dir);

            if (Target.IsFocusingNormalized(dir, __instance.player.uVelocity.normalized) && !__instance.player.warping)
            {
                if (__instance.player.mecha.coreEnergy >= wrapEnergyLimit && Warp.TryWrap(this, __instance))
                {
#if DEBUG
                    ModDebug.Log("Enter Wrap");
#endif
                    return;
                }
                else if (CanSpeedUp())
                {
#if DEBUG
                    ModDebug.Log("Try SpeedUp");
#endif
                    Sail.TrySpeedUp(this, __instance);
                }
            }

            bool CanSpeedUp()
            {
                if (__instance.player.mecha.coreEnergy >= speedUpEnergyLimit)
                {
                    if (__instance.player.mecha.thrusterLevel < THRUSTER_LEVEL_WARP)
                        return true;
                    else if (!Warp.HasWarper(__instance))
                        return true;
                    else if (!CanWrapNavigate(__instance))
                        return true;
                }
                //Prepare warp or cannot speed up
                return false;
            }
        }

        public static class Warp
        {
            public static bool HasWarper(PlayerMove_Sail __instance) =>
                 GetWarperCount(__instance) > 0;

            public static int GetWarperCount(PlayerMove_Sail __instance)
            {
                return __instance.player.mecha.warpStorage.GetItemCount(1210);
            }

            public static bool TryWrap(AutoStellarNavigation self, PlayerMove_Sail __instance)
            {
                if (HasWarpChance(self, __instance))
                {
                    TryEnterWarp(__instance);
                    return true;
                }
                return false;
            }

            public static bool HasWarpChance(AutoStellarNavigation self, PlayerMove_Sail __instance)
            {
                if (__instance.player.mecha.thrusterLevel < THRUSTER_LEVEL_WARP)
                    return false;

                if (__instance.mecha.coreEnergy <
                    __instance.mecha.warpStartPowerPerSpeed * (double)__instance.mecha.maxWarpSpeed)
                    return false;

                if (!HasWarper(__instance))
                    return false;
                
                
                if (self.CanWrapNavigate(__instance))
                    return true;

                return false;
            }

            public static bool TryEnterWarp(PlayerMove_Sail __instance, bool playSound = true)
            {
                if (!__instance.player.warping && __instance.player.mecha.UseWarper())
                {
                    __instance.player.warpCommand = true;
                    if (playSound)
                    {
                        VFAudio.Create("warp-begin", __instance.player.transform, Vector3.zero, true);
                    }
                    //GameMain.gameScenario.NotifyOnWarpModeEnter();

                    return true;
                }

                return false;
            }

            public static bool TryLeaveWarp(PlayerMove_Sail __instance, bool playSound = true)
            {
                if (__instance.player.warping)
                {
                    __instance.player.warpCommand = false;
                    if (playSound)
                    {
                        VFAudio.Create("warp-end", __instance.player.transform, Vector3.zero, true);
                    }

                    return true;
                }

                return false;
            }
        }

        public static class Sail
        {
            public static void SetDir(PlayerMove_Sail __instance, VectorLF3 dir) =>
                __instance.controller.fwdRayUDir = dir;

            public static void TrySpeedUp(AutoStellarNavigation __this, PlayerMove_Sail __instance)
            {
                if (__instance.player.mecha.coreEnergy >= __this.speedUpEnergyLimit)
                {
                    __this.sailSpeedUp = true;
                }
            }
        }

        public static class Fly
        {
            public static bool TrySwtichToSail(PlayerMove_Fly __instance)
            {
#if DEBUG
                ModDebug.Log("Try Swtich To Sail");
#endif

                if (__instance.mecha.thrusterLevel < THRUSTER_LEVEL_SAIL)
                    return false;

                //取消建造模式
                if (__instance.controller.cmd.type == ECommand.Build)
                    __instance.controller.cmd.type = ECommand.None;

                __instance.controller.movementStateInFrame = EMovementState.Sail;
                __instance.controller.actionSail.ResetSailState();

                GameCamera.instance.SyncForSailMode();
                GameMain.gameScenario.NotifyOnSailModeEnter();

                return true;
            }
        }

        public static class WalkOrDrift
        {
            public static bool TrySwitchToFly(PlayerMove_Walk __instance)
            {
#if DEBUG
                ModDebug.Log("Try Switch To Fly");
#endif

                if (__instance.mecha.thrusterLevel < THRUSTER_LEVEL_FLY)
                    return false;

                __instance.jumpCoolTime = 0.3f;
                __instance.jumpedTime = 0.0f;

                __instance.flyUpChance = 0.0f;
                __instance.SwitchToFly();

                return true;
            }

            public static bool TrySwitchToFly(PlayerMove_Drift __instance)
            {
#if DEBUG
                ModDebug.Log("Try Switch To Fly");
#endif

                if (__instance.mecha.thrusterLevel < THRUSTER_LEVEL_FLY)
                    return false;

                __instance.SwitchToFly();

                return true;
            }
        }
    }
}