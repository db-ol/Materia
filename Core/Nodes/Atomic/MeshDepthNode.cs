﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Materia.Imaging.GLProcessing;
using Materia.Textures;
using Materia.Geometry;
using Materia.Nodes.Attributes;
using Newtonsoft.Json;
using RSMI;
using Materia.Math3D;
using System.IO;
using Materia.Material;
using Materia.MathHelpers;
using System.Threading;
using Materia.Archive;

namespace Materia.Nodes.Atomic
{
    public class MeshDepthNode : ImageNode
    {
        private MTGArchive archive;

        string relativePath;
        string path;
        [Editable(ParameterInputType.MeshFile, "Mesh File", "Content")]
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                if (string.IsNullOrEmpty(path))
                {
                    relativePath = "";
                }
                else
                {
                    relativePath = System.IO.Path.Combine("resources", System.IO.Path.GetFileName(path));
                }
                TriggerValueChange();
            }
        }

        [Editable(ParameterInputType.Toggle, "Resource", "Content")]
        public bool Resource
        {
            get; set;
        }

        MVector position;
        [Editable(ParameterInputType.Float3Input, "Position")]
        public MVector Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                TriggerValueChange();
            }
        }

        MVector rotation;
        [Editable(ParameterInputType.Float3Slider, "Rotation", "Default", 0, 360)]
        public MVector Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
                TriggerValueChange();
            }
        }

        MVector scale;
        [Editable(ParameterInputType.Float3Input, "Scale")]
        public MVector Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                TriggerValueChange();
            }
        }

        float cameraZoom;

        [Editable(ParameterInputType.FloatInput, "Camera Z")]
        public float CameraZ
        {
            get
            {
                return cameraZoom;
            }
            set
            {
                cameraZoom = value;
                TriggerValueChange();
            }
        }

        MeshDepthProcessor processor;
        MeshRenderer mesh;
        NodeOutput Output;

        static Matrix4 Proj = Matrix4.CreatePerspectiveFieldOfView(40 * ((float)Math.PI / 180.0f), 1, 0.03f, 1000.0f);
        static PBRMaterial mat = new PBRDepth();

        public MeshDepthNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            Name = "Mesh Depth";

            Id = Guid.NewGuid().ToString();

            width = w;
            height = h;

            tileX = tileY = 1;

            scale = new MVector(1, 1, 1);
            position = new MVector(0, 0, 0);
            rotation = new MVector(0, 0, 0);
            cameraZoom = 3;

            previewProcessor = new BasicImageRenderer();
            processor = new MeshDepthProcessor();

            internalPixelType = p;

            Output = new NodeOutput(NodeType.Gray, this);
            Outputs.Add(Output);
        }

        private void ReadMeshFile()
        {
            if (mesh == null && meshes == null)
            {
                if (archive != null && !string.IsNullOrEmpty(relativePath) && Resource)
                {
                    archive.Open();
                    List<MTGArchive.ArchiveFile> files = archive.GetAvailableFiles();

                    var m = files.Find(f => f.path.Equals(relativePath));
                    if (m != null)
                    {
                        using (Stream ms = m.GetStream())
                        {
                            RSMI.Importer imp = new Importer();
                            meshes = imp.Parse(ms);
                            archive.Close();
                            return;
                        }
                    }

                    archive.Close();
                }


                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    RSMI.Importer imp = new Importer();
                    meshes = imp.Parse(path);
                }
                else if (!string.IsNullOrEmpty(relativePath) && ParentGraph != null && !string.IsNullOrEmpty(ParentGraph.CWD) && File.Exists(System.IO.Path.Combine(ParentGraph.CWD, relativePath)))
                {
                    var p = System.IO.Path.Combine(ParentGraph.CWD, relativePath);

                    RSMI.Importer imp = new Importer();
                    meshes = imp.Parse(p);
                }
            }
        }

        private void LoadMesh()
        {
            if(string.IsNullOrEmpty(path))
            {
                if(mesh != null)
                {
                    mesh.Release();
                }

                mesh = null;
            }

            if(meshes != null && meshes.Count > 0)
            {
                mesh = new MeshRenderer(meshes[0]);
                meshes.Clear();
                meshes = null;
            }
        }

        public override void TryAndProcess()
        {
            ReadMeshFile();
            LoadMesh();
            Process();
        }

        List<RSMI.Containers.Mesh> meshes;
        void Process()
        {
            if (mesh == null) return;

            CreateBufferIfNeeded();

            mesh.Mat = mat;

            float rx = (float)this.rotation.X * ((float)Math.PI / 180.0f);
            float ry = (float)this.rotation.Y * ((float)Math.PI / 180.0f);
            float rz = (float)this.rotation.Z * ((float)Math.PI / 180.0f);

            Quaternion rot = Quaternion.FromEulerAngles(rx, ry, rz);
            Matrix4 rotation = Matrix4.CreateFromQuaternion(rot);
            Matrix4 translation = Matrix4.CreateTranslation(position.X, position.Y, position.Z);
            Matrix4 scale = Matrix4.CreateScale(this.scale.X, this.scale.Y, this.scale.Z);

            Matrix4 view = rotation * Matrix4.CreateTranslation(0, 0, -cameraZoom);
            Vector3 pos = Vector3.Normalize((view * new Vector4(0, 0, 1, 1)).Xyz) * cameraZoom;

            mesh.View = view;
            mesh.CameraPosition = pos;
            mesh.Projection = Proj;

            //TRS
            mesh.Model = scale * translation;

            //light position currently doesn't do anything
            //just setting values to a default
            mesh.LightPosition = new Vector3(0, 0, 0);
            mesh.LightColor = new Vector3(1, 1, 1);

            processor.TileX = tileX;
            processor.TileY = tileY;
            processor.Mesh = mesh;
            processor.Process(width, height, buffer);
            processor.Complete();

            Output.Data = buffer;
            TriggerTextureChange();
        }

        public class MeshDepthNodeData : NodeData
        {
            public string path;
            public string relativePath;
            public bool resource;

            public float translateX;
            public float translateY;
            public float translateZ;

            public float scaleX;
            public float scaleY;
            public float scaleZ;

            public float rotationX;
            public float rotationY;
            public float rotationZ;

            public float cameraZoom;
        }

        public override void FromJson(string data, MTGArchive arch = null)
        {
            archive = arch;
            FromJson(data);
        }

        public override void FromJson(string data)
        {
            MeshDepthNodeData d = JsonConvert.DeserializeObject<MeshDepthNodeData>(data);
            SetBaseNodeDate(d);

            path = d.path;
            Resource = d.resource;
            relativePath = d.relativePath;

            position = new MVector(d.translateX, d.translateY, d.translateZ);
            scale = new MVector(d.scaleX, d.scaleY, d.scaleZ);
            rotation = new MVector(d.rotationX, d.rotationY, d.rotationZ);

            cameraZoom = d.cameraZoom;
        }

        public override string GetJson()
        {
            MeshDepthNodeData d = new MeshDepthNodeData();
            FillBaseNodeData(d);

            d.path = path;
            d.relativePath = relativePath;
            d.resource = Resource;

            d.translateX = position.X;
            d.translateY = position.Y;
            d.translateZ = position.Z;

            d.scaleX = scale.X;
            d.scaleY = scale.Y;
            d.scaleZ = scale.Z;

            d.rotationX = rotation.X;
            d.rotationY = rotation.Y;
            d.rotationZ = rotation.Z;

            d.cameraZoom = cameraZoom;

            return JsonConvert.SerializeObject(d);
        }

        public override void CopyResources(string CWD)
        {
            if (!Resource) return;

            CopyResourceTo(CWD, relativePath, path);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (mesh != null)
            {
                mesh.Release();
                mesh = null;
            }

            if(processor != null)
            {
                processor.Release();
            }
        }
    }
}
