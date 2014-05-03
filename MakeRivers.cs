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
            _children_ = new List<Node>();
            maxTailLength = 1;
        }

        public Node(Point myCoords)
        {
            center_coords = myCoords;
            _children_ = new List<Node>();
            maxTailLength = 1;
        }

        public int numChildren()
        {
            return _children_.Count();
        }

        public int maxTailLength;
        public int maxLengthIndex; // The index of the tail that is the longest.
        public Point center_coords; // The coordinates of the current water tile, by center
        public Point edge_coords; // The coordinates of the current water tile, by edge

        // Travel the nodes, choosing the longest path.
        // Calling this on the base node effectively updates the entire tree.
        public int getMaxTailLength()
        {
            if (_children_.Count() == 0)
                maxTailLength = 1;
            else
            {
                int maxlength = _children_[0].getMaxTailLength();
                int index = 0;
                for (int i = 1; i < _children_.Count(); i++)
                    if (_children_[i].getMaxTailLength() > maxlength)
                    {
                        maxlength = _children_[i].maxTailLength;
                        index = i;
                    }
                        
                maxTailLength = maxlength;
                maxLengthIndex = index;
            }

            return maxTailLength;
        }

        // Do not edit directly.
        public List<Node> _children_; // int is the length of the trailing node. 

        public Node parent;
        public void insertChild(Node child)
        {
            child.parent = this;
            _children_.Add(child);
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
            var result = processOceanToRiver(inarray, xtiles, ytiles);            

            DateTime end1 = DateTime.Now;
            Console.WriteLine("Processing done. Elapsed time {0}ms", (end1 - start).Milliseconds);
            render(result, "output.png", xtiles, ytiles, scale);
            DateTime end2 = DateTime.Now;
            Console.WriteLine("Output done. Elapsed time {0}ms", (end2 - end1).Milliseconds);

            Console.ReadKey();
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
        static processedResults processOceanToRiver(int[,] input, int width, int height)
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
            {
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)pt;
                    int type = input[neighbor.X, neighbor.Y];
                    if (RiverTypes.Contains(type) && !working.Contains(neighbor)) // If it's any kind of river plus it hasn't already been added to the list, because duplicates are bad
                    {
                        working.Add(neighbor);
                    }
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

                Node parent = null;

                // First, find the land tile connecting this river start and the ocean.
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)basePoint;
                    // For each land tile surrounding the start of the ocean, find the nearest ocean tile and go there.
                    if (input[neighbor.X, neighbor.Y] == IntFromName["land"])
                        foreach (Point ddP in neighbors)
                        {
                            Point neighbor2 = ddP + (Size)neighbor;
                            if (input[neighbor2.X, neighbor2.Y] == IntFromName["ocean"])
                            {
                                parent = new Node(neighbor2);
                                parent.insertChild(new Node(neighbor));
                                parent._children_[0].insertChild(new Node(basePoint));
                                goto exit;
                            }
                        }                        
                }
                if (parent == null)
                    parent = new Node(basePoint);
            exit:
                Dictionary<Point, Node> treeWorking = new Dictionary<Point, Node>(); // This working hashset is for building a river tree. The Node is a reference to the parent of this node.
                // There is no need for a treeDone set because what's done is done.                
               
                
                // Now, if the parent has children, it means the place we are going to start looking for the river is the third node of the tree.
                if (parent._children_.Count > 0)
                {
                    Node startnode = parent._children_[0]._children_[0];
                    Point startcoords = startnode.center_coords;
                    foreach(Point dP in neighbors)
                    {
                        treeWorking[dP + (Size)startcoords] = startnode;
                    }
                }
                else
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


            ////// TEST: FUNCTION TO MOVE VERTEXES TO EDGE FROM CENTER SEMI-RECURSIVELY
            for (int i = 0; i < rivers.Count; i++)
            {
                startTranslate(rivers[i]);
            }

            processedResults prout = new processedResults();
            prout.rivers = rivers;
            prout.oceans = oceans;
            prout.lakes = lakes;
            return prout;
        }

        // Building the actual river tree is identical 
       /* static processedResults buildRiverTrees()
        {

        }*/

        static Point edgifyNodes(Node n1, Node n2)
        {
            Point p1 = n1.center_coords;
            Point p2 = n2.center_coords;
            Point p3 = new Point();
            if (p1.X == p2.X && p1.Y != p2.Y)
            {
                p3.X = p1.X;
                p3.Y = (p1.Y + p2.Y) / 2;
            }
            else if (p1.X != p2.X && p1.Y == p2.Y)
            {
                p3.X = (p1.X + p2.X) / 2;
                p3.Y = p1.Y;
            }
            else
                throw new InvalidOperationException("Node n1 and n2 are equal or at diagonals to each other.");

            return p3;
        }

        // Semi-recursive function to move all points in a river tree to the edges from the centers.
        static void recursiveTranslate(Node previous, Node next, int depth)
        {
            Node current = previous;
            Node following = next;

            following.edge_coords = edgifyNodes(current, following);
            current = following;
            while (current._children_.Count == 1) // Don't call the recursive function needlessly if there's no need to
            {
                following = current._children_[0];
                following.edge_coords = edgifyNodes(current, following);
                current = following;
            }
            if (depth > 100)
                Console.ReadKey();
            // At this point, either this has more than 1 child or has no children
            if (current._children_.Count > 1)
            {
                startTranslate(current, depth + 1);
                /*for (int i = 0; i < current._children_.Count; i++)
                {
                    recursiveTranslate(current, current._children_[i], depth+1);
                }*/
            }

        }

        static void startTranslate(Node parent, int depth = 0)
        {
            if (parent._children_.Count == 0)
                return;
            for (int i = 0; i < parent._children_.Count; i++)
                recursiveTranslate(parent, parent._children_[i], depth);
        }

        static void render(processedResults pr, string filename, int width, int height, int scale, bool edges = false)
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

                // Initially add the coords to the list.
                points.Add(current.center_coords); // Always starts with the center coords, because the first tile never gets an edge coord set.

                while (current.numChildren() > 0)
                {                    
                    int largestIndex = current.maxLengthIndex; 
                    // Except for the largest index, add every other child node to the rendernodes list as a separate branch.
                    for (int i = 0; i < current._children_.Count; i++)
                    {
                        if (largestIndex != i)
                        {
                            Node newparent = new Node();
                            newparent.center_coords = current.center_coords;
                            newparent.edge_coords = current.edge_coords;
                            newparent.insertChild(current._children_[i]);
                            newparent.getMaxTailLength();
                            rendernodes.Add(newparent);
                        }
                    }
                    current = current._children_[largestIndex]; // Finally, change the current to point to the new node.

                    if (edges)
                        points.Add(current.edge_coords);
                    else
                        points.Add(current.center_coords); // Also, extend the pointslist.
                }

                curvepoints.Add(points);
            }

            Bitmap output = new Bitmap(width * scale, height * scale);
            Graphics g = Graphics.FromImage(output);
            SolidBrush oceanbrush = new SolidBrush(ocean_c);
            SolidBrush landbrush = new SolidBrush(land_c);
            SolidBrush lakebrush = new SolidBrush(lake_c);

            // Draws the land
            g.FillRectangle(landbrush, new Rectangle(0, 0, width * scale, height * scale));

            // Draw rivers
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
                    points[i].X += rng.Next(-scale / 4, scale / 4);
                    points[i].Y += rng.Next(-scale / 4, scale / 4);
                }

                if (pointlist.Count == 1)
                    g.DrawRectangle(pen, new Rectangle(points[0].X, points[0].Y, scale, scale));
                else
                    g.DrawCurve(pen, points, 0.6f);
            }

            // Finally, draw the oceans and lakes over the rivers to "hide" the ends of the river.
            foreach (var point in oceans)
            {
                g.FillRectangle(oceanbrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }
            foreach (var point in lakes)
            {
                g.FillRectangle(lakebrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }


            output.Save(filename);
        }
    }
}
