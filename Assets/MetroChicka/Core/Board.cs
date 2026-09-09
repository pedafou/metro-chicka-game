using System;
using System.Collections.Generic;
using System.Linq;

namespace MetroChicka.Core
{
    public enum Shell { Straight, Bend, Bridge, Fork }
    public struct Point : IEquatable<Point>
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
        public bool Equals(Point p) => X == p.X && Y == p.Y;
        public override bool Equals(object o) => o is Point p && Equals(p);
        public override int GetHashCode() => X * 397 ^ Y;
        public static Point operator +(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
        public static Point Rotate(Point p, int d) { for (int i = 0; i < (d % 4 + 4) % 4; i++) p = new Point(-p.Y, p.X); return p; }
        public override string ToString() => $"({X},{Y})";
    }
    public sealed class Chicka
    {
        public int Id, Direction, Used;
        public Point Position;
        public Shell[] Layers;
        public int Remaining => Layers.Length - Used;
        public Chicka Copy() => new Chicka { Id = Id, Direction = Direction, Used = Used, Position = Position, Layers = (Shell[])Layers.Clone() };
    }
    public sealed class Road
    {
        public Shell Kind;
        public int Owner;
        public Point[] Main;
        public Point[] Spur;
        public Point End => Main[Main.Length - 1];
        public IEnumerable<(Point a, Point b)> Segments()
        {
            for (int i = 1; i < Main.Length; i++) yield return (Main[i - 1], Main[i]);
            if (Spur != null) for (int i = 1; i < Spur.Length; i++) yield return (Spur[i - 1], Spur[i]);
        }
        public bool Touches(Point p)
        {
            if (Kind == Shell.Bridge) return p.Equals(Main[0]) || p.Equals(End);
            return Segments().Any(s => On(s.a, s.b, p));
        }
        private static bool On(Point a, Point b, Point p) =>
            (b.X-a.X)*(p.Y-a.Y)==(b.Y-a.Y)*(p.X-a.X) && p.X>=Math.Min(a.X,b.X) && p.X<=Math.Max(a.X,b.X) && p.Y>=Math.Min(a.Y,b.Y) && p.Y<=Math.Max(a.Y,b.Y);
        public bool Connects(Road r)
        {
            if (Kind == Shell.Bridge) return r.Touches(Main[0]) || r.Touches(End);
            if (r.Kind == Shell.Bridge) return Touches(r.Main[0]) || Touches(r.End);
            foreach (var s in Segments()) foreach (var t in r.Segments())
            {
                if (On(s.a,s.b,t.a)||On(s.a,s.b,t.b)||On(t.a,t.b,s.a)||On(t.a,t.b,s.b)) return true;
                if ((s.a.X==s.b.X && t.a.Y==t.b.Y && On(s.a,s.b,new Point(s.a.X,t.a.Y)) && On(t.a,t.b,new Point(s.a.X,t.a.Y))) ||
                    (t.a.X==t.b.X && s.a.Y==s.b.Y && On(t.a,t.b,new Point(t.a.X,s.a.Y)) && On(s.a,s.b,new Point(t.a.X,s.a.Y)))) return true;
            }
            return false;
        }
        public static Road Create(Chicka c, int direction)
        {
            var kind = c.Layers[c.Used];
            var main = kind == Shell.Bend ? new[] { new Point(0,0),new Point(2,0),new Point(2,2) } : new[] { new Point(0,0),new Point(4,0) };
            return new Road { Owner=c.Id,Kind=kind,Main=main.Select(p=>c.Position+Point.Rotate(p,direction)).ToArray(),
                Spur=kind==Shell.Fork ? new[]{new Point(2,0),new Point(2,2)}.Select(p=>c.Position+Point.Rotate(p,direction)).ToArray() : null };
        }
        public bool InBounds => Main.Concat(Spur ?? Array.Empty<Point>()).All(p=>Math.Abs(p.X)<=8 && Math.Abs(p.Y)<=5);
    }
    public sealed class Move
    {
        public int Id, Depth;
        public Point From, To;
        public Road Road;
    }
    public sealed class Plan
    {
        public readonly List<Move> Moves = new List<Move>();
        public readonly List<int> Blocked = new List<int>();
        public string Error;
        public int Score => 10 * Moves.Count * (Moves.Count + 1) / 2;
        public int Revision;
    }
    public sealed class Board
    {
        public List<Chicka> Chickas = new List<Chicka>();
        public List<Road> Roads = new List<Road>();
        public int Score, Actions, Revision;
        public bool Relay = true;
        public int Remaining => Chickas.Sum(c=>c.Remaining);
        public Board Copy() => new Board { Chickas=Chickas.Select(c=>c.Copy()).ToList(),Roads=new List<Road>(Roads),Score=Score,Actions=Actions,Revision=Revision,Relay=Relay };
        public bool Aim(int id,int direction)
        {
            var c=Chickas.Find(x=>x.Id==id);
            if(c==null || c.Remaining==0) return false;
            c.Direction=(direction%4+4)%4; Revision++; return true;
        }
        public void SetRelay(bool value) { Relay=value; Revision++; }
        public Plan Preview(int id, int direction)
        {
            var result = new Plan { Revision=Revision };
            var first=Chickas.Find(c=>c.Id==id);
            if(first==null || first.Remaining<=0) { result.Error="NO_LAYERS"; return result; }
            var start=Road.Create(first,direction);
            if(!start.InBounds) { result.Error="OUTSIDE"; return result; }
            var visited=new HashSet<int>();
            var queue=new Queue<(Chicka c,Road road,int depth)>();
            queue.Enqueue((first,start,0)); visited.Add(first.Id);
            var all=new List<Road>(Roads);
            while(queue.Count>0)
            {
                var item=queue.Dequeue();
                result.Moves.Add(new Move { Id=item.c.Id,From=item.c.Position,To=item.road.End,Road=item.road,Depth=item.depth });
                all.Add(item.road);
                if(!Relay) continue;
                // Flood only the component reached by this new road. Bridge interiors are insulated.
                var connected=new HashSet<Road> { item.road };
                var roadsQueue=new Queue<Road>(); roadsQueue.Enqueue(item.road);
                while(roadsQueue.Count>0)
                {
                    var road=roadsQueue.Dequeue();
                    foreach(var other in all) if(!connected.Contains(other)&&road.Connects(other)) { connected.Add(other); roadsQueue.Enqueue(other); }
                }
                foreach(var c in Chickas.OrderBy(c=>c.Id))
                {
                    if(c.Remaining<=0 || visited.Contains(c.Id) || !connected.Any(r=>r.Touches(c.Position))) continue;
                    visited.Add(c.Id);
                    var next=Road.Create(c,c.Direction);
                    if(!next.InBounds) { result.Blocked.Add(c.Id); continue; }
                    queue.Enqueue((c,next,item.depth+1));
                }
            }
            return result;
        }
        public bool Apply(Plan plan)
        {
            if(plan==null || plan.Error!=null || plan.Moves.Count==0 || plan.Revision!=Revision) return false;
            foreach(var move in plan.Moves)
            {
                var c=Chickas.Find(x=>x.Id==move.Id);
                c.Position=move.To; c.Used++; Roads.Add(move.Road);
            }
            Score+=plan.Score; Actions++; Revision++; return true;
        }
        public static Board Scenario(int index)
        {
            var b=new Board();
            void Add(int id,int x,int y,int d,params Shell[] layers) => b.Chickas.Add(new Chicka {Id=id,Position=new Point(x,y),Direction=d,Layers=layers});
            if(index==0) Add(0,-5,-2,0,Shell.Straight,Shell.Bend,Shell.Bridge);
            else if(index==1)
            {
                Add(0,-6,0,0,Shell.Straight,Shell.Bend,Shell.Bridge);
                Add(1,-2,0,0,Shell.Straight,Shell.Fork,Shell.Bend);
                Add(2,2,0,1,Shell.Bend,Shell.Straight,Shell.Bridge);
            }
            else
            {
                Add(0,-6,-2,0,Shell.Fork,Shell.Bridge,Shell.Bend);
                Add(1,-4,0,0,Shell.Bend,Shell.Straight,Shell.Fork);
                Add(2,0,2,3,Shell.Bridge,Shell.Bend,Shell.Straight);
                b.Roads.Add(new Road {Kind=Shell.Straight,Owner=1,Main=new[]{new Point(-4,0),new Point(0,0),new Point(0,2)}});
            }
            return b;
        }
    }
}
