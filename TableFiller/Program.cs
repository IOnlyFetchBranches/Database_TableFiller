using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableFiller.Util;

namespace TableFiller
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = 0;
            while (x < 1000000)
            {

                var name = Generators.NameGen.Gen(false, false);
                Console.WriteLine(name + "\n");
                x++;

            }

            Console.WriteLine("Done");
            while (true)
            {
                
            }
            
        }
    }
}
