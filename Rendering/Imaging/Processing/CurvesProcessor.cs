﻿using Materia.Rendering.Textures;
using Materia.Rendering.Interfaces;
using Materia.Rendering.Mathematics;

namespace Materia.Rendering.Imaging.Processing
{
    public class CurvesProcessor : ImageProcessor
    {
        IGLProgram shader;
        GLTexture2D CurveLUT;
        public CurvesProcessor(GLTexture2D lut) : base()
        {
            shader = GetShader("image.glsl", "curve.glsl");
            CurveLUT = lut;
        }

        public override void Process(int width, int height, GLTexture2D tex, GLTexture2D output)
        {
            base.Process(width, height, tex, output);

            if (shader != null)
            {
                ResizeViewTo(tex, output, tex.Width, tex.Height, width, height);
                tex = output;
                IGL.Primary.Clear((int)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

                Vector2 tiling = new Vector2(TileX, TileY);

                shader.Use();
                shader.SetUniform2("tiling", ref tiling);
                shader.SetUniform("MainTex", 0);
                IGL.Primary.ActiveTexture((int)TextureUnit.Texture0);
                tex.Bind();
                shader.SetUniform("CurveLUT", 1);
                IGL.Primary.ActiveTexture((int)TextureUnit.Texture1);
                CurveLUT.Bind();

                if (renderQuad != null)
                {
                    renderQuad.Draw();
                }

                GLTexture2D.Unbind();
                //output.Bind();
                //output.CopyFromFrameBuffer(width, height);
                //GLTexture2D.Unbind();
                Blit(output, width, height);
            }
        }
    }
}