using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableFiller.Util.Models
{
    public class CityArea
    {

        public string  City { get; set; }   
        public int  Zip { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        /// <summary>
        /// This constructor expects something along the lines of (mycity), (mystate), (myzip) , (mycountry)
        /// </summary>
        /// <param name="areaPart"></param>
        public CityArea(string areaPart)
        {
            var parts = areaPart.Split(',');
            City = parts[0];
            State = parts[1];
            Zip = Int32.Parse(parts[2]);
            Country = parts[3];
        }

        public override string ToString()
        {
            return City + ", " + State + " " + Zip + " " + Country;
        }
    }
}
