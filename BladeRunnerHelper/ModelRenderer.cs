using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BladeRunnerHelper
{
    public class ModelRenderer : ModelResources
    {
        public delegate void vertexFunction(float x, float y, int r, int g, int b);
        public delegate void polygonBeginFunction(int i);
        public delegate void polygonEndFunction(int i);
        public delegate void sliceBeginFunction(int slice, float sliceHeight);
        public delegate void sliceEndFunction(int slice, float sliceHeight);

        public int animationId;
        public int frameId;
        public Vector3 position;
        public float angle;
        public float scale;
        public byte[] frameData;
        public BinaryReader frameReader;
        
        public float frameScaleX;
        public float frameScaleY;
        public float framePositionX;
        public float framePositionY;
        public float frameSliceHeight;
        public float frameBottomZ;
        public int framePaletteIndex;
        public int frameSliceCount;
        public vertexFunction vertex;
        public polygonBeginFunction polygonBegin;
        public polygonEndFunction polygonEnd;
        public sliceBeginFunction sliceBegin;
        public sliceEndFunction sliceEnd;


        public void setupFrame(int animation, int frame, Vector3 position, float facing, float scale)
        {
            this.animationId = animation;
            this.frameId = frame;
            this.position = position;
            this.angle = facing;
            this.scale = scale;

            frameData = this.getData(this.animationId, this.frameId);

            frameReader = new BinaryReader(new MemoryStream(frameData));

         
            frameScaleX = frameReader.ReadSingle();
            frameScaleY = frameReader.ReadSingle();
            frameSliceHeight = frameReader.ReadSingle();
            framePositionX = frameReader.ReadSingle();
            framePositionY = frameReader.ReadSingle();
            frameBottomZ = frameReader.ReadSingle();
            framePaletteIndex = frameReader.ReadInt32();
            frameSliceCount = frameReader.ReadInt32();
            

        }


        public void drawFrame()
        {
            for (var i = 0; i < frameSliceCount; i++)
            {
                sliceBegin?.Invoke(i, frameSliceHeight);
                drawSlice(i);
                sliceEnd?.Invoke(i, frameSliceHeight);
            }
        }

        public void drawSlice(int slice)
        {
            var palette = this.palettes[framePaletteIndex];
            var pos = frameReader.BaseStream.Position;
            frameReader.BaseStream.Seek(0x20 + 4 * slice, SeekOrigin.Begin);
            var offset = frameReader.ReadInt32();
            frameReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            var polyCount = frameReader.ReadInt32();

            for (var i = 0; i < polyCount; i++)
            {
                polygonBegin?.Invoke(i);
                var vertexCount = frameReader.ReadInt32();
                if (vertexCount == 0)
                    continue;

                for (var j = 0; j < vertexCount; j++)
                {
                    var x = frameReader.ReadByte();
                    var y = frameReader.ReadByte();


                    var color = frameReader.ReadByte();

                    var r = palette.colors[color].r;
                    var g = palette.colors[color].g;
                    var b = palette.colors[color].b;

                    var x1 = x * frameScaleX + framePositionX;
                    var y1 = y * frameScaleY + framePositionY;

                    vertex?.Invoke(x1, y1, r, g, b);

                }

                polygonEnd?.Invoke(i);
            }
        }
    }
}

