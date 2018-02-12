using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace TableFiller.Util.Models
{
    //This class is a special wrapper for names.
    //Imagine each name as a character, this class would be the string.
    public class Name
    {

        //The basis of our structure, the base size is 2 for both space and obvious reasons.
        private string[] names = new string[2];

        //Members
        public string First //Property Getty/Setter
        {
            //Example lambda expression
            get => names[0];
            set => names[0] = value;
        }

        public string Last
        {
            get => names[names.Length - 1];
            set => names[names.Length - 1] = value;
        }

        public string[] Additional
        {
            get
            {

                if (names.Length > 2)
                {

                    //Store names here, remember 2 is the constant that excludes The First and last names.
                    var additional = new string[names.Length - 2];
                    var indexInAdditional = 0;
                    //Start after first name and read until last name
                    for (int x = 1; x < names.Length; x++)
                    {
                        //Add
                        additional[indexInAdditional] = names[x];
                        //Push
                        indexInAdditional++;

                    }
                    return additional;
                }
                else
                {
                    return names;
                }
            }

            set
            {
                //Expand the array with this algo
                var oldNames = names;
                var newNames = new string[oldNames.Length + 1];
                //Set First
                newNames[0] = oldNames[0];
                //Set Last
                newNames[newNames.Length-1] = oldNames[oldNames.Length-1];

                //Set additional
                var indexInVals = 0; //Index to keep track of entry reading
                for (int x = 1; x < newNames.Length - 1; x++)
                {
                    newNames[x] = value[indexInVals];
                    indexInVals++;
                }
            }
        }

        public Name(params string[] name)
        {
            if (name.Length == 2)
            {
                //This..
                names[0] = name[0];
                names[1] = name[1];
            }
            else
            {
                //Is equal to this.
                First = name[0];
                Last = name[name.Length-1]; //note that the actual last would be quite longer, names[names.length-1] = name [name.Length-1]. Properties rock...

                //Add additional
                var additionalNames = new string[names.Length - 2];
                var indexInName = 1;
                for (int x=0; x < name.Length - 1; x++)
                {
                    additionalNames[x] = name[indexInName];
                    indexInName++;
                }
            }
        }

        
        public string Get()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string name in names)
            {
                sb.Append(name+" ");
            }
            return sb.ToString();
        }

        public int Size => names.Length; //Get Size

        public override string ToString()

        {
            return Get();
        }
    }
}
