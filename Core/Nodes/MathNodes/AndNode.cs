﻿using Materia.MathHelpers;
using Materia.Nodes.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Materia.Nodes.MathNodes
{
    public class AndNode : MathNode
    {
        NodeOutput output;

        static int MIN_INPUTS = 2;

        public AndNode(int w, int h, GraphPixelType p = GraphPixelType.RGBA) : base()
        {
            //we ignore w,h,p

            CanPreview = false;

            Name = "And";
            Id = Guid.NewGuid().ToString();
            shaderId = "S" + Id.Split('-')[0];

            output = new NodeOutput(NodeType.Bool, this);

            for (int i = 0; i < 2; ++i)
            {
                var input = new NodeInput(NodeType.Bool, this, "Bool Input " + i);
                input.OnInputChanged += Input_OnInputChanged;
                Inputs.Add(input);
            }
            Outputs.Add(output);
        }

        private void Input_OnInputChanged(NodeInput n)
        {
            int inputsFilled = 0;
            for (int i = 0; i < Inputs.Count; ++i)
            {
                var input = Inputs[i];
                if (input.HasInput)
                {
                    ++inputsFilled;
                }
            }

            if (inputsFilled >= Inputs.Count - 1)
            {
                AddPlaceholderInput();
            }
            // minus 2 to account for an empty one
            // +1 to account for execute pin
            else if (inputsFilled < Inputs.Count - 2 && inputsFilled > MIN_INPUTS + 1)
            {
                for (int i = MIN_INPUTS + 1; i < Inputs.Count; ++i)
                {
                    var input = Inputs[i];
                    Inputs.RemoveAt(i);
                    RemovedInput(input);
                    --i;
                }
            }
        }

        protected override void AddPlaceholderInput()
        {
            var input = new NodeInput(NodeType.Bool, this, "Bool Input " + Inputs.Count);
            Inputs.Add(input);
            AddedInput(input);
        }

        public override string GetShaderPart(string currentFrag)
        {
            var s = shaderId + "1";

            string compute = "";
            string sep = "";

            foreach (var inp in Inputs)
            {
                if (inp != executeInput)
                {
                    if (inp.HasInput)
                    {
                        var index = inp.Reference.Node.Outputs.IndexOf(inp.Reference);
                        var n1id = (inp.Reference.Node as MathNode).ShaderId;

                        n1id += index;

                        compute += sep + n1id + " > 0";
                        sep = " && ";
                    }
                }
            }

            if (string.IsNullOrEmpty(compute)) return "";

            return "float " + s + " = (" + compute + ") ? 1 : 0;\r\n";
        }

        public override void TryAndProcess()
        {
            bool result = true;
            foreach(var inp in Inputs)
            {
                if (inp != executeInput)
                {
                    if(inp.IsValid)
                    {
                        float f = inp.Data.ToFloat();
                        if (f <= 0)
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }

            output.Data = result ? 1 : 0;
            this.result = output.Data?.ToString();
        }
    }
}
