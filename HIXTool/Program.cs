using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HIXTool
{
    class Program
    {
        struct ImageData
        {
            public ImageData(byte[] hash, byte unk, uint offset, uint length)
            {
                Hash = hash;
                Unk = unk;
                Offset = offset;
                Length = length;
            }
            public byte[] Hash;
            public byte Unk;
            public uint Offset;
            public uint Length;
        }

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                if (args[0] == "-x")
                {
                    string hixPath = args[1];
                    string harPath = Path.GetDirectoryName(hixPath) + "\\" + Path.GetFileNameWithoutExtension(hixPath) + ".har";
                    string outDir = Path.GetDirectoryName(hixPath) + "\\" + Path.GetFileNameWithoutExtension(hixPath);
                    if (File.Exists(hixPath) && File.Exists(harPath))
                    {
                        List<ImageData> data = new List<ImageData>();

                        using (BinaryReader reader = new BinaryReader(new FileStream(hixPath, FileMode.Open, FileAccess.Read)))
                        {
                            Console.WriteLine("Reading HIX file...");

                            if (!reader.ReadBytes(4).SequenceEqual(new byte[] { 0x48, 0x49, 0x44, 0x58 }))
                            {
                                Console.WriteLine("Error: Not a valid HIX file!");
                                return;
                            }

                            uint imageCount = reader.ReadUInt32();

                            for (int i = 0; i < imageCount; i++)
                            {
                                data.Add(new ImageData(reader.ReadBytes(8), reader.ReadByte(), reader.ReadUInt32(), reader.ReadUInt32()));
                            }
                        }

                        using (BinaryReader reader = new BinaryReader(new FileStream(harPath, FileMode.Open, FileAccess.Read)))
                        {
                            Console.WriteLine("Extracting from HAR file...");

                            if (!reader.ReadBytes(4).SequenceEqual(new byte[] { 0x48, 0x41, 0x52, 0x43 }))
                            {
                                Console.WriteLine("Error: Not a valid HAR file!");
                                return;
                            }

                            uint imageCount = reader.ReadUInt32();

                            if (!Directory.Exists(outDir))
                                Directory.CreateDirectory(outDir);

                            for (int i = 0; i < imageCount; i++)
                            {
                                string hash = BytesToString(data[i].Hash, "X2");

                                Console.WriteLine($"Extracting image {hash}...");
                                Console.WriteLine($"Unk: {data[i].Unk.ToString("X2")}, Offset: 0x{data[i].Offset.ToString("X8")}, Length: {data[i].Length}");
                                reader.BaseStream.Seek(0xC + data[i].Offset, SeekOrigin.Begin);
                                File.WriteAllBytes(outDir + "\\" + hash + ".png", reader.ReadBytes((int)data[i].Length));
                            }
                        }

                        Console.WriteLine("Done.");
                    }
                    else
                    {
                        if (!File.Exists(hixPath))
                        {
                            Console.WriteLine("Error: HIX does not exist!");
                        }
                        else if (!File.Exists(harPath))
                        {
                            Console.WriteLine("Error: HAR does not exist!");
                        }
                    }
                }
                else if (args[0] == "-b")
                {
                    string dir = args[1];
                    if (Directory.Exists(dir))
                    {
                        string[] files = Directory.GetFiles(dir, "*.png");

                        List<ImageData> data = new List<ImageData>();

                        using (BinaryWriter writer = new BinaryWriter(new FileStream(dir + ".har", FileMode.OpenOrCreate, FileAccess.Write)))
                        {
                            Console.WriteLine("Building HAR...");

                            writer.Write(new byte[] { 0x48, 0x41, 0x52, 0x43 });
                            writer.Write(files.Length);
                            writer.Write((uint)0);

                            for (int i = 0; i < files.Length; i++)
                            {
                                byte[] hash;
                                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                                if (fileName.Length == 16)
                                {
                                    hash = StringToBytes(fileName);
                                }
                                else
                                    continue;

                                byte[] file = File.ReadAllBytes(files[i]);

                                data.Add(new ImageData(hash, 0, (uint)writer.BaseStream.Position - 0xC, (uint)file.Length));

                                writer.Write(file);
                            }

                            writer.BaseStream.Seek(0x8, SeekOrigin.Begin);
                            writer.Write((uint)writer.BaseStream.Length - 0xC);
                        }

                        using (BinaryWriter writer = new BinaryWriter(new FileStream(dir + ".hix", FileMode.OpenOrCreate, FileAccess.Write)))
                        {
                            Console.WriteLine("Building HIX...");

                            writer.Write(new byte[] { 0x48, 0x49, 0x44, 0x58 });
                            writer.Write(data.Count);

                            for (int i = 0; i < data.Count; i++)
                            {
                                writer.Write(data[i].Hash);
                                writer.Write(data[i].Unk);
                                writer.Write(data[i].Offset);
                                writer.Write(data[i].Length);
                            }
                        }

                        Console.WriteLine("Done.");
                    }
                }
                else
                {
                    PrintUsage();
                }
            }
            else
            {
                PrintUsage();
            }
        }

        static string BytesToString(byte[] bytes, string format)
        {
            string o = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                o += bytes[i].ToString(format);
            }

            return o;
        }

        static byte[] StringToBytes(string str)
        {
            List<byte> o = new List<byte>();

            for (int i = 0; i < str.Length; i += 2)
            {
                o.Add(byte.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }

            return o.ToArray();
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:\n  To Extract:\n    HIXTool.exe -x <hix file>\n  To Rebuild:\n    HIXTool.exe -b <directory>");
        }
    }
}
