using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableFiller.Util
{
    /*
     * As we design features, they need to  have a tester method here.
     */
    public abstract class Testers
    {
        public static void TestNameGen()
        {
            var x = 0;
            while (x < 1000000)
            {

                var name = Generators.NameGen.Gen(false, false);
                Console.WriteLine(name + "\n");
                x++;

            }
        }

        //Using linq here
        public static void TestDataBaseConnection()
        {
            
        }
    }
}
