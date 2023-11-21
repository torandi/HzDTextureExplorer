using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Pfim;

namespace HzdTextureExplorer
{
    public class HzDCore
    {
        private List<Texture> m_textures = new List<Texture>();
        private List<TextureSet> m_textureSet = new List<TextureSet>();
        private List<UITexture> m_uiTextures = new List<UITexture>();
        private List<Misc> m_misc = new List<Misc>();

        private string m_file;

        public String Path
        {
            get
            {
                return m_file;
            }
        }

        public List<Texture> Textures
        {
            get
            {
                return m_textures;
            }
        }
        public List<UITexture> UITextures
        {
            get
            {
                return m_uiTextures;
            }
        }


        public HzDCore(string path)
        {
            FileStream core = File.OpenRead(path);
            m_file = path;

            ReadCore(core);
        }

        private void ReadCore(FileStream core)
        {
            int headerSize = 12;

            BinaryReader reader = new BinaryReader(core);

            while((core.Position + headerSize) < core.Length)
            {
                UInt64 typeHash = reader.ReadUInt64();

                switch(typeHash)
                {
                    case TextureSet.TypeHash:
                        m_textureSet.Add(TextureSet.Read(this, core, reader));
                        break;
                    case Texture.TypeHash:
                        m_textures.Add(Texture.Read(this, core, reader));
                        break;
                    case UITexture.TypeHash:
                        m_uiTextures.Add(UITexture.Read(this, core, reader));
                        break;
                    default:
                        m_misc.Add(Misc.Read(this, core, reader));
                        break;
                }
            }
        }

        private byte[] ReadStreamData(UInt64 offset, UInt64 size)
        {
            FileStream file = File.OpenRead(m_file + ".stream");
            BinaryReader reader = new BinaryReader(file);
            file.Position = (long)offset;
            byte[] bytes = reader.ReadBytes((int)size);
            file.Close();
            return bytes;
        }
        private void WriteStreamData(UInt64 offset, byte[] data)
        {
            FileStream file = File.OpenWrite(m_file + ".stream");
            BinaryWriter writer = new BinaryWriter(file);
            file.Position = (long)offset;
            writer.Write(data);
            file.Close();
        }

        public void WriteCoreData(UInt64 offset, byte[] data)
        {
            FileStream file = File.OpenWrite(m_file);
            BinaryWriter writer = new BinaryWriter(file);
            file.Position = (long)offset;
            writer.Write(data);
            file.Close();
        }

        public void ReadImage(ImageData image, BinaryWriter writer, bool allowFail = false)
        {
            HzDException exception = null;
            try
            {
                Helper.WriteDdsHeader(writer, image.Width, image.Height, image.MipMaps, image.Slices, image.Format);
            }
            catch(HzDException ex)
            {
                if (allowFail)
                {
                    exception = new HzDException($"{ex.Message}: File exported as raw instead.");
                }
                else
                {
                    throw;
                }
            }
            if (image.HasStreamableData)
            {
                writer.Write(ReadStreamData(image.StreamStart, image.StreamLength));
            }
            if (image.HasEmbeddedData)
            {
                writer.Write(image.EmbeddedData);
            }

            writer.Flush();
            if (exception != null)
                throw exception;
        }

        public Stream OpenImage(ImageData image)
        {
            MemoryStream stream = new MemoryStream((int)(148 + image.StreamSize));
            BinaryWriter writer = new BinaryWriter(stream);

            ReadImage(image, writer);

            stream.Position = 0;

            return stream;
        }

        public void UpdateImage(ImageData image, byte[] newData)
        {
            if (image.StreamLength != (ulong)newData.Length)
                throw new HzDException($"New data is not the right size!");
            WriteStreamData(image.StreamStart, newData);
        }

    }

    public class BaseItem
    {
        protected UInt32 Size;
        protected Guid Id;

        protected long BasePosition;

        public HzDCore Core;

        protected BaseItem(HzDCore core)
        {
            Core = core;
        }

        protected virtual void ReadInternal(FileStream stream, BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            BasePosition = stream.Position;
            byte[] guid = reader.ReadBytes(16); // sizeof guid
            Id = new Guid(guid);
        }

        //  Skips this item
        protected void Skip(BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            reader.ReadBytes((int)Size);
        }

        protected long RemainingSize(FileStream stream)
        {
            return Size - (stream.Position - BasePosition);
        }
    }

    class Helper
    {
        public static String ReadString(BinaryReader reader)
        {
            UInt32 len = reader.ReadUInt32();
            if (len == 0)
                return "";

            reader.ReadUInt32(); // hash

            char[] chars = reader.ReadChars((int)len);
            return new string(chars);
        }

        public static void WriteDdsHeader(BinaryWriter writer, UInt32 width, UInt32 height, uint mipmapCount, uint slices, ImageFormat format)
        {
            const UInt32 DDSCAPS_COMPLEX = 0x8;
            const UInt32 DDSCAPS_MIPMAP = 0x400000;
            const UInt32 DDSCAPS_TEXTURE = 0x1000;
            const UInt32 dummy = 0;

            byte[] magic = new byte[]{ (byte)'D', (byte)'D', (byte)'S', (byte)' ' };
            writer.Write(magic);
            const UInt32 HeaderSize = 124;
            writer.Write(HeaderSize);
            Pfim.DdsFlags flags = Pfim.DdsFlags.Caps | Pfim.DdsFlags.Height | Pfim.DdsFlags.Width | Pfim.DdsFlags.PixelFormat | Pfim.DdsFlags.MipMapCount;
            writer.Write((UInt32)flags);
            writer.Write(height);
            writer.Write(width);
            writer.Write(dummy); // pitch
            writer.Write(dummy); // depth
            writer.Write((UInt32)mipmapCount);
            for (uint i = 0; i < 11; ++i)
                writer.Write(dummy); // reserved

            Pfim.DdsPixelFormat ddsFormat;


            // pixelformat:
            switch (format.Format)
            {
                case ImageFormat.Formats.BC1:
                case ImageFormat.Formats.BC3:
                case ImageFormat.Formats.BC4U:
                case ImageFormat.Formats.BC5U:
                case ImageFormat.Formats.BC6U:
                case ImageFormat.Formats.BC6S:
                case ImageFormat.Formats.BC7:
                case ImageFormat.Formats.RGBA_8888:
                    ddsFormat.Size = 32;
                    ddsFormat.PixelFormatFlags = Pfim.DdsPixelFormatFlags.Fourcc;
                    ddsFormat.FourCC = Pfim.CompressionAlgorithm.DX10;
                    ddsFormat.RGBBitCount = 0;
                    ddsFormat.RBitMask = 0;
                    ddsFormat.GBitMask = 0;
                    ddsFormat.BBitMask = 0;
                    ddsFormat.ABitMask = 0;
                    break;
                default:
                    throw new HzDException($"Only BC1, BC3, BC4U, BC5U, BC6, BC7 and RGBA_8888 supported right now. Tried to write {format.Format.ToString()}");
            }

            writer.Write(ddsFormat.Size);
            writer.Write((uint)ddsFormat.PixelFormatFlags);
            writer.Write((uint)ddsFormat.FourCC);
            writer.Write(ddsFormat.RGBBitCount);
            writer.Write(ddsFormat.RBitMask);
            writer.Write(ddsFormat.GBitMask);
            writer.Write(ddsFormat.BBitMask);
            writer.Write(ddsFormat.ABitMask);

            writer.Write(DDSCAPS_COMPLEX | DDSCAPS_TEXTURE | DDSCAPS_MIPMAP); // caps
            for (uint i = 0; i < 4; ++i)
                writer.Write(dummy); // caps2-4 and reserved2

            if(ddsFormat.FourCC == Pfim.CompressionAlgorithm.DX10)
            {

                switch (format.Format)
                {
                    case ImageFormat.Formats.BC1:
                        writer.Write((uint)Pfim.DxgiFormat.BC1_UNORM);
                        break;
                    case ImageFormat.Formats.BC3:
                        writer.Write((uint)Pfim.DxgiFormat.BC3_UNORM);
                        break;
                    case ImageFormat.Formats.BC4U:
                        writer.Write((uint)Pfim.DxgiFormat.BC4_UNORM);
                        break;
                    case ImageFormat.Formats.BC5U:
                        writer.Write((uint)Pfim.DxgiFormat.BC5_UNORM);
                        break;
                    case ImageFormat.Formats.BC6U:
                        writer.Write((uint)Pfim.DxgiFormat.BC6H_UF16);
                        break;
                    case ImageFormat.Formats.BC6S:
                        writer.Write((uint)Pfim.DxgiFormat.BC6H_SF16);
                        break;
                    case ImageFormat.Formats.BC7:
                        writer.Write((uint)Pfim.DxgiFormat.BC7_UNORM);
                        break;
                    case ImageFormat.Formats.RGBA_8888:
                        writer.Write((uint)Pfim.DxgiFormat.R8G8B8A8_UNORM);
                        break;
                }
                writer.Write((uint)Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE2D);
                writer.Write(dummy); // misc flag
                writer.Write((uint)slices>0?slices:1); // array size
                writer.Write((uint)8); // alpha mode
            }
        }

        public static void AddImageInfo(List<InfoItem> target, ImageData image)
        {
            target.Add(new InfoItem("Type", image.Type.ToString()));
            target.Add(new InfoItem("Width", image.Width.ToString()));
            target.Add(new InfoItem("Height", image.Height.ToString()));
            target.Add(new InfoItem("Format", image.Format.ToString()));
            target.Add(new InfoItem("Slices", image.Slices.ToString()));
            target.Add(new InfoItem("Mip Maps", image.MipMaps.ToString()));
            target.Add(new InfoItem("Stream MipMaps", image.StreamMipMaps.ToString()));
        }
    }

    public struct ImageType
    {
        public enum Types
        {
            Texture_2D = 0x0,
            Texture_3D = 0x1,
            Texture_CubeMap = 0x2,
            Texture_2DArray = 0x3,
        };
        public Types Type; // byte

        public ImageType(BinaryReader reader)
        {
            Type = (Types)reader.ReadUInt16();
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
    public struct ImageFormat
    {
        public enum Formats
        {
            INVALID = 0x4C,
            RGBA_5551 = 0x0,
            RGBA_5551_REV = 0x1,
            RGBA_4444 = 0x2,
            RGBA_4444_REV = 0x3,
            RGB_888_32 = 0x4,
            RGB_888_32_REV = 0x5,
            RGB_888 = 0x6,
            RGB_888_REV = 0x7,
            RGB_565 = 0x8,
            RGB_565_REV = 0x9,
            RGB_555 = 0xA,
            RGB_555_REV = 0xB,
            RGBA_8888 = 0xC,
            RGBA_8888_REV = 0xD,
            RGBE_REV = 0xE,
            RGBA_FLOAT_32 = 0xF,
            RGB_FLOAT_32 = 0x10,
            RG_FLOAT_32 = 0x11,
            R_FLOAT_32 = 0x12,
            RGBA_FLOAT_16 = 0x13,
            RGB_FLOAT_16 = 0x14,
            RG_FLOAT_16 = 0x15,
            R_FLOAT_16 = 0x16,
            RGBA_UNORM_32 = 0x17,
            RG_UNORM_32 = 0x18,
            R_UNORM_32 = 0x19,
            RGBA_UNORM_16 = 0x1A,
            RG_UNORM_16 = 0x1B,
            R_UNORM_16 = 0x1C, // Old: INTENSITY_16
            RGBA_UNORM_8 = 0x1D,
            RG_UNORM_8 = 0x1E,
            R_UNORM_8 = 0x1F, // Old: INTENSITY_8
            RGBA_NORM_32 = 0x20,
            RG_NORM_32 = 0x21,
            R_NORM_32 = 0x22,
            RGBA_NORM_16 = 0x23,
            RG_NORM_16 = 0x24,
            R_NORM_16 = 0x25,
            RGBA_NORM_8 = 0x26,
            RG_NORM_8 = 0x27,
            R_NORM_8 = 0x28,
            RGBA_UINT_32 = 0x29,
            RG_UINT_32 = 0x2A,
            R_UINT_32 = 0x2B,
            RGBA_UINT_16 = 0x2C,
            RG_UINT_16 = 0x2D,
            R_UINT_16 = 0x2E,
            RGBA_UINT_8 = 0x2F,
            RG_UINT_8 = 0x30,
            R_UINT_8 = 0x31,
            RGBA_INT_32 = 0x32,
            RG_INT_32 = 0x33,
            R_INT_32 = 0x34,
            RGBA_INT_16 = 0x35,
            RG_INT_16 = 0x36,
            R_INT_16 = 0x37,
            RGBA_INT_8 = 0x38,
            RG_INT_8 = 0x39,
            R_INT_8 = 0x3A,
            RGB_FLOAT_11_11_10 = 0x3B,
            RGBA_UNORM_10_10_10_2 = 0x3C,
            RGB_UNORM_11_11_10 = 0x3D,
            DEPTH_FLOAT_32_STENCIL_8 = 0x3E,
            DEPTH_FLOAT_32_STENCIL_0 = 0x3F,
            DEPTH_24_STENCIL_8 = 0x40,
            DEPTH_16_STENCIL_0 = 0x41,
            BC1 = 0x42, // Old: S3TC1
            BC2 = 0x43, // Old: S3TC3
            BC3 = 0x44, // Old: S3TC5
            BC4U = 0x45,
            BC4S = 0x46,
            BC5U = 0x47,
            BC5S = 0x48,
            BC6U = 0x49,
            BC6S = 0x4A,
            BC7 = 0x4B
        };
        public Formats Format; // byte
        byte Unknown2;
        byte Unknown3;

        public ImageFormat(BinaryReader reader)
        {
            Format = (Formats)reader.ReadByte();
            Unknown2 = reader.ReadByte();
            Unknown3 = reader.ReadByte();
        }

        public override string ToString()
        {
            return Format.ToString();
        }
    }

    public struct ImageSize
    {
        public uint Width;
        public uint Height;

        // Populate when read as 14 bits
        public uint WidthCrop;
        public uint HeightCrop;

        public static ImageSize ReadUint(BinaryReader reader)
        {
            ImageSize s = new ImageSize();
            s.Width = reader.ReadUInt32();
            s.Height = reader.ReadUInt32();
            s.WidthCrop = 0;
            s.HeightCrop = 0;
            return s;
        }

        public static ImageSize Read14bits(BinaryReader reader)
        {
            ImageSize s = new ImageSize();
            ushort raw = reader.ReadUInt16();
            s.Width = (ushort)(raw & 0x3fff); // 14 bits
            s.WidthCrop = (byte)((raw >> 14) & 0x3); // 2 bits
            raw = reader.ReadUInt16();
            s.Height = (ushort)(raw & 0x3fff); // 14 bits
            s.HeightCrop = (byte)((raw >> 14) & 0x3); // 2 bits

            return s;
        }
    }
    
    public class TextureSet : BaseItem
    {
        public const UInt64 TypeHash = 0xE02735CED4F1CDF;

        public TextureSet(HzDCore core)
            : base(core)
        { }

        public static TextureSet Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            TextureSet result = new TextureSet(core);
            result.ReadInternal(stream, reader);
            return result;
        }

        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.Skip(reader);
        }
    }

    class Misc : BaseItem
    {
        public static Misc Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            Misc result = new Misc(core);
            result.ReadInternal(stream, reader);
            return result;
        }
        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.Skip(reader);
        }

        public Misc(HzDCore core)
            : base(core)
        { }

    }
}
