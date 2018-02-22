using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Util.Data_Structures;
using TableFiller.Util.Models;

namespace TableFiller.Util
{
    public abstract class Generators
    {
        /// <summary>
        /// Contains Methods for generating various names.
        /// </summary>
        public static class NameGen
        {
            //This sub-class can be called gor the name generator...

                //Random Num generator
            private static Random random = new Random(DateTime.Now.Millisecond);

           


            //I've considered merging them all to one filter, however I wanted to test my persistance names methods...
            //Use bloom Filter of all used names
            private static BloomFilter filter = new BloomFilter("usedNames");

            private static List<string> _firstNames = new List<string>()
            {
                "Tom","Bill","Sarah","Sandy","Ash","Ashley","Sid","Chuck",
                "Martha","Ryan","Stewert","Jason","Charlie","Vicky","Tim",
                "Timothy","Nicole","Levi","Tony","Zach","Roger","Haley","Sam",
                "Rob","Randi", "Chris","Faith","Andy","Stacy","Bailey","York",
                "Grayson","Alice", "Monika", "Chance","Jack","Jimmy","Jim","Billy",
                "Bob","Bobby", "Jeff", "George", "Robert", "Thomas" , "Kevin", "Will",
                "Jill","Max", "Jordan", "Sean","Shawn","Marcus", "Syd","Kimi","Peter"
            };

            private static List<string> _lastNames = new List<string>()
            {
                "Jackson","Harris","Smith","Lankerton","Anderson","Reynolds",
                "Joachim","Phillips","Jordanson","Allen","O'Riley","Brady",
                "Lofton","Cheng","Rose","Davidson","Owen","Reed","Thomas",
                "Later", "Magnant","Laster","Rhodes","Washington", "Jefferson",
                "Helms","Hart", "Williams" , "Evans","Brown","Brees","Bush","Sanders",
                "Savage","Stevens","Duff", "Halpert","Caroll","Scott","Harvey",
                "Pan","Louis","Howard", "Zhang","De"

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
                    isUnique = !filter.CheckFor(fName + lName);
                    if(!isUnique)
                        dupCounter++;
                    if (dupCounter >= (CALC_CONSTANT^2))
                        
                    {
                       throw new Exception("Out of combinations...");

                    }
                } while (!isUnique);

                //Add to used names
                filter.AddItem(fName+lName);
                //Return new Name Object => See class for more info...
                return new Name(fName,lName);

                    

            }

            public static void CloseFilter()
            {
                filter.Close();
            }
        }

        /// <summary>
        /// Contains Methods for generating various Addresses
        /// </summary>
        public static class AddressGen
        {

            //Filter

            private static BloomFilter usedAddr;

        


            private static List<string> RoadNames = new List<string>()
            {
                "Charles","College","River","Main","Lanier","Chandler","Wave","Waters","Park","Over","Under",
                "Green","Rhode","Washington","Junior","Martin","City","Tree","Langston","Tiger", "Turner", "Wish"
                ,"Cat","Dogston","Quick","Lunar","Tech","Mars", "Claire","Sadderton","Con", "Rucker","Boating","Thaser",
                "Railway","Rolls","Poisson","Ein","Shakesphere","Rose", "Westin","Bravo","Alpha","Charlie","Delta","Tango"
                ,"Plant","Flower", "Shedlowski", "Georgia","Dreamers","Carn"
            };

            private static List<string> Prefixes = new List<string>()
            {
                "Ave.",
                "Rd.",
                "Blvd.",
                "Ln.",
                "St.",
                "Hwy",
                "Dr."
            };

            private static List<string> CityAreas = new List<string>()
            {
                "Statesboro,Georgia,30458,USA",
                "Conyers,Georgia,30012,USA",
                "Augusta,Georgia,30805,USA",
                "Savannah,Georgia,31302,USA",
                "Marietta,Georgia,30006,USA",
                "Kennesaw,Georgia,30144,USA",
                "Athens,Georgia,30601,USA",
                "Decatur,Georgia,30030,USA"
                
            };

            //Our Random Instance
            private static Random random = new Random(DateTime.Now.Millisecond);

            //Generates a random zip
            private static CityArea GenRegion()
            {
                return new CityArea(CityAreas[random.Next(CityAreas.Count)]);
            }

            public static Address GenAddress()
            {

                if (usedAddr == null)
                {
                    usedAddr = new BloomFilter("Addresses");
                    Logger.LogG("Created Filter.");
                }
                var street = RoadNames[random.Next(RoadNames.Count)] + " " + Prefixes[random.Next(Prefixes.Count)];
                var address = new Address(street,GenRegion());

                if (usedAddr.CheckFor(address))
                {
                    //If it exists
                    return GenAddress(); // Gen a new addr
                }
                else
                    return address;
            }

            public static void CloseFilter()
            {
                usedAddr.Close();
            }
        }

        /// <summary>
        /// Containse methods for generating various IDs.
        /// </summary>
        public abstract class IDGen
        {
            //Our Hasher
            private static readonly SHA256Managed hasher = new SHA256Managed();

            //Bloom Filter for checking if ID Has been given already
            private static readonly BloomFilter filter = new BloomFilter("IDGen");

            /// <summary>
            /// Generates a DID, Given the dept name
            /// </summary>
            /// <param name="deptName"></param>
            /// <returns></returns>
            public static string DID(string deptName, string STRID)
            {
                var id = "Dpt" + (
                             Convert.ToBase64String(
                                 hasher.ComputeHash(
                                     Encoding.ASCII.GetBytes(deptName + STRID))).Substring(0, 20) +
                             Convert.ToBase64String(
                                 Encoding.ASCII.GetBytes(deptName))).Substring(0, 10);
                if (!filter.CheckFor(id))
                    filter.AddItem(id);
                
                return id.Replace("/", "c").Replace("=", "b").Replace("+", "A").Replace("\\", "C");
            }
            /// <summary>
            /// Generates an EID.
            /// </summary>
            /// <param name="fName">Employee's first name.</param>
            /// <param name="lName">Their last name.</param>
            /// <param name="DID">Their DID.</param>
            /// <param name="position">Their Position.</param>
            /// <returns></returns>
            public static string EID(string fName, string lName, string DID,string position)
            {
                var id = "Emp" + (
                             Convert.ToBase64String(
                                 hasher.ComputeHash(
                                     Encoding.ASCII.GetBytes(fName+lName+DID))).Substring(0, 20) +
                             Convert.ToBase64String(
                                 Encoding.ASCII.GetBytes(DID+position))).Substring(0, 10);

                if (!filter.CheckFor(id))
                    filter.AddItem(id);

                return id.Replace("/", "c").Replace("=", "b").Replace("+", "A").Replace("\\", "C");
            }
            /// <summary>
            /// Generates a Store ID, given the address.
            /// You can't have two differnt STRIDs at the same addr
            /// </summary>
            /// <param name="address">Address of the store.</param>
            /// <returns></returns>
            public static string STRID(string address)
            {
                var id = "Str" + (
                             Convert.ToBase64String(
                                 hasher.ComputeHash(
                                     Encoding.ASCII.GetBytes(address))).Substring(0, 20) +
                             Convert.ToBase64String(
                                 Encoding.ASCII.GetBytes(address))).Substring(0, 10);

                if (!filter.CheckFor(id))
                    filter.AddItem(id);

                return id.Replace("/", "c").Replace("=", "b").Replace("+", "A").Replace("\\", "C");

            }

            public static void CloseFilter()
            {
                filter.Close();
                
            }
        }
    }
}
