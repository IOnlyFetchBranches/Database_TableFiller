using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableFiller.Util.Models
{
   public class Address
    {

        public string Street { get; set; }
        public CityArea Region { get; set; }

        public Address(string street, CityArea area)
        {
            Street = street;
            Region = area;
        }

        //We need to override toString
        public override string ToString()
        {
            return Street + " " + Region; 
        }
    }
}
