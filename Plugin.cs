using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    public partial class MoreCamerasPlugin : BaseUnityPlugin
    {
        public const string Name = "More Cameras Plug-In";              
        public const string Guid = "org.lordashes.plugins.morecameras";
        public const string Version = "1.1.0.0";

        public static List<AuxCamera> auxCameras = new List<AuxCamera>();
        public static bool rendering = false;
        public static DiagnostiocMode debug = DiagnostiocMode.high;

        public enum DiagnostiocMode
        { 
            none = 0,
            low = 1,
            high = 2
        }

        public enum UpdateSpecifications
        {
            fullThrottle = 1,
            highPowerDevice = 1,
            mediumPowerDevice = 10,
            lowerEndDevice = 30,
            tokenIndicationOnly = 60
        }

        public class AuxCamera : IDisposable
        {
            public bool active { get; private set; } = false;
            public Camera camera { get; private set; } = null;
            public Rect projection { get; private set; } = new Rect(0,0,1920,1080);
            public UpdateSpecifications updateFrameInterval { get; set; } = UpdateSpecifications.mediumPowerDevice;
            private int updateFrameCount = 1;
            
            private Texture2D image = null;

            public AuxCamera(string name, Rect projectionScreen)
            {
                Construtor(name, projectionScreen, Vector3.zero, Vector3.zero);
            }

            public AuxCamera(string name, Rect projectionScreen, Vector3 cameraPos, Vector3 cameraRot)
            {
                Construtor(name, projectionScreen, cameraPos, cameraRot);
            }

            private void Construtor(string name, Rect projectionScreen, Vector3 cameraPos, Vector3 cameraRot)
            {
                if (debug >= DiagnostiocMode.low) { Debug.Log("More Cameras Plugin: Added Camera " + Convert.ToString(name) + " (" + projectionScreen.width + "x" + projectionScreen.height + ") at " + cameraPos.ToString() + ", " + cameraRot.ToString()); }
                this.projection = projectionScreen;
                GameObject dolly = new GameObject();
                this.camera = dolly.AddComponent<Camera>();
                this.camera.name = name;
                this.camera.rect = new Rect(0f, 0f, 0f, 0f);
                this.camera.transform.position = cameraPos;
                this.camera.transform.eulerAngles = cameraRot;
                this.active = false;
            }

            public void On()
            {
                if (debug >= DiagnostiocMode.low) { Debug.Log("More Cameras Plugin: Turned Camera " + Convert.ToString(this.camera.name) + " On"); }
                this.active = true;
            }

            public void Off()
            {
                if (debug >= DiagnostiocMode.low) { Debug.Log("More Cameras Plugin: Turned Camera " + Convert.ToString(this.camera.name) + " Off"); }
                this.active = false;
            }

            public void MoveTo(Vector3 pos)
            {
                if (debug >= DiagnostiocMode.high) { Debug.Log("More Cameras Plugin: Moved Camera " + Convert.ToString(this.camera.name) + " To "+pos.ToString()); }
                this.camera.transform.position = pos;
            }

            public void RotateTo(Vector3 rot)
            {
                if (debug >= DiagnostiocMode.high) { Debug.Log("More Cameras Plugin: Rotated Camera " + Convert.ToString(this.camera.name) + " To " + rot.ToString()); }
                this.camera.transform.eulerAngles = rot;
            }

            public void AttachTo(GameObject controller, bool syncFirst = false)
            {
                if (debug >= DiagnostiocMode.high) { Debug.Log("More Cameras Plugin: Attached Camera " + Convert.ToString(this.camera.name) + " To " + Convert.ToString(controller.name)+" (SyncFirst="+syncFirst.ToString()+")"); }
                if (syncFirst)
                {
                    this.camera.gameObject.transform.position = controller.transform.position;
                    this.camera.gameObject.transform.eulerAngles = controller.transform.eulerAngles;
                }
                this.camera.gameObject.transform.SetParent(controller.transform);
            }

            public void Render()
            {
                if (this.active)
                {
                    if (BoardSessionManager.Instance != null && this.camera != null && this.updateFrameCount >= (int)this.updateFrameInterval)
                    {
                        this.updateFrameCount = 1;
                        if (debug >= DiagnostiocMode.high) { Debug.Log("More Cameras Plugin: Rendering Camera " + Convert.ToString(this.camera.name) + " ("+this.projection.width+"x"+this.projection.height+")"); }
                        RenderTexture rt = new RenderTexture((int)this.projection.width,(int)this.projection.height, 24);
                        this.camera.enabled = true;
                        this.camera.targetTexture = rt;
                        this.camera.pixelRect = new Rect(0, 0, this.projection.width, this.projection.height);
                        this.image = new Texture2D((int)this.projection.width, (int)this.projection.height, TextureFormat.RGB24, false);
                        using (NativeArray<Unity.Rendering.FrustumPlanes.PlanePacket4> planePacketsForCam = ZoneGpuState.GetPlanePacketsForCam(this.camera, Patches.ActiveCameraManagerLateUpdatePatch.planes, Allocator.TempJob))
                        {
                            BoardSessionManager.Instance.Render(this.camera, planePacketsForCam);
                        }
                        this.camera.Render();
                        RenderTexture.active = rt;
                        this.image.ReadPixels(new Rect(0, 0, this.projection.width, this.projection.height), 0, 0);
                        this.camera.targetTexture = null;
                        RenderTexture.active = null;
                        this.image.LoadImage(this.image.EncodeToPNG());
                        this.camera.enabled = false;
                    }
                    this.updateFrameCount++;
                }
            }

            public void Project()
            {
                if (this.image != null)
                {
                    GUI.DrawTexture(new Rect(this.projection.x, this.projection.y, this.projection.width, this.projection.height), this.image, ScaleMode.ScaleToFit);
                }
            }

            public void Remove()
            {
                this.Dispose();
            }

            public void Dispose()
            {
                if (debug >= DiagnostiocMode.low) { Debug.Log("More Cameras Plugin: Removing Camera " + Convert.ToString(this.camera.name)); }
                GameObject.Destroy(this.camera.gameObject);
            }
        }

        void Awake()
        {
            UnityEngine.Debug.Log("More Cameras Plugin: Active.");

            debug = Config.Bind("Setting", "Diagnostic Logs Setting", DiagnostiocMode.low).Value;

            var harmony = new Harmony(Guid);
            harmony.PatchAll();
        }

        public static AuxCamera AddCamera(string name, Rect projectionScreen)
        {
            auxCameras.Add(new AuxCamera(name, projectionScreen));
            return auxCameras[auxCameras.Count - 1];
        }

        public static AuxCamera AddCamera(string name, Rect projectionScreen, Vector3 pos)
        {
            auxCameras.Add(new AuxCamera(name, projectionScreen, pos, Vector3.zero));
            return auxCameras[auxCameras.Count - 1];
        }

        public static AuxCamera AddCamera(string name, Rect projectionScreen, int height, Vector3 pos, Vector3 rot)
        {
            auxCameras.Add(new AuxCamera(name, projectionScreen, pos, rot));
            return auxCameras[auxCameras.Count - 1];
        }

        public static void RemoveCamera(string name)
        {
            for(int c=0; c<auxCameras.Count; c++)
            {
                if(auxCameras[c].camera.name.ToUpper()==name.ToUpper())
                {
                    auxCameras[c].Remove();
                    auxCameras.RemoveAt(c);
                    break;
                }
            }
        }

        void OnGUI()
        {
            if (!rendering) { StartCoroutine("UpdateRenderImages"); }
            foreach (AuxCamera auxCamera in auxCameras)
            {
                auxCamera.Project();
            }
        }

        IEnumerator UpdateRenderImages()
        {
            rendering = true;
            yield return new WaitForSeconds(0.01f);
            foreach (AuxCamera auxCamera in auxCameras)
            {
                auxCamera.Render();
            }
            rendering = false;
        }
    }
}
