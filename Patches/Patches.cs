using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace LordAshes
{
    public partial class MoreCamerasPlugin : BaseUnityPlugin
    {
        static class Patches
        {
            [HarmonyPatch(typeof(ActiveCameraManager), "LateUpdate")]
            internal class ActiveCameraManagerLateUpdatePatch
            {
                public static Plane[] planes = null;

                internal static void Prefix(ref Plane[] ____tmpCamPlanes, ref Camera ____camera, ref BoardSessionManager ____boardSessionManager)
                {
                    planes = ____tmpCamPlanes;
                }
            }
        }
    }
}
