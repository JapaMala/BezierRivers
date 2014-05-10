using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BezierRivers;

namespace BezierRivers
{
    class Program
    {
        static void Main(string[] args)
        {
            /*Point p1, p2, p3, p4;
            p1 = new Point(20, 20); // start
            p2 = new Point(20, 80); // control points
            p3 = new Point(80, 80);
            p4 = new Point(80, 20); // end
            

            Bezier.rasterizeReference(p1, p2, p3, p4, "reference.png");
            Bezier.rasterizeDirect(p1, p2, p3, p4, "direct.png");
            Bezier.rasterizeSlope(p1, p2, p3, p4, "sloped.png");
            return;*/
            MakeRivers mr = new MakeRivers("testinput.png");
            mr.makeRivers();
            Console.ReadKey();
        }
    }
}
