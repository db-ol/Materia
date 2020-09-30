﻿using System;
using Materia.Rendering.Attributes;
using Newtonsoft.Json;
using Materia.Rendering.Imaging.Processing;
using Materia.Rendering.Textures;
using Materia.Graph;

namespace Materia.Nodes.Atomic
{
    public class InputNode : ImageNode
    {
        public new int Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public new int Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public new float TileX
        {
            get
            {
                return tileX;
            }
            set
            {
                tileX = value;
            }
        }

        public new float TileY
        {
            get
            {
                return tileY;
            }
            set
            {
                tileY = value;
            }
        }

        NodeOutput Output;

        public InputNode(GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            Id = Guid.NewGuid().ToString();

            Name = "Input";

            internalPixelType = p;

            //this actually does nothing for this node
            width = 16;
            height = 16;
            tileX = tileY = 1;

            previewProcessor = new BasicImageRenderer();

            //only an output is present
            Output = new NodeOutput(NodeType.Color | NodeType.Gray, this);
            Outputs.Add(Output);
        }

        public override GLTexture2D GetActiveBuffer()
        {
            if(Inputs.Count > 0 && Inputs[0].HasInput)
            {
                return Inputs[0].Reference.Node.GetActiveBuffer();
            }

            return null;
        }

        public override byte[] GetPreview(int width, int height)
        {
            GLTexture2D active = GetActiveBuffer();

            if (active == null) return null;
            if (active.Id == 0) return null;

            previewProcessor.Process(width, height, active);
            byte[] bits = previewProcessor.ReadByte(width, height);
            previewProcessor.Complete();
            return bits;
        }

        public override void TryAndProcess()
        {
            Process();
        }

        void Process()
        {
            if (Inputs.Count == 0 || !Inputs[0].HasInput) return;

            GLTexture2D i1 = (GLTexture2D)Inputs[0].Reference.Data;

            if (i1 == null) return;
            if (i1.Id == 0) return;

            width = i1.Width;
            height = i1.Height;

            Output.Data = i1;
            TriggerTextureChange();
        }

        public override void FromJson(string data)
        {
            NodeData d = JsonConvert.DeserializeObject<NodeData>(data);
            SetBaseNodeDate(d);

            Output = new NodeOutput(NodeType.Color | NodeType.Gray, this);
            Outputs.Clear();
            Outputs.Add(Output);
        }

        public override string GetJson()
        {
            NodeData d = new NodeData();
            FillBaseNodeData(d);

            return JsonConvert.SerializeObject(d);
        }
    }
}