using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BladeRunnerHelper
{
    public class ModelResources
    {
        public class Palette
        {
            public class Color
            {
                public int r;
                public int g;
                public int b;
                public int rgb555;
            }

            public Color[] colors;
        }

        public class Animation
        {
            public int frameCount;
            public int frameSize;
            public float fps;
            public float x;
            public float y;
            public float z;
            public float angle;
            public int offset;
        }

        public class PageId
        {
            public int inMemoryId;
            public int cachedFrameId;
            public int coreId;
            public int frameId;
        }



        public int timeStamp;
        public int pageSize;
        public int pageCount;
        public int paletteCount;
        public Palette[] palettes;
        public int animationsCount;
        public Animation[] animations;
        public PageId[] pageIds;

        public BinaryReader coreReader;
        public int coreCount;
        public long coreDataPosition;

        public BinaryReader frameReader;
        public int frameCount;
        public long frameDataPosition;

        public int maxPagesInMemory;
        public byte[] inMemoryData;
        public int[] inMemoryIndexes;

        public Counter counter;


        public ModelResources()
        {
            counter = new Counter();
        }

        public void loadIndex()
        {
            using (var reader = new BinaryReader(File.OpenRead(@"STARTUP_MIX_index.dat")))
            {
                timeStamp = reader.ReadInt32();
                pageSize = reader.ReadInt32();
                pageCount = reader.ReadInt32();
                paletteCount = reader.ReadInt32();
                palettes = new Palette[paletteCount];
                for (var i = 0; i < paletteCount; i++)
                {
                    palettes[i] = new Palette();
                    palettes[i].colors = new Palette.Color[256];
                    for (var j = 0; j < 256; j++)
                    {
                        palettes[i].colors[j] = new Palette.Color();
                        palettes[i].colors[j].r = reader.ReadByte();
                        palettes[i].colors[j].g = reader.ReadByte();
                        palettes[i].colors[j].b = reader.ReadByte();
                        palettes[i].colors[j].rgb555 = palettes[i].colors[j].r << 10 + palettes[i].colors[j].g <<
                                                       5 + palettes[i].colors[j].b;
                    }
                }
                animationsCount = reader.ReadInt32();
                animations = new Animation[animationsCount];
                for (var i = 0; i < animationsCount; i++)
                {
                    animations[i] = new Animation();
                    animations[i].frameCount = reader.ReadInt32();
                    animations[i].frameSize = reader.ReadInt32();
                    animations[i].fps = reader.ReadSingle();
                    animations[i].x = reader.ReadSingle();
                    animations[i].y = reader.ReadSingle();
                    animations[i].z = reader.ReadSingle();
                    animations[i].angle = reader.ReadSingle();
                    animations[i].offset = reader.ReadInt32();
                }
                pageIds = new PageId[pageCount];
                for (var i = 0; i < pageCount; i++)
                {
                    pageIds[i] = new PageId();
                    pageIds[i].inMemoryId = -1;
                    pageIds[i].cachedFrameId = -1;
                    pageIds[i].coreId = -1;
                    pageIds[i].frameId = -1;
                }
            }
        }

        public void loadCore()
        {
            if (coreReader != null)
                coreReader.Close();

            coreReader = new BinaryReader(File.OpenRead(@"COREANIM.DAT"));

            var timeStamp = coreReader.ReadInt32();
            for (int i = 0; i < pageCount; i++)
            {
                pageIds[i].coreId = -1;
            }

            coreCount = coreReader.ReadInt32();
            for (var i = 0; i < coreCount; i++)
            {
                var pageIndex = coreReader.ReadInt32();
                if (pageIndex < pageCount)
                    pageIds[pageIndex].coreId = i;
            }

            coreDataPosition = coreReader.BaseStream.Position;
        }

        public void loadFrame()
        {
            if (frameReader != null)
                frameReader.Close();

            frameReader = new BinaryReader(File.OpenRead(@"HDFRAMES.DAT"));

            var timeStamp = frameReader.ReadInt32();
            for (int i = 0; i < pageCount; i++)
            {
                pageIds[i].frameId = -1;
            }

            frameCount = frameReader.ReadInt32();
            for (var i = 0; i < frameCount; i++)
            {
                var pageIndex = frameReader.ReadInt32();

                if ((pageIndex < pageCount) && (pageIndex > 0))
                    pageIds[pageIndex].frameId = i;
            }
            frameDataPosition = frameReader.BaseStream.Position;
        }

        public class Counter
        {
            public int count;
            public int[] list1;
            public int[] list2;

            public void setCount(int newCount)
            {
                count = newCount;
                list2 = new int[count + 1];
                list1 = new int[count + 1];

                if (count != -1)
                {
                    for (var i = 0; i <= count; i++)
                    {
                        list2[i] = (i + count) % (count + 1);
                    }


                    for (var i = 0; i <= count; i++)
                    {
                        list1[i] = (i + 1) % (count + 1);
                    }
                }
            }

            public void update(int value)
            {
                if (value < count)
                {
                    list1[list2[value]] = list1[value];
                    list2[list1[value]] = list2[value];
                    list1[value] = list1[count];
                    list2[value] = count;
                    list2[list1[count]] = value;
                    list1[count] = value;
                }
            }

        }

        public byte[] getData(int animationId, int frame)
        {
            if (animationId >= animationsCount)
                return null;
            if (frame >= animations[animationId].frameCount)
                return null;

            var frameOffset = animations[animationId].offset + frame * animations[animationId].frameSize;
            var pageIndex = frameOffset / pageSize;
            var pageOffset = frameOffset % pageSize;
            var inMemoryId = pageIds[pageIndex].inMemoryId;

            var position = 0;

            if (inMemoryId > 0)
            {
                position = pageSize * inMemoryId;
                counter.update(inMemoryId);
                return inMemoryData.Skip(position + pageOffset).Take(animations[animationId].frameSize).ToArray();
            }


            var coreId = pageIds[pageIndex].coreId;
            inMemoryId = counter.list2[counter.count];
            position = pageSize * inMemoryId;

            if (coreId != -1)
            {
                coreReader.BaseStream.Seek(coreDataPosition + coreId * pageSize, SeekOrigin.Begin);
                var data = coreReader.ReadBytes(pageSize);
                data.CopyTo(inMemoryData, position);
                if (inMemoryIndexes[inMemoryId] != -1)
                    pageIds[inMemoryIndexes[inMemoryId]].inMemoryId = -1;
                pageIds[pageIndex].inMemoryId = inMemoryId;
                inMemoryIndexes[inMemoryId] = pageIndex;

                position = pageSize * inMemoryId;
                counter.update(inMemoryId);
                return inMemoryData.Skip(position + pageOffset).Take(animations[animationId].frameSize).ToArray();
            }

            var cachedFrameId = pageIds[pageIndex].cachedFrameId;

            if (cachedFrameId != -1)
            {
                // var pos2 = cachedFramesDataPosition + cachedFrameAnimId * pageSize
                //TODO
                return null;
            }

            var frameId = pageIds[pageIndex].frameId;
            if (frameId != -1)
            {
                frameReader.BaseStream.Seek(frameDataPosition + frameId * pageSize, SeekOrigin.Begin);
                var data = frameReader.ReadBytes(pageSize);
                data.CopyTo(inMemoryData, position);
                if (inMemoryIndexes[inMemoryId] != -1)
                    pageIds[inMemoryIndexes[inMemoryId]].inMemoryId = -1;
                pageIds[pageIndex].inMemoryId = inMemoryId;
                inMemoryIndexes[inMemoryId] = pageIndex;

                // update cached frames

                position = pageSize * inMemoryId;
                counter.update(inMemoryId);
                return inMemoryData.Skip(position + pageOffset).Take(animations[animationId].frameSize).ToArray();

            }

            return null;

        }

        public void setMaxMemory(int maxMemory)
        {
            maxPagesInMemory = maxMemory / pageSize;
            inMemoryData = new byte[maxPagesInMemory * pageSize];
            inMemoryIndexes = new int[maxPagesInMemory];

            for (var i = 0; i < maxPagesInMemory; i++)
                inMemoryIndexes[i] = -1;

            counter.setCount(maxPagesInMemory);

            for (var i = 0; i < pageCount; i++)
                pageIds[i].inMemoryId = -1;
        }
    }
}
