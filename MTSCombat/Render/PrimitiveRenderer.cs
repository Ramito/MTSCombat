using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Render
{
    public sealed class PrimitiveRenderer
    {
        private GraphicsDevice mDevice;
        private BasicEffect mBasicEffect;
        private VertexBuffer mVertexBuffer;
        private VertexPositionColor[] mVertexBufferData;
        private int mCurrentIndex = -1;

        public void Render()
        {
            if (mCurrentIndex > 0)
            {
                mVertexBuffer.SetData(mVertexBufferData, 0, mCurrentIndex);
                int triangleCount = mCurrentIndex - 2;
                mDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, triangleCount);
                mCurrentIndex = 0;
            }
        }

        public void PushPolygon(List<Vector2> vertexList, Color color)
        {
            Debug.Assert(vertexList.Count >= 3);

            PushVertexData(vertexList[0], color);
            foreach(Vector2 vertex in vertexList)
            {
                PushVertexData(vertex, color);
            }
            PushVertexData(vertexList[vertexList.Count - 1], color);
        }

        private void PushVertexData(Vector2 vertex, Color color)
        {
            mVertexBufferData[mCurrentIndex].Position = new Vector3(vertex, 0f);
            mVertexBufferData[mCurrentIndex].Color = color;
            ++mCurrentIndex;
        }

        private void SetupVertexBuffer()
        {
            const int kVertexCount = 250 * 6;
            mVertexBuffer = new VertexBuffer(mDevice, typeof(VertexPositionColor), kVertexCount, BufferUsage.WriteOnly);
            mDevice.SetVertexBuffer(mVertexBuffer);
            mVertexBufferData = new VertexPositionColor[kVertexCount];
            mCurrentIndex = 0;
        }

        public void Setup(GraphicsDevice device, float arenaWidth, float arenaHeight)
        {
            mDevice = device;
            SetupVertexBuffer();
            SetupCamera(device, arenaWidth, arenaHeight);
        }

        private void SetupCamera(GraphicsDevice device, float arenaWidth, float arenaHeight)
        {
            float aspectRatio = device.Adapter.CurrentDisplayMode.AspectRatio;
            mBasicEffect = new BasicEffect(device);
            mBasicEffect.VertexColorEnabled = true;
            mBasicEffect.World = Matrix.CreateTranslation(-arenaWidth * 0.5f, -arenaHeight * 0.5f, 0f);
            mBasicEffect.View = Matrix.CreateOrthographic(aspectRatio * arenaHeight, arenaHeight, -100, 100);
            mBasicEffect.CurrentTechnique.Passes[0].Apply();
        }
    }
}
