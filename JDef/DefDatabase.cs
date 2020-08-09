using System.IO;

namespace JDef
{
    /// <summary>
    /// A definition database stores all loaded definitions.
    /// Definitions loaded into the database can automatically reference each other.
    /// </summary>
    public class DefDatabase
    {
        private DefLoader loader;

        public DefDatabase()
        {
            loader = new DefLoader();
        }

        public void LoadFromDir(string dir)
        {
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException($"Cannot load, dir not found: {dir}");

            string[] files = Directory.GetFiles(dir, "*.xml");
            string[] data = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                data[i] = File.ReadAllText(path);
            }
            loader.Load(data);
        }
    }
}
