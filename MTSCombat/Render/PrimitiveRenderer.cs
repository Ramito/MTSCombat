using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ranitas.Core.Render
{
    public sealed class PrimitiveRenderer
    {
        public GraphicsDevice Device { get; private set; }
        private VertexBuffer mVertexBuffer;
        private VertexPositionColor[] mVertexBufferData;
        private int mCurrentIndex = -1;

        public void Setup(GraphicsDevice device)
        {
            //TODO: Where should the basic effect live??
            Device = device;
            SetupVertexBuffer();
        }

        public void Render()
        {
            if (mCurrentIndex > 0)
            {
                mVertexBuffer.SetData(mVertexBufferData, 0, mCurrentIndex);
                int triangleCount = mCurrentIndex - 2;
                Device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, triangleCount);
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
            mVertexBuffer = new VertexBuffer(Device, typeof(VertexPositionColor), kVertexCount, BufferUsage.WriteOnly);
            Device.SetVertexBuffer(mVertexBuffer);
            mVertexBufferData = new VertexPositionColor[kVertexCount];
            mCurrentIndex = 0;
        }
    }
}
