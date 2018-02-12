using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Util.Models;

namespace TableFiller.Util
{
    public abstract class Generators
    {
        public static class NameGen
        {
            //This sub-class can be called gor the name generator...

                //Random Num generator
            private static Random random = new Random(DateTime.Now.Millisecond);
           



            //Store a map of all used names
            private static Dictionary<string, string> _usedNames = new Dictionary<string, string>();

            private static List<string> _firstNames = new List<string>()
            {
                "Tom","Bill","Sarah","Sandy","Ash","Ashley","Sid","Chuck",
                "Martha","Ryan","Stewert","Jason","Charlie","Vicky","Tim",
                "Timothy","Nicole","Levi","Tony","Zach","Roger","Haley","Sam",
                "Rob","Randi", "Chris","Faith","Andy","Stacy","Bailey","York",
                "Grayson","Alice", "Monika", "Chance","Jack","Jimmy","Jim","Billy",
                "Bob","Bobby", "Jeff", "George", "Robert", "Thomas" , "Kevin", "Will",
                "Jill","Max", "Jordan"
            };

            private static List<string> _lastNames = new List<string>()
            {
                "Jackson","Harris","Smith","Lankerton","Anderson","Reynolds",
                "Joachim","Phillips","Jordanson","Allen","O'Riley","Brady",
                "Lofton","Cheng","Rose","Davidson","Owen","Reed","Thomas",
                "Later", "Magnant","Laster","Rhodes","Washington", "Jefferson",
                "Helms","Hart", "Williams" , "Evans","Brown","Brees","Bush","Sanders",
                "Savage","Stevens","Duff",
                
            };

            //Limits
            private static readonly long CALC_CONSTANT = 2 ^(_firstNames.Count + _lastNames.Count);

            

            public static Name Gen(bool allowDuplicates, bool firstNameOnly)
            {
                string fName, lName;

                bool isUnique = true;

                long dupCounter = 0;
                do
                {
                    //Generate first Name
                    fName = _firstNames[random.Next(0, _firstNames.Count)];
                    //Generate last Name
                    lName = _lastNames[random.Next(0, _lastNames.Count)];
                    //Enforce Uniqueness.
                    isUnique = !_usedNames.ContainsKey(fName + lName);
                    if(!isUnique)
                        dupCounter++;
                    if (dupCounter >= (CALC_CONSTANT^2))
                        
                    {
                       throw new Exception("Out of combinations...");

                    }
                } while (!isUnique);

                //Add to used names
                _usedNames.Add(fName+lName,"");
                //Return new Name Object => See class for more info...
                return new Name(fName,lName);

                    

            }
        }
    }
}
