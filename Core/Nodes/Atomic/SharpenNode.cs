﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Materia.Imaging.GLProcessing;
using Materia.Nodes.Attributes;
using Materia.Textures;
using Newtonsoft.Json;
using Materia.Nodes.Helpers;

namespace Materia.Nodes.Atomic
{
    public class SharpenNode : ImageNode
    {
        NodeInput input;
        NodeOutput Output;

        SharpenProcessor processor;

        protected float intensity;
        [Promote(NodeType.Float)]
        [Editable(ParameterInputType.FloatSlider, "Intensity", "Default", 1, 10)]
        public float Intensity
        {
            get
            {
                return intensity;
            }
            set
            {
                intensity = value;
                TriggerValueChange();
            }
        }

        public SharpenNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            Name = "Sharpen";
            Id = Guid.NewGuid().ToString();
            width = w;
            height = h;

            tileX = tileY = 1;

            previewProcessor = new BasicImageRenderer();
            processor = new SharpenProcessor();

            intensity = 1;

            internalPixelType = p;

            input = new NodeInput(NodeType.Gray | NodeType.Color, this, "Image");
            Output = new NodeOutput(NodeType.Gray | NodeType.Color, this);

            Inputs.Add(input);
            Outputs.Add(Output);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (processor != null)
            {
                processor.Release();
                processor = null;
            }
        }

        private void GetParams()
        {
            if (!input.HasInput) return;

            pintensity = intensity;

            if (ParentGraph != null && ParentGraph.HasParameterValue(Id, "Intensity"))
            {
                pintensity = Utils.ConvertToFloat(ParentGraph.GetParameterValue(Id, "Intensity"));
            }
        }

        public override void TryAndProcess()
        {
            GetParams();
            Process();
        }

        float pintensity;
        void Process()
        {
            if (!input.HasInput) return;

            GLTextuer2D i1 = (GLTextuer2D)input.Reference.Data;

            if (i1 == null) return;
            if (i1.Id == 0) return;

            if (processor == null) return;

            CreateBufferIfNeeded();

            processor.TileX = tileX;
            processor.TileY = tileY;

            processor.Intensity = pintensity;

            processor.Process(width, height, i1, buffer);
            processor.Complete();

            Output.Data = buffer;
            TriggerTextureChange();
        }

        public class SharpenData : NodeData
        {
            public float intensity;
        }

        public override void FromJson(string data)
        {
            SharpenData d = JsonConvert.DeserializeObject<SharpenData>(data);
            SetBaseNodeDate(d);
            intensity = d.intensity;
        }

        public override string GetJson()
        {
            SharpenData d = new SharpenData();
            FillBaseNodeData(d);
            d.intensity = intensity;

            return JsonConvert.SerializeObject(d);
        }
    }
}
