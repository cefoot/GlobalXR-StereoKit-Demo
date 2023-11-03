using System;
using StereoKit;
using StereoKit.Framework;

namespace SKGlobalXR
{

    class Program
    {

        static Random rand = new Random();

        public class BoxData
        {
            public Pose Pose;
            public float Speed;
            public Model Model;
        }

        static void Main(string[] args)
        {
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "SKGlobalXR",
                assetsFolder = "Assets",
                blendPreference = DisplayBlend.AnyTransparent,
            };

            var passthrough = SK.GetOrCreateStepper<PassthroughMetaExt>();
            // Tex oldSkyTex;
            // SphericalHarmonics oldSkyLight;

            if (!SK.Initialize(settings))
                return;
            if (passthrough != null)
            {
                passthrough.Enabled = true;
            }

            Renderer.SkyTex = Tex.FromCubemapEquirectangular("20231103_104052_972.jpg");
            Renderer.SkyTex.OnLoaded += t => Renderer.SkyLight = t.CubemapLighting;
            var materialRed = new Material(Default.ShaderPbr);
            materialRed.Transparency = Transparency.Blend;
            materialRed[MatParamName.ColorTint] = Color.Hex(0xFF000088);
            materialRed[MatParamName.MetallicAmount] = .9f;
            materialRed[MatParamName.RoughnessAmount] = .1f;
            var materialGreen = materialRed.Copy();
            materialGreen[MatParamName.ColorTint] = Color.Hex(0x00FF0088);
            var materialBlue = materialRed.Copy();
            materialBlue[MatParamName.ColorTint] = Color.Hex(0x0000FF88);



            // Create assets used by the app


            BoxData[] boxData;
            var playing = false;
            var points = 0;
            Vec3 moveDir = new Vec3(0f, 0f, 0.1f);
            Pose menuPose = new Pose(0.5f, 0, -0.5f, Quat.LookDir(-1, 0, 1));

            Pose cubePose = GetCubeStartPose();
            Model baseBox = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                materialRed);
            boxData = new BoxData[3];
            boxData[0] = ResetBox(new BoxData
            {
                Model = Model.FromMesh(
                        Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                        materialRed)
            });
            boxData[1] = ResetBox(new BoxData
            {
                Model = Model.FromMesh(
                        Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                        materialBlue)
            });
            boxData[2] = ResetBox(new BoxData
            {
                Model = Model.FromMesh(
                        Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                        materialGreen)
            });

            Matrix floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));

            Material floorMaterial = new Material("floor.hlsl");
            floorMaterial.Transparency = Transparency.Blend;


            // Core application loop
            SK.Run(() =>
            {
                if (SK.System.displayType == Display.Opaque && !passthrough.Available)
                    Mesh.Cube.Draw(floorMaterial, floorTransform);

                foreach (var item in boxData)
                {
                    Renderer.Add(item.Model, item.Pose.ToMatrix(1f), Color.White);
                }
                if (playing)
                {
                    foreach (var item in boxData)
                    {
                        if (item.Pose.position.z > Input.Head.position.z)
                        {
                            item.Pose = GetCubeStartPose();
                            points--;
                            Sound.Unclick.Play(item.Pose.position);
                            continue;
                        }
                        for (int i = 0; i < (int)Handed.Max; i++)
                        {
                            Hand h = Input.Hand((Handed)i);
                            if (!h.IsTracked) continue;
                            if (item.Model.Bounds.Contains(h.palm.position - item.Pose.position, h.Get(FingerId.Index, JointId.Tip).position - item.Pose.position))
                            {
                                item.Pose = GetCubeStartPose();
                                points++;

                                Sound.Click.Play(item.Pose.position);
                                continue;
                            }
                        }
                        item.Pose.position += moveDir * Time.Stepf * item.Speed;
                    }

                }


                UI.WindowBegin("Menu", ref menuPose);
                UI.Label($"Punkte:{points}");
                if (UI.Button("Start"))
                {
                    points = 0;
                    playing = true;
                }
                if (UI.Button("Stop"))
                {
                    playing = false;
                }
                UI.WindowEnd();
            });
        }

        private static BoxData ResetBox(BoxData boxData)
        {
            boxData.Pose = GetCubeStartPose();
            boxData.Speed = (float)rand.NextDouble() * 10f;
            return boxData;
        }


        private static Pose GetCubeStartPose()
        {
            return new Pose((float)rand.NextDouble() * .5f - .25f, (float)rand.NextDouble() * .5f - .25f, -1f, Quat.Identity);
        }

    }
}