using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TableFiller.Util.Data_Structures
{

    public class BloomFilter
    {

        //Our bit array
        private static BitArray bitArray = new BitArray(10000000); //Create 1 Billion Bits


        //Max amount of items
        private static long limit = 2 ^ 32;
        //Total amount of hashes given
        private static long totalHashes;

        //The name of this fitler instance


        //Trigger this to save the filter after each run
        private static bool doAutoSave = true; //Default is true
        //Where to save
        private  DirectoryInfo saveDir = Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\.filters\\");

        private  FileStream saveFile;
        private string name;




        

        //Our constructor
        /// <summary>
        /// Creates a Bloom Filter.
        /// </summary>
        /// <param name="name">Specify Name, If a filter is found matching the name it will be loaded.</param>
        public BloomFilter(string name)
        {
            //Attach name
            this.name = name;

            saveFile = new FileStream((saveDir.FullName + name + ".blm"), FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite);

            if (saveFile.Length > 10)
            {
                var ms = new MemoryStream(File.ReadAllBytes(saveFile.Name));


                //It was a file before
                var save = (Save) new BinaryFormatter().Deserialize(ms);

                bitArray = save.Data;
                limit = save.HashLimit;
                totalHashes = save.UsedHashes;

                if (name != save.Name)
                {
                    throw new IOException(
                        "File was found that matched the name, however it did not pass name verification!");
                }


                Save(); //Initial Save

            }

            Save();
            


        }

        public bool IsAtMax()
        {
            return totalHashes == limit;
        }
        public long TotalHashesGiven() => totalHashes;

        /// <summary>
        /// This wipes the filter. Be Careful!
        /// For Accident Prevention, this will not save. 
        /// Call an AddItem to finalize this state on disk.
        /// </summary>
        public void Clear()
        {
            bitArray.SetAll(false);
            totalHashes = 0;
        }

        public  void AddItem(Object o)
        {

            int hash = o.GetHashCode();
            var k = 20; //hash it 20 times , We've already hashed it once.
            for (int i = 0; i < k - 1; i++)
            {
                //Only good for 2^32 hashes
                hash = hash.GetHashCode();
            }
            //Get rid of negatives...
            hash = Math.Abs(hash);
            //Add to bit array
            bitArray[hash % bitArray.Length] = true;
            totalHashes++;

            if (doAutoSave)
            {
                Save();
            }
        }

        /// <summary>
        /// Checks filter for existence
        /// </summary>
        /// <param name="o">The object you want to check for.</param>
        /// <returns></returns>
        public  bool CheckFor(Object o)
        {
            int hash = o.GetHashCode();
            var k = 20; //hash it 20 times , We've already hashed it once.
            for (int i = 0; i < k - 1; i++)
            {
                hash = hash.GetHashCode();
            }

            hash = Math.Abs(hash);
            //check from bit array
            return bitArray[hash % bitArray.Length];


        }

        /// <summary>
        /// Saves Filter to Disk.
        /// </summary>
        public void Save()
        {
            //Create save object
            Save save = new Save();
            save.Data = bitArray;
            save.Name = this.name;
            save.HashLimit = limit;
            save.UsedHashes = totalHashes;

            //Alert
            Logger.LogG("Saving to "+saveFile.Name);
            //Save to disk
            var ms = new MemoryStream();
            var writer = new BinaryFormatter();
            writer.Serialize(ms, save);
            saveFile.Write(ms.ToArray(), 0, ms.ToArray().Length);
            


        }
        /// <summary>
        /// Call this at the end of your program, if needed
        /// Saves and Then Closes all Streams In Use By The Filter.
        /// </summary>
        public void Close()
        {
            Save();
            saveFile.Close();
        }

    }

    /// <summary>
    /// Stores BloomFilter Saves
    /// </summary>
    [Serializable]
    internal struct Save
    {
        public string Name;
        public long UsedHashes;
        public long HashLimit;
        public BitArray Data;

    }
}
