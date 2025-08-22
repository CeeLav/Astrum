using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AstrumTool.Proto2CS
{
    internal class OpcodeInfo
    {
        public string Name = string.Empty;
        public int Opcode;
    }

    public static class Proto2CS
    {
        public static void Export()
        {
            InnerProto2CS.Proto2CS();
            Console.WriteLine("proto2cs succeed!");
        }
    }

    public static partial class InnerProto2CS
    {
        private const string protoDir = "../../AstrumConfig/Proto";
        private const string clientMessagePath = "../../AstrumProj/Assets/Script/Generated/Message/";
        private const string serverMessagePath = "../../AstrumServer/AstrumServer/Generated/";
        private const string clientServerMessagePath = "../../AstrumProj/Assets/Script/Generated/Message/";
        private static readonly char[] splitChars = [' ', '\t'];
        private static readonly List<OpcodeInfo> msgOpcode = [];

        public static void Proto2CS()
        {
            msgOpcode.Clear();

            Console.WriteLine($"Proto目录: {Path.GetFullPath(protoDir)}");
            Console.WriteLine($"客户端消息路径: {Path.GetFullPath(clientMessagePath)}");
            Console.WriteLine($"服务端消息路径: {Path.GetFullPath(serverMessagePath)}");
            Console.WriteLine($"客户端服务端消息路径: {Path.GetFullPath(clientServerMessagePath)}");

            RemoveAllFilesExceptMeta(clientMessagePath);
            RemoveAllFilesExceptMeta(serverMessagePath);
            RemoveAllFilesExceptMeta(clientServerMessagePath);

            List<string> list = FileHelper.GetAllFiles(protoDir, "*.proto");
            Console.WriteLine($"找到 {list.Count} 个proto文件");
            
            foreach (string s in list)
            {
                Console.WriteLine($"处理文件: {s}");
                if (!s.EndsWith(".proto"))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(s);
                string[] ss2 = fileName.Split('_');
                if (ss2.Length < 3)
                {
                    Console.WriteLine($"文件名格式错误: {fileName}，需要格式: name_C_1.proto");
                    continue;
                }
                
                string protoName = ss2[0];
                string cs = ss2[1];
                int startOpcode = int.Parse(ss2[2]);
                Console.WriteLine($"解析: protoName={protoName}, cs={cs}, startOpcode={startOpcode}");
                ProtoFile2CS(fileName, protoName, cs, startOpcode);
            }

            RemoveUnusedMetaFiles(clientMessagePath);
            RemoveUnusedMetaFiles(serverMessagePath);
            RemoveUnusedMetaFiles(clientServerMessagePath);
        }

        private static void ProtoFile2CS(string fileName, string protoName, string cs, int startOpcode)
        {
            msgOpcode.Clear();

            string proto = Path.Combine(protoDir, $"{fileName}.proto");
            string s = File.ReadAllText(proto);

            StringBuilder sb = new();
            sb.Append("using MemoryPack;\n");
            sb.Append("using System.Collections.Generic;\n");
            sb.Append("using Astrum.CommonBase;\n\n");
            sb.Append($"namespace Astrum.Generated\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            string msgName = "";
            string responseType = "";
            StringBuilder sbDispose = new();
            Regex responseTypeRegex = ResponseTypeRegex();
            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();
                if (string.IsNullOrEmpty(newline))
                {
                    continue;
                }

                if (responseTypeRegex.IsMatch(newline))
                {
                    responseType = responseTypeRegex.Replace(newline, string.Empty);
                    responseType = responseType.Trim().Split(':')[1].Trim().Split(' ')[0].TrimEnd('\r', '\n');
                    continue;
                }

                if (!isMsgStart && newline.StartsWith("//"))
                {
                    if (newline.StartsWith("///"))
                    {
                        sb.Append("\t/// <summary>\n");
                        sb.Append($"\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t/// </summary>\n");
                    }
                    else
                    {
                        sb.Append($"\t// {newline.TrimStart('/', ' ')}\n");
                    }

                    continue;
                }

                if (newline.StartsWith("message"))
                {
                    isMsgStart = true;

                    string parentClass = "";
                    msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(["//"], StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }

                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = ++startOpcode });

                    sb.Append($"\t[MemoryPackable]\n");
                    sb.Append($"\t[MessageAttribute({startOpcode})]\n");
                    if (!string.IsNullOrEmpty(responseType))
                    {
                        sb.Append($"\t[ResponseType(nameof({responseType}))]\n");
                    }

                    sb.Append($"\tpublic partial class {msgName} : MessageObject");

                    if (parentClass != "")
                    {
                        sb.Append($", {parentClass}\n");
                    }
                    else
                    {
                        sb.Append('\n');
                    }

                    continue;
                }

                if (isMsgStart)
                {
                    if (newline.StartsWith('{'))
                    {
                        sbDispose.Clear();
                        sb.Append("\t{\n");
                        sb.Append($"\t\tpublic static {msgName} Create(bool isFromPool = false)\n\t\t{{\n\t\t\treturn ObjectPool.Instance.Fetch(typeof({msgName}), isFromPool) as {msgName};\n\t\t}}\n\n");
                        continue;
                    }

                    if (newline.StartsWith('}'))
                    {
                        isMsgStart = false;
                        responseType = "";

                        // 加了no dispose则自己去定义dispose函数，不要自动生成
                        if (!newline.Contains("// no dispose"))
                        {
                            sb.Append($"\t\tpublic override void Dispose()\n\t\t{{\n\t\t\tif (!this.IsFromPool)\n\t\t\t{{\n\t\t\t\treturn;\n\t\t\t}}\n\n\t\t\t{sbDispose.ToString().TrimEnd('\t')}\n\t\t\tObjectPool.Instance.Recycle(this);\n\t\t}}\n");
                        }

                        sb.Append("\t}\n\n");
                        continue;
                    }

                    if (newline.StartsWith("//"))
                    {
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t\t/// </summary>\n");
                        continue;
                    }

                    string memberStr;
                    if (newline.Contains("//"))
                    {
                        string[] lineSplit = newline.Split("//");
                        memberStr = lineSplit[0].Trim();
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {lineSplit[1].Trim()}\n");
                        sb.Append("\t\t/// </summary>\n");
                    }
                    else
                    {
                        memberStr = newline;
                    }

                    if (memberStr.StartsWith("map<"))
                    {
                        Map(sb, memberStr, sbDispose);
                    }
                    else if (memberStr.StartsWith("repeated"))
                    {
                        Repeated(sb, memberStr, sbDispose);
                    }
                    else
                    {
                        Members(sb, memberStr, sbDispose);
                    }
                }
            }

            sb.Append("\tpublic static class " + protoName + "\n\t{\n");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.Append($"\t\tpublic const ushort {info.Name} = {info.Opcode};\n");
            }

            sb.Append("\t}\n");

            sb.Append('}');

            sb.Replace("\t", "    ");
            string result = sb.ToString().ReplaceLineEndings("\r\n");

            // 生成到客户端目录
            GenerateCS(result, clientServerMessagePath, proto);
            // 生成到服务器端目录
            GenerateCS(result, serverMessagePath, proto);
        }

        private static void GenerateCS(string result, string path, string proto)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string csPath = Path.Combine(path, Path.GetFileNameWithoutExtension(proto) + ".cs");
            using FileStream txt = new(csPath, FileMode.Create, FileAccess.ReadWrite);
            using StreamWriter sw = new(txt);
            sw.Write(result);
        }

        private static void Map(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            int start = newline.IndexOf('<') + 1;
            int end = newline.IndexOf('>');
            string types = newline.Substring(start, end - start);
            string[] ss = types.Split(',');
            string keyType = ConvertType(ss[0].Trim());
            string valueType = ConvertType(ss[1].Trim());
            string tail = newline[(end + 1)..];
            ss = tail.Trim().Replace(";", "").Split(' ');
            string v = ss[0];
            int n = int.Parse(ss[2]);

            sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
            sb.Append($"\t\tpublic Dictionary<{keyType}, {valueType}> {v} {{ get; set; }} = new();\n");

            sbDispose.Append($"this.{v}.Clear();\n\t\t\t");
        }

        private static void Repeated(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[1];
                type = ConvertType(type);
                string name = ss[2];
                int n = int.Parse(ss[4]);

                sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
                sb.Append($"\t\tpublic List<{type}> {name} {{ get; set; }} = new();\n\n");

                sbDispose.Append($"this.{name}.Clear();\n\t\t\t");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        private static string ConvertType(string type)
        {
            return type switch
            {
                "int16" => "short",
                "int32" => "int",
                "bytes" => "byte[]",
                "uint32" => "uint",
                "long" => "long",
                "int64" => "long",
                "uint64" => "ulong",
                "uint16" => "ushort",
                _ => type
            };
        }

        private static void Members(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[0];
                string name = ss[1];
                int n = int.Parse(ss[3]);
                string typeCs = ConvertType(type);

                sb.Append($"\t\t[MemoryPackOrder({n - 1})]\n");
                sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}\n\n");

                switch (typeCs)
                {
                    case "bytes":
                    {
                        break;
                    }
                    default:
                        sbDispose.Append($"this.{name} = default;\n\t\t\t");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        /// <summary>
        /// 删除meta以外的所有文件
        /// </summary>
        static void RemoveAllFilesExceptMeta(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            DirectoryInfo targetDir = new(directory);
            FileInfo[] fileInfos = targetDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo info in fileInfos)
            {
                if (!info.Name.EndsWith(".meta"))
                {
                    File.Delete(info.FullName);
                }
            }
        }

        /// <summary>
        /// 删除多余的meta文件
        /// </summary>
        static void RemoveUnusedMetaFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            DirectoryInfo targetDir = new(directory);
            FileInfo[] fileInfos = targetDir.GetFiles("*.meta", SearchOption.AllDirectories);
            foreach (FileInfo info in fileInfos)
            {
                string pathWithoutMeta = info.FullName.Remove(info.FullName.LastIndexOf(".meta", StringComparison.Ordinal));
                if (!File.Exists(pathWithoutMeta) && !Directory.Exists(pathWithoutMeta))
                {
                    File.Delete(info.FullName);
                }
            }
        }

            [GeneratedRegex(@"//\s*ResponseType")]
    private static partial Regex ResponseTypeRegex();
}

// 文件操作辅助类
public static class FileHelper
{
    public static List<string> GetAllFiles(string dir, string searchPattern = "*")
    {
        List<string> list = new List<string>();
        GetAllFiles(list, dir, searchPattern);
        return list;
    }
    
    public static void GetAllFiles(List<string> files, string dir, string searchPattern = "*")
    {
        string[] fls = Directory.GetFiles(dir);
        foreach (string fl in fls)
        {
            if (searchPattern == "*" || Path.GetFileName(fl).EndsWith(searchPattern.Replace("*", "")))
            {
                files.Add(fl);
            }
        }

        string[] subDirs = Directory.GetDirectories(dir);
        foreach (string subDir in subDirs)
        {
            GetAllFiles(files, subDir, searchPattern);
        }
    }
    
    public static void CleanDirectory(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }
        foreach (string subdir in Directory.GetDirectories(dir))
        {
            Directory.Delete(subdir, true);		
        }

        foreach (string subFile in Directory.GetFiles(dir))
        {
            File.Delete(subFile);
        }
    }
    
    public static void CopyDirectory(string srcDir, string tgtDir)
    {
        DirectoryInfo source = new DirectoryInfo(srcDir);
        DirectoryInfo target = new DirectoryInfo(tgtDir);
	
        if (target.FullName.StartsWith(source.FullName, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new Exception("父目录不能拷贝到子目录！");
        }
	
        if (!source.Exists)
        {
            return;
        }
	
        if (!target.Exists)
        {
            target.Create();
        }
	
        FileInfo[] files = source.GetFiles();
	
        for (int i = 0; i < files.Length; i++)
        {
            File.Copy(files[i].FullName, Path.Combine(target.FullName, files[i].Name), true);
        }
	
        DirectoryInfo[] dirs = source.GetDirectories();
	
        for (int j = 0; j < dirs.Length; j++)
        {
            CopyDirectory(dirs[j].FullName, Path.Combine(target.FullName, dirs[j].Name));
        }
    }
    
    public static void ReplaceExtensionName(string srcDir, string extensionName, string newExtensionName)
    {
        if (Directory.Exists(srcDir))
        {
            string[] fls = Directory.GetFiles(srcDir);

            foreach (string fl in fls)
            {
                if (fl.EndsWith(extensionName))
                {
                    File.Move(fl, fl.Substring(0, fl.IndexOf(extensionName)) + newExtensionName);
                    File.Delete(fl);
                }
            }

            string[] subDirs = Directory.GetDirectories(srcDir);

            foreach (string subDir in subDirs)
            {
                ReplaceExtensionName(subDir, extensionName, newExtensionName);
            }
        }
    }
}
}