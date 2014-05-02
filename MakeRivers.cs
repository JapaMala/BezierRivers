using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace BezierRivers
{

    // Requires the entire tree to be recursively updated once to update tail length.
    class Node
    {
        public Node()
        {

        }

        public Node(Point myCoords)
        {
            coords = myCoords;
            __children__ = new List<Node>();
            maxTailLength = 1;
        }

        public int numChildren()
        {
            return __children__.Count();
        }

        public int maxTailLength;
        public int maxLengthIndex; // The index of the tail that is the longest.
        public Point coords; // The coordinates of the current water tile.

        // Travel the nodes, choosing the longest path.
        // Calling this on the base node effectively updates the entire tree.
        public int getMaxTailLength()
        {
            if (__children__.Count() == 0)
                maxTailLength = 1;
            else
            {
                int maxlength = __children__[0].getMaxTailLength();
                int index = 0;
                for (int i = 1; i < __children__.Count(); i++)
                    if (__children__[i].getMaxTailLength() > maxlength)
                    {
                        maxlength = __children__[i].maxTailLength;
                        index = i;
                    }
                        
                maxTailLength = maxlength;
                maxLengthIndex = index;
            }

            return maxTailLength;
        }

        // Do not edit directly.
        public List<Node> __children__; // int is the length of the trailing node. 

        public Node parent;
        public void insertChild(Node child)
        {
            child.parent = this;
            __children__.Add(child);
        }

    }

    class MakeRivers
    {
        // Define colors
        static Color brook_c = Color.FromArgb(0, 255, 255);
        static Color stream_c = Color.FromArgb(0, 224, 255);
        static Color minorRiver_c = Color.FromArgb(0, 192, 255);
        static Color river_c = Color.FromArgb(0, 160, 255);
        static Color majorRiver_c = Color.FromArgb(0, 128, 255);
        static Color mountain_c = Color.FromArgb(255, 255, 192);
        static Color lake_c = Color.FromArgb(0, 96, 255);
        static Color land_c = Color.FromArgb(128, 64, 32);
        static Color ocean_c = Color.FromArgb(0, 64, 255);
        static Color riverocean_c = Color.FromArgb(0, 112, 255);
        static Dictionary<Color, string> NameFromColor = new Dictionary<Color, string>()
        {
            {brook_c, "brook"},
            {stream_c, "stream"},
            {minorRiver_c, "minor river"},
            {river_c, "river"},
            {majorRiver_c, "major river"},
            {mountain_c, "mountain"},
            {lake_c, "lake"},
            {land_c, "land"},
            {ocean_c, "ocean"},
            {riverocean_c, "river/ocean"}
        };
        static Dictionary<string, int> IntFromName = new Dictionary<string, int>()
        {
            {"brook", 0},
            {"stream", 1},
            {"minor river", 2},
            {"river", 3},
            {"major river", 4},
            {"mountain", 5},
            {"lake", 6},
            {"land", 7},
            {"ocean", 8},
            {"river/ocean", 9}
        };
        static SortedSet<int> RiverTypes = new SortedSet<int>()
        {
            {IntFromName["brook"]},
            {IntFromName["stream"]},
            {IntFromName["minor river"]},
            {IntFromName["river"]},
            {IntFromName["major river"]},
            {IntFromName["river/ocean"]}
        };
        


        static void Main(string[] args)
        {
            //int tiles = 8;
            //int scale = 64;
            Bitmap input = new Bitmap("testinput.png");            
            int xtiles = input.Width;
            int ytiles = input.Height;
            int scale = 10;
            int[,] inarray = new int[xtiles, ytiles];

            for (int y = 0; y < ytiles; y++)
                for (int x = 0; x < xtiles; x++)
                {
                    Color color = input.GetPixel(x, y);
                    inarray[x, y] = IntFromName[NameFromColor[color]];
                }

            DateTime start = DateTime.Now;
            var result = process(inarray, xtiles, ytiles);            

            DateTime end1 = DateTime.Now;
            Console.WriteLine("Processing done. Elapsed time {0}ms", (end1 - start).Milliseconds);
            render(result, "output.png", xtiles, ytiles, scale);
            DateTime end2 = DateTime.Now;
            Console.WriteLine("Output done. Elapsed time {0}ms", (end2 - end1).Milliseconds);
            //Graphics g = Graphics.FromImage(output);

            /*
            Pen pen = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
            Point p1 = new Point(32, 32);
            Point p2 = new Point(100, 100);
            Point p3 = new Point(57, 59);
            Point p4 = new Point(94, 10);
            g.DrawCurve(pen, new Point[] {p1, p2, p3, p4});
            output.Save("test.png");*/
        }

        struct processedResults
        {
            public List<Node> rivers;
            public List<Point> oceans;
            public List<Point> lakes;
        }

        // TODO: Land-locked rivers
        // TODO?: Add the one river tile connecting ocean and river.
        // TODO: smoother curvier rivers
        static processedResults process(int[,] input, int width, int height)
        {
            HashSet<Point> done = new HashSet<Point>(); // River tiles that have been added to a river tree 
            List<Point> oceans = new List<Point>(); // Tiles that are definitely ocean.
            List<Point> lakes = new List<Point>(); // ditto but for lakes
            List<Point> possibleRiver = new List<Point>(); // Ocean tiles that touch at least one land tile
            HashSet<Point> working = new HashSet<Point>(); // River tiles that have yet to be added to a tree.
            List<Node> rivers = new List<Node>(); // List of parent nodes of finished river trees.

            Point[] neighbors = { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) }; // top, down, left, right
            // FLOOD FILL AND PUT ALL OCEAN TILES INTO OCEAN LIST
            // LAKES IN THE LAKE LIST
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Point current = new Point(x, y);
                if (input[x, y] == IntFromName["ocean"])
                    oceans.Add(current);
                else if (input[x, y] == IntFromName["lake"])
                    lakes.Add(current);
            }

            // Move all land tiles bounding ocean tiles to the possibleRiver list.
            foreach (Point pt in oceans)
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)pt; // Von Neumann neighbors! Because C# does not provide an Addition operator for Point + Point for no good reason, one Point must be cast to Size to get the sum.
                    // Check bounds first.
                    if (0 > neighbor.X || width <= neighbor.X || 0 > neighbor.Y || height <= neighbor.Y)
                        continue;
                    if (input[neighbor.X, neighbor.Y] == IntFromName["land"])
                    {
                        possibleRiver.Add(neighbor);
                    }
                }

            // For every land tile neighbor of the Ocean tile, check if there is river. If there is river, add the river point to the working list.
            foreach (Point pt in possibleRiver)
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)pt;
                    int type = input[neighbor.X, neighbor.Y];
                    if (RiverTypes.Contains(type) && !working.Contains(neighbor)) // If it's any kind of river plus it hasn't already been added to the list, because duplicates are bad
                    {
                        working.Add(neighbor);
                    }
                }

            // Finally, let's start building River trees.
            while (working.Count > 0)
            {
                Point basePoint = working.First();                    
                working.Remove(basePoint);
                if (done.Contains(basePoint)) // Skip if the point was already touched.
                    continue;
                done.Add(basePoint); // Mark this point as Done, so future iterations don't accidentally touch it.
                
                Dictionary<Point, Node> treeWorking = new Dictionary<Point, Node>(); // This working hashset is for building a river tree. The Node is a reference to the parent of this node.
                // There is no need for a treeDone set because what's done is done.                
                Node parent = new Node(basePoint);
                foreach (Point dP in neighbors)
                {
                    treeWorking[dP + (Size)basePoint] = parent;
                }

                while (treeWorking.Count > 0) // Next, keep working on tiles until there are no more left to work on.
                {
                    Point pt = treeWorking.Keys.First();
                    Node myParent = treeWorking[pt];
                    // move to done.
                    treeWorking.Remove(pt);
                    if (done.Contains(pt))
                        continue;
                    done.Add(pt);
                    if (!RiverTypes.Contains(input[pt.X, pt.Y])) // If it's not a river, don't work on it.
                        continue;
                    // Make new node and add to parent node of this one.
                    Node me = new Node(pt);
                    myParent.insertChild(me);
                    // Check neighbors for riverness.
                    foreach (Point dP in neighbors)
                    {
                        Point neighbor = dP + (Size)pt;
                        if (RiverTypes.Contains(input[neighbor.X, neighbor.Y]))
                            treeWorking[neighbor] = me; // Needs to add this node as a reference in the data.                            
                    }                
                }

                // At this point, the river tree should be fully built. Add the parent node to a list of rivers.
                rivers.Add(parent);
            }


            // Update tail length counts.
            foreach (Node n in rivers)
                n.getMaxTailLength();

            processedResults prout = new processedResults();
            prout.rivers = rivers;
            prout.oceans = oceans;
            prout.lakes = lakes;
            return prout;
        }

        static void render(processedResults pr, string filename, int width, int height, int scale)
        {
            List<Node> rivers = pr.rivers;
            List<Point> oceans = pr.oceans;
            List<Point> lakes = pr.lakes;

            List<Node> rendernodes = new List<Node>();
            foreach (Node r in rivers) // rendernodes are different from river nodes in that they are not necessarily actual river trees.
                // They should sometimes be smaller branches of main rivers.
                rendernodes.Add(r);

            List<List<Point>> curvepoints = new List<List<Point>>(); // This is the result of this function, a list of list of points for use in drawCurve()
            // 
            while (rendernodes.Count > 0)
            {
                List<Point> points = new List<Point>();
                Node current = rendernodes.First();
                rendernodes.Remove(current);
                points.Add(current.coords); // Initially add the coords to the list.

                while (current.numChildren() > 0)
                {                    
                    int largestIndex = current.maxLengthIndex; 
                    // Except for the largest index, add every other child node to the rendernodes list as a separate branch.
                    for (int i = 0; i < current.__children__.Count; i++)
                    {
                        if (largestIndex != i)
                        {
                            Node newparent = new Node(current.coords);
                            newparent.insertChild(current.__children__[i]);
                            newparent.getMaxTailLength();
                            rendernodes.Add(newparent);
                        }
                    }
                    current = current.__children__[largestIndex]; // Finally, change the current to point to the new node.
                    points.Add(current.coords); // Also, extend the pointslist.
                }

                curvepoints.Add(points);
            }

            Bitmap output = new Bitmap(width * scale, height * scale);
            Graphics g = Graphics.FromImage(output);

            SolidBrush oceanbrush = new SolidBrush(ocean_c);
            SolidBrush landbrush = new SolidBrush(land_c);
            SolidBrush lakebrush = new SolidBrush(lake_c);

            g.FillRectangle(landbrush, new Rectangle(0, 0, width * scale, height * scale));

            foreach (var point in oceans)
            {
                g.FillRectangle(oceanbrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }
            foreach (var point in lakes)
            {
                g.FillRectangle(lakebrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }
            

            Pen pen = new Pen(river_c, 5.0f);
            Random rng = new Random();
            foreach (var pointlist in curvepoints)
            {
                Point[] points = pointlist.ToArray();
                for (int i = 0; i < pointlist.Count; i++ ) // Scale coordinates
                {
                    points[i].X *= scale;
                    points[i].X += scale/2;
                    points[i].Y *= scale;
                    points[i].Y += scale/2;

                    // Experimental: Add noise to coordinates.
                    points[i].X += rng.Next(-scale / 5, scale / 5);
                    points[i].Y += rng.Next(-scale / 5, scale / 5);
                }

                if (pointlist.Count == 1)
                    g.DrawRectangle(pen, new Rectangle(points[0].X, points[0].Y, scale, scale));
                else
                    g.DrawCurve(pen, points, 0.8f);
            }

            output.Save(filename);
        }
    }
}
