//##################################################	
//	Owner: 		gvasilchenko
//	Edited: 	10/5/2016 10:09:57 AM 
//##################################################

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
    ///<summary>
    ///
    ///</summary>

    public static class FileHandler
    {
        public static Boolean CheckFilePresent(String path)
		{
			if (String.IsNullOrEmpty(path))
				return false;

			if (!Directory.Exists(path))
				return false;

            return new FileInfo(path).Exists;
        }
		public static Boolean FileExists(String filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				return false;

			return File.Exists(filePath) || File.Exists
			(
				Path.Combine
				(
					Directory.GetParent(Path.GetDirectoryName(filePath)).FullName,
					Path.GetFileName(filePath)
				)
			);
		}
		public static void LoadData<T>(String path, out T objectToLoadInto)
        {
            using (Stream stream = File.Open(path, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                objectToLoadInto = (T)binaryFormatter.Deserialize(stream);
            }
        }
        public static void SaveData<T>(String path, T objectToWrite)
        {
            using (Stream stream = File.Open(path, false ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }
        public static String GetDirectory()
        {
            return new DirectoryInfo(Assembly.GetCallingAssembly().Location).Parent.FullName;
        }
        public static String CombineDirectoryAndFilename(String fileName)
        {
            return Path.Combine(GetDirectory(), fileName);
        }
        public static String ReadASCIITextFile(String path)
        {
            try
            {
                using (FileStream stream = File.Open(path, FileMode.Open))
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);

                    return Encoding.ASCII.GetString(bytes);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to read file", e);
            }
        }
		public static Boolean IsAllowedToWrite(String folderPath)
		{
			FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Write, folderPath);
			PermissionSet permissionSet = new PermissionSet(PermissionState.None);
			permissionSet.AddPermission(permission);

			return permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
		}
    }
}
