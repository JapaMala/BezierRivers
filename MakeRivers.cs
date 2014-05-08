using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;


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
        public PointF edge_coords; // The coordinates of the current water tile, by edge

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

        static Point[] neighbors = { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) }; // top, down, left, right


        // scale is the ratio of embark tiles to blocks.
        MakeRivers(string inputfilename, int Scale = 8)
        {
            Bitmap inputFile = new Bitmap(inputfilename);
            width = inputFile.Width;
            height = inputFile.Height;
            scale = Scale;
            input = new int[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Color color = inputFile.GetPixel(x, y);
                    input[x, y] = IntFromName[NameFromColor[color]];
                }
        }

        int[,] input;
        int scale;
        int width, height;

        static void Main(string[] args)
        {
            MakeRivers mr = new MakeRivers("testinput.png");
            mr.makeRivers();
        }

        struct processedResults
        {
            public List<Node> rivers;
            public List<Point> oceans;
            public List<Point> lakes;
        }

        bool isRiver(Point coords)
        {
            if (0 <= coords.X && coords.X < width && 0 <= coords.Y && coords.Y < height)
                return RiverTypes.Contains(input[coords.X, coords.Y]);
            return false;
        }

        bool isOcean(Point coords)
        {
            return input[coords.X, coords.Y] == IntFromName["ocean"];
        }

        bool isLand(Point coords)
        {
            return input[coords.X, coords.Y] == IntFromName["land"] || input[coords.X, coords.Y] == IntFromName["mountain"];
        }

        bool isLake(Point coords)
        {
            return input[coords.X, coords.Y] == IntFromName["lake"];
        }

        void makeRivers()
        {
            DateTime start = DateTime.Now;
            var result = process();

            DateTime end1 = DateTime.Now;
            Console.WriteLine("Processing done. Elapsed time {0}ms", (end1 - start).TotalMilliseconds);
            var arrayout = render(result, "output.png", true);
            DateTime end2 = DateTime.Now;
            Console.WriteLine("Output done. Elapsed time {0}ms", (end2 - end1).TotalMilliseconds);
            Console.ReadKey();
        }

        // TODO: Land-locked rivers
        // TODO?: Add the one river tile connecting ocean and river.
        // TODO: smoother curvier rivers
        processedResults process()
        {
            HashSet<Point> done = new HashSet<Point>(); // River tiles that have been added to a river tree 
            List<Point> oceans = new List<Point>(); // Tiles that are definitely ocean.
            List<Point> lakes = new List<Point>(); // ditto but for lakes            
            List<Node> rivers = new List<Node>(); // List of parent nodes of finished river trees.
            // FLOOD FILL AND PUT ALL OCEAN TILES INTO OCEAN LIST
            // LAKES IN THE LAKE LIST
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Point current = new Point(x, y);
                if (isOcean(current))
                    oceans.Add(current);
                else if (isLake(current))
                    lakes.Add(current);
            }

            // Build rivers that start from the ocean
            processOceanToRiver(oceans, rivers, done);

            // Build rivers that start from lakes!
            processLakeToRiver(lakes, rivers, done);

            // Build rivers that start from land!
            processInlandRivers(rivers, done);

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

        void processOceanToRiver(List<Point> oceans, List<Node> rivers, HashSet<Point> done)
        {
            List<Point> possibleRiver = new List<Point>(); // Ocean tiles that touch at least one land tile
            HashSet<Point> working = new HashSet<Point>(); // River tiles that have yet to be added to a tree.

            // Move all land tiles bounding ocean tiles to the possibleRiver list.
            foreach (Point pt in oceans)
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)pt; // Von Neumann neighbors! Because C# does not provide an Addition operator for Point + Point for no good reason, one Point must be cast to Size to get the sum.
                    // Check bounds first.
                    if (0 > neighbor.X || width <= neighbor.X || 0 > neighbor.Y || height <= neighbor.Y)
                        continue;
                    if (isLand(neighbor))
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
                    if (isRiver(neighbor) && !working.Contains(neighbor)) // If it's any kind of river plus it hasn't already been added to the list, because duplicates are bad
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
                    if (isLand(neighbor))
                        foreach (Point ddP in neighbors)
                        {
                            Point neighbor2 = ddP + (Size)neighbor;
                            if (isOcean(neighbor2))
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
                    foreach (Point dP in neighbors)
                    {
                        treeWorking[dP + (Size)startcoords] = startnode;
                    }
                }
                else
                    foreach (Point dP in neighbors)
                    {
                        treeWorking[dP + (Size)basePoint] = parent;
                    }

                buildRiverTrees(done, treeWorking);

                // At this point, the river tree should be fully built. Add the parent node to a list of rivers.
                rivers.Add(parent);
            }
        }

        void processLakeToRiver(List<Point> lakes, List<Node> rivers, HashSet<Point> done)
        {
            List<Tuple<Point, Point>> possibleRiver = new List<Tuple<Point, Point>>();

            // Add (lake tile, river tile) tuple to possibleRiver.
            foreach (Point lake in lakes)
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)lake;
                    if (isRiver(neighbor))
                    {
                        possibleRiver.Add(new Tuple<Point,Point>(lake, neighbor));
                        break;
                    }
                }

            foreach (Tuple<Point, Point> pts in possibleRiver)
            {
                Dictionary<Point, Node> treeWorking = new Dictionary<Point, Node>();
                Node parent = new Node(pts.Item1);
                parent.insertChild(new Node(pts.Item2));
                Point coords = parent._children_[0].center_coords;
                foreach (Point dP in neighbors)
                {
                    Point neighbor = dP + (Size)coords;
                    if (neighbor != pts.Item1) // Make sure not to double back on the origin lake tile.
                        treeWorking[neighbor] = parent._children_[0];
                }
                buildRiverTrees(done, treeWorking);

                rivers.Add(parent);
            }
        }

        // This function is a bit different. If a river is encountered while floodfilling the map that is not in the "done" hashset, the river must be crawled in both directions until it meets /any/ end.
        // A river tree can then be created.
        void processInlandRivers(List<Node> rivers, HashSet<Point> done)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Point coord = new Point(x, y);
                    if (isRiver(coord) && !done.Contains(coord))
                    {
                        // It's a river, and it hasn't already been covered by the other processors.
                        HashSet<Point> searchDone = new HashSet<Point>(); // A sort of "temporary" "done" HashSet.
                        List<Point> working = new List<Point>();
                        Node start = null;

                        working.Add(coord);
                        while (working.Count > 0)
                        {
                            Point current = working.First();
                            working.Remove(current);
                            searchDone.Add(current);
                            int numNeighbors = 0;
                            foreach (Point dP in neighbors)
                            {
                                Point neighbor = dP + (Size)current;
                                if (isRiver(neighbor) && !searchDone.Contains(neighbor)) // Must check that we haven't already been to this river tile to prevent perpetual cycling.
                                {
                                    working.Add(neighbor);
                                    numNeighbors++;
                                }                                
                            }
                            if (numNeighbors == 0) // We have reached the end of the line.
                            {
                                start = new Node(current);
                                break;
                            }                         
                        }

                        if (start == null)
                            throw new ArgumentNullException("Could not find starting point for node at " + coord.ToString());

                        // Now that we have a starting place...
                        Dictionary<Point, Node> treeWorking = new Dictionary<Point, Node>();
                        foreach (Point dP in neighbors)
                        {
                            treeWorking[dP + (Size)start.center_coords] = start;
                        }
                        buildRiverTrees(done, treeWorking);

                        rivers.Add(start);
                    }
                }
        }

        // Building the actual river tree is identical, whether we start from the ocean or a lake
        // Here, treeWorking contains tiles to investigate for riverness along with the parent node they started from.
        void buildRiverTrees(HashSet<Point> done, Dictionary<Point, Node> treeWorking)
         {
             while (treeWorking.Count > 0) // Next, keep working on tiles until there are no more left to work on.
                {
                    Point pt = treeWorking.Keys.First();
                    Node myParent = treeWorking[pt];
                    // move to done.
                    treeWorking.Remove(pt);
                    if (done.Contains(pt))
                        continue;
                    done.Add(pt);
                    if (isLake(pt)) // If it's a lake, add the tile as the final node.
                    {
                        Node lake = new Node(pt);
                        myParent.insertChild(lake);
                        continue;
                    }
                    else if (!isRiver(pt)) // If it's not a river, don't work on it.
                        continue;
                    // Make new node and add to parent node of this one.
                    Node me = new Node(pt);
                    myParent.insertChild(me);
                    // Check neighbors for riverness.
                    foreach (Point dP in neighbors)
                    {
                        Point neighbor = dP + (Size)pt;
                        if (isRiver(neighbor) || isLake(neighbor))
                            treeWorking[neighbor] = me; // Needs to add this node as a reference in the data.                            
                    }                
                }
         }

        static PointF edgifyNodes(Node n1, Node n2)
        {
            PointF p1 = n1.center_coords;
            PointF p2 = n2.center_coords;
            PointF p3 = new Point();
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

        unsafe Dictionary<Point, int> DictionaryFromBitmap(Bitmap bmp)
        {
            Dictionary<Point, int> outarray = new Dictionary<Point, int>();
            // Must lock the bitmap for faster access
            var bitmapdata = bmp.LockBits(new Rectangle(0, 0, scale * width, scale * height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

            // no copy
            // Basically width*height*3
            byte* p = (byte*)(void*)bitmapdata.Scan0.ToPointer();
            int stride = bitmapdata.Stride;
            int colorsize = System.Drawing.Bitmap.GetPixelFormatSize(bitmapdata.PixelFormat) / 8; // Divided by 8 because we're dealing with bytes

            for (int y = 0; y < scale * height; y++)
                for (int x = 0; x < scale * width; x++)
                {
                    byte* row = &p[y * stride];
                    byte b = row[x*colorsize];
                    byte g = row[x*colorsize + 1];
                    byte r = row[x*colorsize + 2];
                    Color pix = Color.FromArgb(r, g, b);
                    if (pix == river_c)
                        outarray[new Point(x, y)] = 1;
                }
            bmp.UnlockBits(bitmapdata);

            return outarray;
        }

        Dictionary<Point,int> render(processedResults pr, string filename, bool edges = false)
        {
            List<Node> rivers = pr.rivers;
            List<Point> oceans = pr.oceans;
            List<Point> lakes = pr.lakes;

            List<Node> rendernodes = new List<Node>();
            foreach (Node r in rivers) // rendernodes are different from river nodes in that they are not necessarily actual river trees.
                // They should sometimes be smaller branches of main rivers.
                rendernodes.Add(r);

            List<List<PointF>> curvepoints = new List<List<PointF>>(); // This is the result of this function, a list of list of points for use in drawCurve()
            // 
            while (rendernodes.Count > 0)
            {
                List<PointF> points = new List<PointF>();
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
            Graphics graphic = Graphics.FromImage(output);
            SolidBrush oceanbrush = new SolidBrush(ocean_c);
            SolidBrush landbrush = new SolidBrush(land_c);
            SolidBrush lakebrush = new SolidBrush(lake_c);

            // Draws the land
            graphic.FillRectangle(landbrush, new Rectangle(0, 0, width * scale, height * scale));

            // Draw rivers
            Pen pen = new Pen(river_c, (float)scale/2.0f);
            Random rng = new Random();
            foreach (var pointlist in curvepoints)
            {
                PointF[] points = pointlist.ToArray();
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
                    continue;//g.DrawRectangle(pen, new Rectangle(points[0].X, points[0].Y, scale, scale));
                else
                    graphic.DrawCurve(pen, points, 0.4f);
            }

            // Finally, draw the oceans and lakes over the rivers to "hide" the ends of the river.
            foreach (var point in oceans)
            {
                graphic.FillRectangle(oceanbrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }
            foreach (var point in lakes)
            {
                graphic.FillRectangle(lakebrush, new Rectangle(point.X * scale, point.Y * scale, scale, scale));
            }


            output.Save(filename);    
            var result = DictionaryFromBitmap(output);
            output.Dispose();
            return result;
        }
    }
}
